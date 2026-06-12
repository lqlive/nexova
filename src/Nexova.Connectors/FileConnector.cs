using Apache.DataFusion;
using Nexova.Core.Connectors;
using Nexova.Core.Entities;

namespace Nexova.Connectors;

public sealed class FileConnector : IConnector
{
    public DataSourceType Type => DataSourceType.File;

    public Task RegisterAsync(SessionContext context, string tableName,
        DataSource dataSource, CancellationToken cancellationToken)
    {
        foreach (var asset in dataSource.FileAssets)
        {
            
        }
        return Task.CompletedTask;
    }
}