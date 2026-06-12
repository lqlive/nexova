using Nexova.Core.Entities;

namespace Nexova.Core.Stores;

public interface IDataSourceStore
{
    Task<bool> CreateAsync(DataSource dataSource, CancellationToken cancellationToken);
    Task<DataSource?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<DataSource>> ListAsync(CancellationToken cancellationToken);
    Task<bool> UpdateAsync(DataSource dataSource, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
