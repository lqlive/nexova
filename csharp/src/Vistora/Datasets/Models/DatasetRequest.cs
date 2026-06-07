namespace Vistora.Datasets.Models;

public sealed record DatasetRequest(
    string Name,
    string Sql,
    string? Description,
    IReadOnlyList<DatasetDataSourceRequest>? DataSources,
    IReadOnlyList<DatasetColumnRequest>? Columns);

public sealed record DatasetDataSourceRequest(
    Guid DataSourceId,
    string? Alias,
    int Order);

public sealed record DatasetColumnRequest(
    string Name,
    string Type,
    bool Nullable,
    int? Precision,
    int? Scale,
    int Ordinal);
