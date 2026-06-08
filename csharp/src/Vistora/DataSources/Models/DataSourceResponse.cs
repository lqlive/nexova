using Vistora.Core.Entities;

namespace Vistora.DataSources.Models;

public sealed record DataSourceResponse(
    Guid Id,
    string Name,
    string Type,
    DataSourceConfiguration Configuration,
    IReadOnlyList<DataSourceFileResponse> Files,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record DataSourceFileResponse(
    Guid Id,
    Guid DataSourceId,
    string FileName,
    string StoragePath,
    string Path,
    string ContentType,
    long Size,
    string TableName,
    string FileType,
    bool? HasHeader,
    string? Delimiter,
    string? Sheet,
    DateTimeOffset CreatedAt);
