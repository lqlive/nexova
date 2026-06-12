using Microsoft.Extensions.DependencyInjection;
using Nexova.Core.Connectors;
using Nexova.Core.Entities;
using Nexova.Core.Management;

namespace Nexova.Connectors;

public static class DataFusionConnectorBuilderExtensions
{
    public static INexovaBuilder AddDataFusionConnectors(this INexovaBuilder builder)
    {
        var services = builder.Services;

        services.AddKeyedScoped<IConnector, FileConnector>(DataSourceType.File);
        services.AddKeyedScoped<IConnector, PostgreSqlConnector>(DataSourceType.PostgreSql);

        services.AddScoped<IQueryExecutor, DataFusionQueryExecutor>();
        return builder;
    }
}