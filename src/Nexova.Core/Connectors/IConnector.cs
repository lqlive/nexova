using Apache.DataFusion;
using Nexova.Core.Entities;

namespace Nexova.Core.Connectors;

public interface IConnector
{
    DataSourceType Type { get; }

    Task RegisterAsync(SessionContext context, string tableName, DataSource dataSource,
        CancellationToken cancellationToken);
}