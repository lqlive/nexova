using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexova.Core.Entities;
using Nexova.Core.Store;
using Nexova.Database.PostgreSQL.Store;

namespace Nexova.Database.PostgreSQL.Management;

public static class NexovaDatabaseBuilderExtensions
{
    public static IServiceCollection AddPostgreSQLDatabase(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var migrationsAssembly = typeof(PostgreSQLContext).GetTypeInfo().Assembly.GetName().Name;

        services.AddDbContext<PostgreSQLContext>(
            options => options.UseNpgsql(
                connectionString,
                sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly)
            )
        );

        services.TryAddScoped<IContext>(provider => provider.GetRequiredService<PostgreSQLContext>());
        services.TryAddTransient<IDataSourceStore, DataSourceStore>();
        services.TryAddTransient<IDatasetStore, DatasetStore>();

        return services;
    }
}
