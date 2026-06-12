using Nexova.Core.Stores;

namespace Nexova.DataSources;

public sealed class DataSourceService(IDataSourceStore dataSourceStore)
{
    public async Task<IEnumerable<Core.Entities.DataSource>> ListAsync(CancellationToken cancellationToken = default)
    {
        var dataSources = await dataSourceStore.ListAsync(cancellationToken);
        return dataSources;
    }
}