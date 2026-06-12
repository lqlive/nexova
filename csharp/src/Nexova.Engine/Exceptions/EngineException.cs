using System.Net;

namespace Nexova.Engine.Exceptions;

/// <summary>
/// Engine error taxonomy, mirrored from the Rust engine's <c>EngineError</c> enum.
/// </summary>
public enum EngineErrorKind
{
    UnsupportedDataSource,
    InvalidConnection,
    SqlSyntax,
    ReadOnlyViolation,
    TableNotFound,
    ColumnNotFound,
    QueryExecution,
    InvalidSql,
    Timeout,
    FileSource,
}

/// <summary>
/// Typed engine failure that carries the stable error contract (code/category/detail)
/// expected by the API layer and the existing front-end. Mirrors the Rust engine's
/// <c>EngineError</c> classification and JSON shape.
/// </summary>
public sealed class EngineException : Exception
{
    public EngineException(EngineErrorKind kind, string detail, Exception? innerException = null)
        : base(detail, innerException)
    {
        Kind = kind;
        Detail = detail;
    }

    public EngineErrorKind Kind { get; }

    public string Detail { get; }

    public string Code => Kind switch
    {
        EngineErrorKind.UnsupportedDataSource => "unsupported_data_source",
        EngineErrorKind.InvalidConnection => "connection_failed",
        EngineErrorKind.SqlSyntax or EngineErrorKind.InvalidSql => "sql_syntax_error",
        EngineErrorKind.ReadOnlyViolation => "read_only_violation",
        EngineErrorKind.TableNotFound => "table_not_found",
        EngineErrorKind.ColumnNotFound => "column_not_found",
        EngineErrorKind.QueryExecution => "query_execution_failed",
        EngineErrorKind.Timeout => "query_timeout",
        EngineErrorKind.FileSource => "file_source_error",
        _ => "engine_error",
    };

    public string Category => Kind switch
    {
        EngineErrorKind.UnsupportedDataSource => "data_source",
        EngineErrorKind.InvalidConnection => "connection_failed",
        EngineErrorKind.SqlSyntax or EngineErrorKind.InvalidSql => "sql_syntax_error",
        EngineErrorKind.ReadOnlyViolation => "permission_or_read_only",
        EngineErrorKind.TableNotFound => "table_not_found",
        EngineErrorKind.ColumnNotFound => "column_not_found",
        EngineErrorKind.QueryExecution => "query_execution",
        EngineErrorKind.Timeout => "timeout",
        EngineErrorKind.FileSource => "file_source",
        _ => "engine",
    };

    public string UserMessage => Kind switch
    {
        EngineErrorKind.UnsupportedDataSource => "Unsupported data source type",
        EngineErrorKind.InvalidConnection => "Connection failed",
        EngineErrorKind.SqlSyntax or EngineErrorKind.InvalidSql => "SQL syntax error",
        EngineErrorKind.ReadOnlyViolation => "Permission or read-only restriction",
        EngineErrorKind.TableNotFound => "Table not found",
        EngineErrorKind.ColumnNotFound => "Column not found",
        EngineErrorKind.QueryExecution => "Query execution failed",
        EngineErrorKind.Timeout => "Query timed out",
        EngineErrorKind.FileSource => "File source error",
        _ => "Engine error",
    };

    public HttpStatusCode StatusCode => Kind switch
    {
        EngineErrorKind.Timeout => HttpStatusCode.RequestTimeout,
        _ => HttpStatusCode.BadRequest,
    };

    public static EngineException UnsupportedDataSource() =>
        new(EngineErrorKind.UnsupportedDataSource, "unsupported data source type");

    public static EngineException InvalidConnection(string detail) =>
        new(EngineErrorKind.InvalidConnection, detail);

    public static EngineException SqlSyntax(string detail) =>
        new(EngineErrorKind.SqlSyntax, detail);

    public static EngineException ReadOnlyViolation(string detail) =>
        new(EngineErrorKind.ReadOnlyViolation, detail);

    public static EngineException TableNotFound(string detail) =>
        new(EngineErrorKind.TableNotFound, detail);

    public static EngineException ColumnNotFound(string detail) =>
        new(EngineErrorKind.ColumnNotFound, detail);

    public static EngineException QueryExecution(string detail) =>
        new(EngineErrorKind.QueryExecution, detail);

    public static EngineException Timeout() =>
        new(EngineErrorKind.Timeout, "query timed out");

    public static EngineException FileSource(string detail) =>
        new(EngineErrorKind.FileSource, detail);
}
