using Nexova.Core.Stores;
using Nexova.Core.Entities;
using Nexova.Core.Configuration;
using Nexova.Core.Management;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Nexova.Database.PostgreSql;

public static class PostgreSqlDatabaseBuilderExtensions
{
    public const string Name = "PostgreSql";

    public static INexovaBuilder AddPostgreSqlDatabase(this INexovaBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var services = builder.Services;

        services.AddDbContext<IContext, PostgreSqlContext>((provider, options) =>
        {
            var databaseOptions = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseNpgsql(databaseOptions.ConnectionString);
        });

        services.AddKeyedScoped<IDataSourceStore, PostgreSqlDataSourceStore>(Name);
        services.AddKeyedScoped<IDatasetStore, PostgreSqlDatasetStore>(Name);

        return builder;
    }
}