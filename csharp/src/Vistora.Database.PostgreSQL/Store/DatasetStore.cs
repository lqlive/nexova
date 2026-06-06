using Microsoft.EntityFrameworkCore;
using Vistora.Core.Entities;
using Vistora.Core.Store;

namespace Vistora.Database.PostgreSQL.Store;

public sealed class DatasetStore(IContext context) : IDatasetStore
{
    public async Task<bool> CreateAsync(Dataset dataset, CancellationToken cancellationToken)
    {
        try
        {
            context.Datasets.Add(dataset);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (context.IsUniqueConstraintViolationException(exception))
        {
            return false;
        }
    }

    public async Task<Dataset?> GetAsync(Guid id, CancellationToken cancellationToken)
        => await context.Datasets
            .Include(dataset => dataset.DataSources)
            .Include(dataset => dataset.Columns)
            .Include(dataset => dataset.Charts)
            .SingleOrDefaultAsync(dataset => dataset.Id == id, cancellationToken);

    public async Task<Dataset?> GetByNameAsync(string name, CancellationToken cancellationToken)
        => await context.Datasets
            .Include(dataset => dataset.DataSources)
            .Include(dataset => dataset.Columns)
            .Include(dataset => dataset.Charts)
            .SingleOrDefaultAsync(dataset => dataset.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Dataset>> ListAsync(CancellationToken cancellationToken)
        => await context.Datasets
            .OrderBy(dataset => dataset.Name)
            .ToListAsync(cancellationToken);

    public async Task<bool> UpdateAsync(Dataset dataset, CancellationToken cancellationToken)
    {
        try
        {
            dataset.UpdatedAt = DateTimeOffset.UtcNow;
            context.Datasets.Update(dataset);
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
        var dataset = await context.Datasets
            .SingleOrDefaultAsync(value => value.Id == id, cancellationToken);

        if (dataset is null)
        {
            return false;
        }

        context.Datasets.Remove(dataset);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
