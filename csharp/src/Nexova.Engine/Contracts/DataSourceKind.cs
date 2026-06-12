namespace Nexova.Engine.Contracts;

/// <summary>
/// Query guardrail defaults, mirrored from the Rust engine's <c>types.rs</c>.
/// </summary>
public static class EngineConstants
{
    public const uint DefaultLimit = 1_000;

    public const uint MaxLimit = 5_000;

    public const ulong DefaultTimeoutMs = 30_000;
}

public enum DataSourceKind
{
    Postgres,
    MySql,
    ClickHouse,
    MongoDb,
    Sqlite,
    Csv,
    Json,
    Excel,
    Parquet,
    Files,
}

public static class DataSourceKindExtensions
{
    /// <summary>Human readable table type label, mirrored from the Rust engine.</summary>
    public static string TableType(this DataSourceKind kind) => kind switch
    {
        DataSourceKind.Csv => "CSV",
        DataSourceKind.Json => "JSON",
        DataSourceKind.Excel => "EXCEL",
        DataSourceKind.Parquet => "PARQUET",
        DataSourceKind.Files => "FILE",
        DataSourceKind.Postgres => "POSTGRES",
        DataSourceKind.MySql => "MYSQL",
        DataSourceKind.ClickHouse => "CLICKHOUSE",
        DataSourceKind.MongoDb => "MONGODB",
        DataSourceKind.Sqlite => "SQLITE",
        _ => "UNKNOWN",
    };

    public static bool IsFileKind(this DataSourceKind kind) => kind is
        DataSourceKind.Csv or
        DataSourceKind.Json or
        DataSourceKind.Excel or
        DataSourceKind.Parquet or
        DataSourceKind.Files;

    public static bool IsDatabaseKind(this DataSourceKind kind) => !kind.IsFileKind();
}
