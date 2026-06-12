using Microsoft.EntityFrameworkCore;

namespace Nexova.Core.Entities;

public abstract class AbstractContext<TContext> : DbContext, IContext
    where TContext : DbContext
{
    public DbSet<DataSource> DataSources { get; set; } 
    public DbSet<Dataset> Datasets { get; set; }
    public DbSet<User> Users { get; set; }
    public bool SupportsLimitInSubqueries => true;
    public abstract bool IsUniqueConstraintViolationException(DbUpdateException exception);
}
