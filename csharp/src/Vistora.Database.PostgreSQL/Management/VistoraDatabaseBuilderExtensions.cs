using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vistora.Core.Entities;
using Vistora.Core.Store;
using Vistora.Database.PostgreSQL.Store;

namespace Vistora.Database.PostgreSQL.Management;

public static class VistoraDatabaseBuilderExtensions
{
    public static IServiceCollection AddPostgreSQLDatabase(
        this IServiceCollection services,
        string connectionString
    )
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
