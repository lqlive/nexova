namespace Vistora.DataSources.Models;

public sealed record FileUploadResponse(
    string FileName,
    string ContentType,
    long Size,
    string StoragePath,
    string Path);

public sealed record AddDataSourceFileRequest(
    string FileName,
    string ContentType,
    long Size,
    string StoragePath,
    string Path,
    bool? HasHeader,
    string? Delimiter,
    string? Sheet);
