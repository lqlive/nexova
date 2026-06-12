using Apache.DataFusion;
using Microsoft.Extensions.DependencyInjection;
using Nexova.Core.Connectors;
using Nexova.Core.Entities;

namespace Nexova.Connectors;

public sealed class DataFusionQueryExecutor(IServiceProvider serviceProvider) : IQueryExecutor
{
    public async Task<QueryResult> ExecuteAsync(string sql,
        IReadOnlyCollection<DataSource> dataSources, CancellationToken cancellationToken)
    {
        using var context = new SessionContext();

        foreach (var dataSource in dataSources)
        {
            var connector = serviceProvider.GetRequiredKeyedService<IConnector>(dataSource.Type);
            await connector.RegisterAsync(context, dataSource.Name, dataSource, cancellationToken);
        }

        using var dataFrame = context.Sql(sql);
        using var reader = dataFrame.ExecuteStream();

        while (await reader.ReadNextRecordBatchAsync(cancellationToken) is { } batch)
        {
           
        }
        return new QueryResult();
    }
}