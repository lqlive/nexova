using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Vistora.Core.Entities;

public interface IContext
{
    DatabaseFacade Database { get; }

    DbSet<DataSource> DataSources { get; set; }

    DbSet<Dataset> Datasets { get; set; }

    DbSet<Chart> Charts { get; set; }

    DbSet<Dashboard> Dashboards { get; set; }

    /// <summary>
    /// Check whether a <see cref="DbUpdateException"/> is due to a SQL unique constraint violation.
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns>Whether the exception was caused by a SQL unique constraint violation.</returns>
    bool IsUniqueConstraintViolationException(DbUpdateException exception);

    /// <summary>
    /// Whether this database engine supports LINQ "Take" in subqueries.
    /// </summary>
    bool SupportsLimitInSubqueries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Applies any pending migrations for the context to the database.
    /// Creates the database if it does not already exist.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>A task that completes once migrations are applied.</returns>
    Task RunMigrationsAsync(CancellationToken cancellationToken);
}
