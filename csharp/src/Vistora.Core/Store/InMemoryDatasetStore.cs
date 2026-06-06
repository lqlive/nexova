using System.Collections.Concurrent;
using Vistora.Core.Entities;

namespace Vistora.Core.Store;

public sealed class InMemoryDatasetStore : IDatasetStore
{
    private readonly ConcurrentDictionary<Guid, Dataset> datasets = [];

    public Task<bool> CreateAsync(Dataset dataset, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (datasets.Values.Any(value => value.Name == dataset.Name))
        {
            return Task.FromResult(false);
        }

        dataset.CreatedAt = DateTimeOffset.UtcNow;
        dataset.UpdatedAt = dataset.CreatedAt;
        return Task.FromResult(datasets.TryAdd(dataset.Id, dataset));
    }

    public Task<Dataset?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        datasets.TryGetValue(id, out var dataset);
        return Task.FromResult(dataset);
    }

    public Task<Dataset?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dataset = datasets.Values.SingleOrDefault(value => value.Name == name);
        return Task.FromResult(dataset);
    }

    public Task<IReadOnlyList<Dataset>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<Dataset> items = datasets.Values
            .OrderBy(value => value.Name)
            .ToList();

        return Task.FromResult(items);
    }

    public Task<bool> UpdateAsync(Dataset dataset, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!datasets.ContainsKey(dataset.Id))
        {
            return Task.FromResult(false);
        }

        if (datasets.Values.Any(value => value.Id != dataset.Id && value.Name == dataset.Name))
        {
            return Task.FromResult(false);
        }

        dataset.UpdatedAt = DateTimeOffset.UtcNow;
        datasets[dataset.Id] = dataset;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(datasets.TryRemove(id, out _));
    }
}
