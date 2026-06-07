using ErrorOr;
using Vistora.Core.Entities;
using Vistora.Core.Store;
using Vistora.DataSources.Errors;
using Vistora.DataSources.Models;

namespace Vistora.DataSources;

public sealed class DataSourceService(IDataSourceStore dataSourceStore)
{
    public async Task<IEnumerable<DataSourceResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var dataSources = await dataSourceStore.ListAsync(cancellationToken);
        return dataSources.Select(MapToResponse);
    }

    public async Task<DataSourceResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dataSource = await dataSourceStore.GetAsync(id, cancellationToken);
        return dataSource is null ? null : MapToResponse(dataSource);
    }

    public async Task<ErrorOr<DataSourceResponse>> CreateAsync(
        DataSourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var dataSource = new DataSource
        {
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            Configuration = request.Configuration ?? new DataSourceConfiguration()
        };

        var created = await dataSourceStore.CreateAsync(dataSource, cancellationToken);
        if (!created)
        {
            return DataSourceErrors.NameAlreadyExists;
        }

        return MapToResponse(dataSource);
    }

    public async Task<ErrorOr<DataSourceResponse>> UpdateAsync(
        Guid id,
        DataSourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var dataSource = await dataSourceStore.GetAsync(id, cancellationToken);
        if (dataSource is null)
        {
            return DataSourceErrors.NotFound;
        }

        dataSource.Name = request.Name.Trim();
        dataSource.Type = request.Type.Trim();
        dataSource.Configuration = request.Configuration ?? new DataSourceConfiguration();

        var updated = await dataSourceStore.UpdateAsync(dataSource, cancellationToken);
        if (!updated)
        {
            return DataSourceErrors.NameAlreadyExists;
        }

        return MapToResponse(dataSource);
    }

    public async Task<ErrorOr<Success>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await dataSourceStore.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return DataSourceErrors.NotFound;
        }

        return Result.Success;
    }

    private static DataSourceResponse MapToResponse(DataSource dataSource)
        => new(
            dataSource.Id,
            dataSource.Name,
            dataSource.Type,
            dataSource.Configuration,
            dataSource.CreatedAt,
            dataSource.UpdatedAt);
}
