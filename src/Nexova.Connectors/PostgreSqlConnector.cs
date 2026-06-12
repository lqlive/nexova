using Apache.DataFusion;
using Nexova.Core.Connectors;
using Nexova.Core.Entities;

namespace Nexova.Connectors;

public sealed class PostgreSqlConnector : IConnector
{
    public DataSourceType Type => DataSourceType.PostgreSql;

    public Task RegisterAsync(SessionContext context, string tableName,
        DataSource dataSource, CancellationToken cancellationToken)
    {
        var options = new PostgresTableOptions(
            dataSource.Configuration.ConnectionString!,
            tableName);

        context.RegisterPostgres(tableName, options);
        return Task.CompletedTask;
    }
}