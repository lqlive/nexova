using Nexova.Core.Entities;
using Nexova.Core.Stores;

namespace Nexova.Database.PostgreSql;

public class PostgreSqlDatasetStore : IDatasetStore
{
    public Task<bool> CreateAsync(Dataset dataset, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Dataset?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Dataset>> ListAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateAsync(Dataset dataset, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}