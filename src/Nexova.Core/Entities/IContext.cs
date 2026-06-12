using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Nexova.Core.Entities;

public interface IContext
{
    DatabaseFacade Database { get; }
    DbSet<DataSource> DataSources { get; set; }
    DbSet<Dataset> Datasets { get; set; }
    DbSet<User> Users { get; set; }
    bool SupportsLimitInSubqueries { get; }
    bool IsUniqueConstraintViolationException(DbUpdateException exception);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
