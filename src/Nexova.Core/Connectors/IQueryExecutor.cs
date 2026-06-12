using Nexova.Core.Entities;

namespace Nexova.Core.Connectors;

public interface IQueryExecutor
{
    Task<QueryResult> ExecuteAsync(
        string sql,
        IReadOnlyCollection<DataSource> dataSources,
        CancellationToken cancellationToken = default);
}