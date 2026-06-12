using System.Collections.Concurrent;
using Nexova.Core.Entities;

namespace Nexova.Core.Store;

public sealed class InMemoryDataSourceStore : IDataSourceStore
{
    private readonly ConcurrentDictionary<Guid, DataSource> dataSources = [];

    public Task<bool> CreateAsync(DataSource dataSource, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (dataSources.Values.Any(source => source.Name == dataSource.Name))
        {
            return Task.FromResult(false);
        }

        dataSource.CreatedAt = DateTimeOffset.UtcNow;
        dataSource.UpdatedAt = dataSource.CreatedAt;
        return Task.FromResult(dataSources.TryAdd(dataSource.Id, dataSource));
    }

    public Task<DataSource?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        dataSources.TryGetValue(id, out var dataSource);
        return Task.FromResult(dataSource);
    }

    public Task<DataSource?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dataSource = dataSources.Values.SingleOrDefault(source => source.Name == name);
        return Task.FromResult(dataSource);
    }

    public Task<IReadOnlyList<DataSource>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<DataSource> items = dataSources.Values
            .OrderBy(source => source.Name)
            .ToList();

        return Task.FromResult(items);
    }

    public Task<bool> UpdateAsync(DataSource dataSource, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!dataSources.ContainsKey(dataSource.Id))
        {
            return Task.FromResult(false);
        }

        if (dataSources.Values.Any(source => source.Id != dataSource.Id && source.Name == dataSource.Name))
        {
            return Task.FromResult(false);
        }

        dataSource.UpdatedAt = DateTimeOffset.UtcNow;
        dataSources[dataSource.Id] = dataSource;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(dataSources.TryRemove(id, out _));
    }
}
