using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;
using Nexova.Core.Entities;

namespace Nexova.Database.PostgreSQL;

public sealed class PostgreSQLContext(DbContextOptions<PostgreSQLContext> options)
    : AbstractContext<PostgreSQLContext>(options)
{
    public override bool IsUniqueConstraintViolationException(DbUpdateException exception)
        => exception.InnerException is PostgresException postgresException
           && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
}

public sealed class PostgreSQLContextDesignFactory : IDesignTimeDbContextFactory<PostgreSQLContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Username=postgres;Password=123456;Database=nexova";

    public PostgreSQLContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PostgreSQLContext>();
        var migrationsAssembly = typeof(PostgreSQLContextDesignFactory).GetTypeInfo().Assembly.GetName().Name;
        var connectionString = GetConnectionString(args);

        optionsBuilder.UseNpgsql(
            connectionString,
            options => options.MigrationsAssembly(migrationsAssembly)
        );

        return new PostgreSQLContext(optionsBuilder.Options);
    }

    private static string GetConnectionString(IReadOnlyList<string> args)
    {
        if (args.Count > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            return args[0];
        }

        return Environment.GetEnvironmentVariable("VISTORA_POSTGRESQL_CONNECTION_STRING")
               ?? DefaultConnectionString;
    }
}
