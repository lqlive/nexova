using Apache.DataFusion;
using Nexova.Engine.Contracts;
using Nexova.Engine.Exceptions;
using Nexova.Engine.Sources.Files.ObjectStore;

namespace Nexova.Engine.Sources.Files;

/// <summary>
/// Registers file-backed tables (CSV/JSON/Parquet/Excel, local or S3) into a DataFusion
/// <see cref="SessionContext"/>. Mirrors the Rust engine's <c>files</c> module.
/// </summary>
public sealed class FileSourceRegistrar
{
    private readonly S3SourceRegistrar s3;

    public FileSourceRegistrar(S3SourceRegistrar s3)
    {
        this.s3 = s3;
    }

    /// <summary>Builds a standalone context with every discovered table of a single file source.</summary>
    public SessionContext BuildContext(DataSourceConnection source)
    {
        var tables = FileDiscovery.Discover(source);
        var buckets = RemoteBuckets(tables);

        SessionContext context;
        if (buckets.Count > 0)
        {
            var builder = SessionContext.CreateBuilder();
            s3.RegisterStores(builder, buckets);
            context = builder.Build();
        }
        else
        {
            context = new SessionContext();
        }

        foreach (var table in tables)
        {
            RegisterTable(context, table, source);
        }

        return context;
    }

    /// <summary>S3 buckets used by a source, so a shared (federated) context can pre-register stores.</summary>
    public IReadOnlyCollection<string> CollectBuckets(DataSourceConnection source) =>
        RemoteBuckets(FileDiscovery.Discover(source));

    /// <summary>Registers a file source's tables into an existing (shared) context for federation.</summary>
    public void RegisterTables(SessionContext context, DataSourceConnection source, ISet<string> registeredNames)
    {
        var tables = FileDiscovery.Discover(source);
        var alias = Sanitize(source.Alias);
        if (alias is not null && tables.Count > 1)
        {
            throw EngineException.InvalidConnection(
                "file source alias can only be used with a single discovered table");
        }

        foreach (var table in tables)
        {
            var effective = alias is not null ? table with { Name = alias } : table;
            if (!registeredNames.Add(effective.Name))
            {
                throw EngineException.FileSource($"duplicate federated table name '{effective.Name}'");
            }

            RegisterTable(context, effective, source);
        }
    }

    private void RegisterTable(SessionContext context, FileTable table, DataSourceConnection source)
    {
        var target = table.Location.PathOrUrl;
        switch (table.Kind)
        {
            case DataSourceKind.Csv:
                context.RegisterCsv(table.Name, target, CsvOptionsFactory.Build(source));
                break;

            case DataSourceKind.Json:
                context.RegisterJson(table.Name, target);
                break;

            case DataSourceKind.Parquet:
                context.RegisterParquet(table.Name, target);
                break;

            case DataSourceKind.Excel:
                if (table.Location.IsRemote)
                {
                    throw EngineException.FileSource("Excel over S3 is not supported by the in-process engine");
                }

                context.RegisterTable(table.Name, ExcelTableLoader.LoadLocal(target, source.Sheet));
                break;

            default:
                throw EngineException.UnsupportedDataSource();
        }
    }

    private static List<string> RemoteBuckets(IReadOnlyList<FileTable> tables) =>
        tables
            .Where(table => table.Location is { IsRemote: true, Bucket: not null })
            .Select(table => table.Location.Bucket!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string? Sanitize(string? raw)
    {
        var trimmed = raw?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
