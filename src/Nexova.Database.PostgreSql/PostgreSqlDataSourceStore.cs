using Nexova.Core.Entities;
using Nexova.Core.Stores;

namespace Nexova.Database.PostgreSql;

public class PostgreSqlDataSourceStore : IDataSourceStore
{
    public Task<bool> CreateAsync(DataSource dataSource, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<DataSource?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<DataSource>> ListAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateAsync(DataSource dataSource, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
