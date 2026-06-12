using Nexova.Engine.Contracts;

namespace Nexova.Engine;

/// <summary>
/// The single public entry point of the engine, mirroring the set of <c>pub fn</c> exposed by the
/// Rust engine's <c>executor.rs</c>. The host application (Nexova) wires its HTTP layer to this.
/// </summary>
public interface IQueryEngine
{
    Task TestConnectionAsync(DataSourceConnection source, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TableInfo>> ListTablesAsync(DataSourceConnection source, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ColumnInfo>> ListColumnsAsync(
        DataSourceConnection source, string? schema, string table, CancellationToken cancellationToken = default);

    Task<QueryResult> QueryAsync(QueryRequest request, CancellationToken cancellationToken = default);

    Task<ExplainResult> ExplainAsync(QueryRequest request, CancellationToken cancellationToken = default);

    Task<QueryResult> FederatedQueryAsync(FederatedQueryRequest request, CancellationToken cancellationToken = default);

    Task<ExplainResult> ExplainFederatedAsync(FederatedQueryRequest request, CancellationToken cancellationToken = default);
}
