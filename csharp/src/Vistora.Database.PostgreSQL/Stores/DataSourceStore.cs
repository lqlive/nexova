using Microsoft.EntityFrameworkCore;
using Vistora.Core.Entities;
using Vistora.Core.Store;

namespace Vistora.Database.PostgreSQL.Stores;

public sealed class DataSourceStore(IContext context) : IDataSourceStore
{
    public async Task<bool> CreateAsync(DataSource dataSource, CancellationToken cancellationToken)
    {
        try
        {
            context.DataSources.Add(dataSource);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (context.IsUniqueConstraintViolationException(exception))
        {
            return false;
        }
    }

    public async Task<DataSource?> GetAsync(Guid id, CancellationToken cancellationToken)
        => await context.DataSources
            .Include(source => source.Datasets)
            .SingleOrDefaultAsync(source => source.Id == id, cancellationToken);

    public async Task<DataSource?> GetByNameAsync(string name, CancellationToken cancellationToken)
        => await context.DataSources
            .Include(source => source.Datasets)
            .SingleOrDefaultAsync(source => source.Name == name, cancellationToken);

    public async Task<IReadOnlyList<DataSource>> ListAsync(CancellationToken cancellationToken)
        => await context.DataSources
            .OrderBy(source => source.Name)
            .ToListAsync(cancellationToken);

    public async Task<bool> UpdateAsync(DataSource dataSource, CancellationToken cancellationToken)
    {
        try
        {
            dataSource.UpdatedAt = DateTimeOffset.UtcNow;
            context.DataSources.Update(dataSource);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (context.IsUniqueConstraintViolationException(exception))
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var dataSource = await context.DataSources
            .SingleOrDefaultAsync(source => source.Id == id, cancellationToken);

        if (dataSource is null)
        {
            return false;
        }

        context.DataSources.Remove(dataSource);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
