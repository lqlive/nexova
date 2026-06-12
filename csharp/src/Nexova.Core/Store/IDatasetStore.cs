using Nexova.Core.Entities;

namespace Nexova.Core.Store;

public interface IDatasetStore
{
    Task<bool> CreateAsync(Dataset dataset, CancellationToken cancellationToken);

    Task<Dataset?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<Dataset?> GetByNameAsync(string name, CancellationToken cancellationToken);

    Task<IReadOnlyList<Dataset>> ListAsync(CancellationToken cancellationToken);

    Task<bool> UpdateAsync(Dataset dataset, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
