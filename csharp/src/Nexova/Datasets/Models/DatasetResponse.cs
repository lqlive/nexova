namespace Nexova.Datasets.Models;

public sealed record DatasetResponse(
    Guid Id,
    string Name,
    string Sql,
    string? Description,
    IEnumerable<DatasetDataSourceResponse> DataSources,
    IEnumerable<DatasetColumnResponse> Columns,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record DatasetDataSourceResponse(
    Guid DataSourceId,
    string? Alias,
    int Order);

public sealed record DatasetColumnResponse(
    Guid Id,
    string Name,
    string Type,
    bool Nullable,
    int? Precision,
    int? Scale,
    int Ordinal);
