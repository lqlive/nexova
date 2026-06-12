using Nexova.Engine.Contracts;
using Nexova.Engine.Exceptions;

namespace Nexova.Engine.Sources.Files;

/// <summary>Where a discovered file table physically lives.</summary>
public sealed record FileLocation(bool IsRemote, string PathOrUrl, string? Bucket)
{
    public static FileLocation Local(string path) => new(false, path, null);

    public static FileLocation Remote(string url, string bucket) => new(true, url, bucket);
}

/// <summary>A logical table discovered from a file source.</summary>
public sealed record FileTable(string Name, DataSourceKind Kind, FileLocation Location);

/// <summary>
/// Discovers the logical tables backing a file source and computes a change-detection signature.
/// Local directories yield one table per supported file (mirroring the Rust engine); S3 sources
/// yield a single listing table (the in-process binding does not expose object listing).
/// </summary>
public static class FileDiscovery
{
    public static bool IsS3(DataSourceConnection source) =>
        source.Path?.TrimStart().StartsWith("s3://", StringComparison.OrdinalIgnoreCase) ?? false;

    public static IReadOnlyList<FileTable> Discover(DataSourceConnection source)
    {
        return IsS3(source) ? DiscoverRemote(source) : DiscoverLocal(source);
    }

    /// <summary>
    /// Fingerprint used to invalidate cached contexts when the backing files change. For local
    /// paths it folds in file size and last-write time; for S3 the URL is used as-is.
    /// </summary>
    public static string Signature(DataSourceConnection source)
    {
        var path = source.RequirePath();
        if (IsS3(source))
        {
            return $"s3:{path};table={source.Table};sheet={source.Sheet}";
        }

        if (Directory.Exists(path))
        {
            var parts = Directory.EnumerateFiles(path)
                .Where(file => KindFromExtension(file) is not null)
                .OrderBy(file => file, StringComparer.Ordinal)
                .Select(Fingerprint);
            return string.Join('|', parts);
        }

        if (File.Exists(path))
        {
            return Fingerprint(path);
        }

        throw EngineException.FileSource("path is neither a file nor a directory");
    }

    private static string Fingerprint(string path)
    {
        var info = new FileInfo(path);
        return $"{path}:{info.Length}:{info.LastWriteTimeUtc.Ticks}";
    }

    private static IReadOnlyList<FileTable> DiscoverRemote(DataSourceConnection source)
    {
        var requestedKind = source.Kind();
        var url = source.RequirePath();
        var (bucket, key) = ParseS3Url(url);

        var kind = KindFromExtension(key) ?? (requestedKind != DataSourceKind.Files
            ? requestedKind
            : throw EngineException.FileSource("specify a concrete file type (csv/json/parquet) for an S3 prefix"));

        var name = ExplicitTableName(source)
            ?? ExplicitAlias(source)
            ?? TableNameFromPath(key);

        return [new FileTable(name, kind, FileLocation.Remote(url, bucket))];
    }

    private static IReadOnlyList<FileTable> DiscoverLocal(DataSourceConnection source)
    {
        var requestedKind = source.Kind();
        var root = source.RequirePath();

        List<FileTable> tables;
        if (Directory.Exists(root))
        {
            tables = DiscoverDirectory(root, requestedKind);
        }
        else if (File.Exists(root))
        {
            tables = [DiscoverFile(root, requestedKind, ExplicitTableName(source))];
        }
        else
        {
            throw EngineException.FileSource("path is neither a file nor a directory");
        }

        if (tables.Count == 0)
        {
            throw EngineException.FileSource("no supported files found for this data source");
        }

        EnsureUniqueNames(tables);
        return tables;
    }

    private static List<FileTable> DiscoverDirectory(string root, DataSourceKind requestedKind)
    {
        var tables = new List<FileTable>();
        foreach (var path in Directory.EnumerateFiles(root).OrderBy(file => file, StringComparer.Ordinal))
        {
            var kind = KindFromExtension(path);
            if (kind is null)
            {
                continue;
            }

            if (requestedKind == DataSourceKind.Files || requestedKind == kind)
            {
                tables.Add(new FileTable(TableNameFromPath(path), kind.Value, FileLocation.Local(path)));
            }
        }

        return tables;
    }

    private static FileTable DiscoverFile(string path, DataSourceKind requestedKind, string? explicitName)
    {
        var actual = KindFromExtension(path)
            ?? throw EngineException.FileSource("unsupported file extension");

        DataSourceKind kind;
        if (requestedKind == DataSourceKind.Files)
        {
            kind = actual;
        }
        else if (requestedKind.IsFileKind() && requestedKind != DataSourceKind.Files)
        {
            if (actual != requestedKind)
            {
                throw EngineException.FileSource(
                    $"file extension does not match source type: expected {requestedKind.TableType()}, found {actual.TableType()}");
            }

            kind = requestedKind;
        }
        else
        {
            throw EngineException.UnsupportedDataSource();
        }

        return new FileTable(explicitName ?? TableNameFromPath(path), kind, FileLocation.Local(path));
    }

    public static string ResolveTableName(DataSourceConnection source, string requestedTable)
    {
        var tables = Discover(source);
        var requested = requestedTable.Trim();
        if (requested.Length == 0)
        {
            return tables[0].Name;
        }

        if (tables.Any(table => string.Equals(table.Name, requested, StringComparison.Ordinal)))
        {
            return requested;
        }

        throw EngineException.InvalidConnection($"unknown file table '{requested}'");
    }

    public static (string Bucket, string Key) ParseS3Url(string url)
    {
        var withoutScheme = url.Trim()["s3://".Length..];
        var slash = withoutScheme.IndexOf('/');
        if (slash < 0)
        {
            return (withoutScheme, string.Empty);
        }

        return (withoutScheme[..slash], withoutScheme[(slash + 1)..]);
    }

    private static string? ExplicitTableName(DataSourceConnection source) =>
        Sanitize(source.Table);

    private static string? ExplicitAlias(DataSourceConnection source) =>
        Sanitize(source.Alias);

    private static string? Sanitize(string? raw)
    {
        var trimmed = raw?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : SanitizeTableName(trimmed);
    }

    private static DataSourceKind? KindFromExtension(string path)
    {
        var extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        return extension switch
        {
            "csv" => DataSourceKind.Csv,
            "json" or "jsonl" or "ndjson" => DataSourceKind.Json,
            "xlsx" or "xls" => DataSourceKind.Excel,
            "parquet" or "pq" => DataSourceKind.Parquet,
            _ => null,
        };
    }

    private static string TableNameFromPath(string path)
    {
        var stem = Path.GetFileNameWithoutExtension(path);
        return SanitizeTableName(string.IsNullOrEmpty(stem) ? "data" : stem);
    }

    private static string SanitizeTableName(string raw)
    {
        var chars = raw.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray();
        var name = new string(chars).Trim('_');
        if (name.Length == 0)
        {
            return "data";
        }

        return char.IsDigit(name[0]) ? $"t_{name}" : name;
    }

    private static void EnsureUniqueNames(IReadOnlyList<FileTable> tables)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var table in tables)
        {
            if (!names.Add(table.Name))
            {
                throw EngineException.FileSource($"duplicate table name '{table.Name}' after sanitizing file names");
            }
        }
    }
}
