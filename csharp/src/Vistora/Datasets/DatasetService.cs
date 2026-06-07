using ErrorOr;
using Vistora.Core.Entities;
using Vistora.Core.Store;
using Vistora.Datasets.Errors;
using Vistora.Datasets.Models;

namespace Vistora.Datasets;

public sealed class DatasetService(IDatasetStore datasetStore)
{
    public async Task<IEnumerable<DatasetResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var datasets = await datasetStore.ListAsync(cancellationToken);
        return datasets.Select(MapToResponse);
    }

    public async Task<DatasetResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dataset = await datasetStore.GetAsync(id, cancellationToken);
        return dataset is null ? null : MapToResponse(dataset);
    }

    public async Task<ErrorOr<DatasetResponse>> CreateAsync(
        DatasetRequest request,
        CancellationToken cancellationToken = default)
    {
        var dataset = CreateDataset(request);
        var created = await datasetStore.CreateAsync(dataset, cancellationToken);
        if (!created)
        {
            return DatasetErrors.NameAlreadyExists;
        }

        return MapToResponse(dataset);
    }

    public async Task<ErrorOr<DatasetResponse>> UpdateAsync(
        Guid id,
        DatasetRequest request,
        CancellationToken cancellationToken = default)
    {
        var dataset = await datasetStore.GetAsync(id, cancellationToken);
        if (dataset is null)
        {
            return DatasetErrors.NotFound;
        }

        dataset.Name = request.Name.Trim();
        dataset.Sql = request.Sql.Trim();
        dataset.Description = request.Description;
        dataset.DataSources = MapDataSourceLinks(request).ToList();
        dataset.Columns = MapColumns(request).ToList();

        var updated = await datasetStore.UpdateAsync(dataset, cancellationToken);
        if (!updated)
        {
            return DatasetErrors.NameAlreadyExists;
        }

        return MapToResponse(dataset);
    }

    public async Task<ErrorOr<Success>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await datasetStore.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return DatasetErrors.NotFound;
        }

        return Result.Success;
    }

    private static Dataset CreateDataset(DatasetRequest request)
        => new()
        {
            Name = request.Name.Trim(),
            Sql = request.Sql.Trim(),
            Description = request.Description,
            DataSources = MapDataSourceLinks(request).ToList(),
            Columns = MapColumns(request).ToList()
        };

    private static IEnumerable<DatasetDataSource> MapDataSourceLinks(DatasetRequest request)
        => (request.DataSources ?? [])
            .Select(link => new DatasetDataSource
            {
                DataSourceId = link.DataSourceId,
                Alias = link.Alias,
                Order = link.Order
            });

    private static IEnumerable<DatasetColumn> MapColumns(DatasetRequest request)
        => (request.Columns ?? [])
            .Select(column => new DatasetColumn
            {
                Name = column.Name.Trim(),
                Type = column.Type.Trim(),
                Nullable = column.Nullable,
                Precision = column.Precision,
                Scale = column.Scale,
                Ordinal = column.Ordinal
            });

    private static DatasetResponse MapToResponse(Dataset dataset)
        => new(
            dataset.Id,
            dataset.Name,
            dataset.Sql,
            dataset.Description,
            dataset.DataSources
                .OrderBy(link => link.Order)
                .Select(link => new DatasetDataSourceResponse(
                    link.DataSourceId,
                    link.Alias,
                    link.Order)),
            dataset.Columns
                .OrderBy(column => column.Ordinal)
                .Select(column => new DatasetColumnResponse(
                    column.Id,
                    column.Name,
                    column.Type,
                    column.Nullable,
                    column.Precision,
                    column.Scale,
                    column.Ordinal)),
            dataset.CreatedAt,
            dataset.UpdatedAt);
}
