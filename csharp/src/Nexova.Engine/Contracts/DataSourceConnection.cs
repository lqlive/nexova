using System.Text.Json.Serialization;
using Nexova.Engine.Exceptions;

namespace Nexova.Engine.Contracts;

/// <summary>
/// Wire contract describing a single data source. Bound directly from the HTTP request body,
/// mirroring the Rust engine's <c>DataSourceConnection</c>. For S3-backed file sources the
/// <see cref="Path"/> is an <c>s3://bucket/prefix</c> URL; credentials are resolved engine-side
/// and are never carried in the request.
/// </summary>
public sealed class DataSourceConnection
{
    [JsonPropertyName("type")]
    public string SourceType { get; init; } = string.Empty;

    public string? ConnectionString { get; init; }

    public string? Host { get; init; }

    public ushort? Port { get; init; }

    public string? Database { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }

    /// <summary>Database schema used by table registration. Postgres defaults to <c>public</c>.</summary>
    public string? Schema { get; init; }

    /// <summary>Local filesystem path, or an <c>s3://bucket/prefix</c> URL for S3-backed sources.</summary>
    public string? Path { get; init; }

    /// <summary>Logical table name exposed to SQL for a single file or database source.</summary>
    public string? Table { get; init; }

    /// <summary>Logical table name exposed to federated SQL.</summary>
    public string? Alias { get; init; }

    /// <summary>Whether the first CSV row contains column headers. Defaults to true.</summary>
    public bool? HasHeader { get; init; }

    /// <summary>Single character CSV delimiter. Defaults to <c>,</c>.</summary>
    public string? Delimiter { get; init; }

    /// <summary>Excel sheet name. Defaults to the first sheet.</summary>
    public string? Sheet { get; init; }

    public IReadOnlyList<ColumnSchemaOverride>? SchemaOverride { get; init; }

    public DataSourceKind Kind() => SourceType.Trim().ToLowerInvariant() switch
    {
        "postgres" or "postgresql" => DataSourceKind.Postgres,
        "mysql" => DataSourceKind.MySql,
        "clickhouse" or "click_house" => DataSourceKind.ClickHouse,
        "mongodb" or "mongo" => DataSourceKind.MongoDb,
        "sqlite" or "sqlite3" => DataSourceKind.Sqlite,
        "csv" => DataSourceKind.Csv,
        "json" or "ndjson" => DataSourceKind.Json,
        "excel" or "xlsx" or "xls" => DataSourceKind.Excel,
        "parquet" or "pq" => DataSourceKind.Parquet,
        "file" or "files" or "folder" or "directory" => DataSourceKind.Files,
        _ => throw EngineException.UnsupportedDataSource(),
    };

    /// <summary>Filesystem path or S3 URL for a file source, validated to be present.</summary>
    public string RequirePath()
    {
        var path = Path?.Trim();
        if (string.IsNullOrEmpty(path))
        {
            throw EngineException.InvalidConnection("missing file path");
        }

        return path;
    }

    /// <summary>Builds a provider connection string for database kinds, mirroring the Rust engine.</summary>
    public string BuildConnectionString()
    {
        var kind = Kind();

        if (kind == DataSourceKind.MongoDb)
        {
            var raw = ConnectionString?.Trim();
            if (!string.IsNullOrEmpty(raw))
            {
                return raw;
            }
        }

        var host = RequireField(Host, "host");
        var port = Port ?? throw EngineException.InvalidConnection("missing port");
        var database = Uri.EscapeDataString(RequireField(Database, "database"));
        var username = Uri.EscapeDataString(RequireField(Username, "username"));
        var password = Uri.EscapeDataString(Password ?? string.Empty);

        return kind switch
        {
            DataSourceKind.Postgres => $"postgres://{username}:{password}@{host}:{port}/{database}",
            DataSourceKind.MySql => $"mysql://{username}:{password}@{host}:{port}/{database}",
            DataSourceKind.ClickHouse => $"http://{host}:{port}",
            DataSourceKind.MongoDb => $"mongodb://{username}:{password}@{host}:{port}/{database}",
            _ => throw EngineException.UnsupportedDataSource(),
        };
    }

    private static string RequireField(string? value, string field)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw EngineException.InvalidConnection($"missing {field}");
        }

        return trimmed;
    }
}

/// <summary>
/// Optional schema override for file sources. Useful for CSV date/timestamp/decimal
/// columns that would otherwise be inferred as strings.
/// </summary>
public sealed class ColumnSchemaOverride
{
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string ColumnType { get; init; } = string.Empty;

    public bool? Nullable { get; init; }

    public byte? Precision { get; init; }

    public sbyte? Scale { get; init; }
}
