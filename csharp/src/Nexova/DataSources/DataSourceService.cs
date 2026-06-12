using ErrorOr;
using Nexova.Core.Entities;
using Nexova.Core.Storage;
using Nexova.Core.Store;
using Nexova.DataSources.Errors;
using Nexova.DataSources.Models;

namespace Nexova.DataSources;

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
        IStorageService storage,
        CancellationToken cancellationToken = default)
    {
        var dataSource = new DataSource
        {
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            Configuration = request.Configuration ?? new DataSourceConfiguration()
        };

        await InitializeFileContainerAsync(dataSource, storage, cancellationToken);

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

    public async Task<ErrorOr<DataSourceResponse>> AddFileAsync(
        Guid id,
        string fileName,
        string storagePath,
        string path,
        string contentType,
        long size,
        bool? hasHeader,
        string? delimiter,
        string? sheet,
        CancellationToken cancellationToken = default)
    {
        var dataSource = await dataSourceStore.GetAsync(id, cancellationToken);
        if (dataSource is null)
        {
            return DataSourceErrors.NotFound;
        }

        if (!IsFileDataSource(dataSource))
        {
            return DataSourceErrors.NotFileDataSource;
        }

        var tableName = TableNameFromFileName(fileName);
        if (dataSource.Files.Any(file =>
                string.Equals(file.FileName, fileName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(file.TableName, tableName, StringComparison.OrdinalIgnoreCase)))
        {
            return DataSourceErrors.FileAlreadyExists;
        }

        dataSource.Files.Add(new DataSourceFile
        {
            DataSourceId = id,
            FileName = fileName,
            StoragePath = storagePath,
            Path = path,
            ContentType = contentType,
            Size = size,
            TableName = tableName,
            FileType = FileTypeFromFileName(fileName),
            HasHeader = hasHeader,
            Delimiter = delimiter,
            Sheet = sheet
        });

        var updated = await dataSourceStore.UpdateAsync(dataSource, cancellationToken);
        if (!updated)
        {
            return DataSourceErrors.NameAlreadyExists;
        }

        return MapToResponse(dataSource);
    }

    public static bool IsFileDataSource(DataSourceResponse dataSource)
        => IsFileDataSource(dataSource.Type);

    private static bool IsFileDataSource(DataSource dataSource)
        => IsFileDataSource(dataSource.Type);

    private static bool IsFileDataSource(string type)
        => string.Equals(type, "files", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "file", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "folder", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "directory", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "csv", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "json", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "excel", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "xlsx", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "xls", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "parquet", StringComparison.OrdinalIgnoreCase);

    private static async Task InitializeFileContainerAsync(
        DataSource dataSource,
        IStorageService storage,
        CancellationToken cancellationToken)
    {
        if (!IsFileDataSource(dataSource)
            || !string.IsNullOrWhiteSpace(dataSource.Configuration.Path))
        {
            return;
        }

        var storagePath = string.IsNullOrWhiteSpace(dataSource.Configuration.StoragePath)
            ? $"data-sources/{dataSource.Id:N}"
            : dataSource.Configuration.StoragePath;
        var downloadUri = await storage.GetDownloadUriAsync(storagePath, cancellationToken);

        dataSource.Configuration.StoragePath = storagePath;
        dataSource.Configuration.Path = downloadUri.IsFile
            ? downloadUri.LocalPath
            : downloadUri.ToString();
    }

    private static DataSourceResponse MapToResponse(DataSource dataSource)
        => new(
            dataSource.Id,
            dataSource.Name,
            dataSource.Type,
            dataSource.Configuration,
            dataSource.Files
                .OrderBy(file => file.FileName)
                .Select(MapToResponse)
                .ToList(),
            dataSource.CreatedAt,
            dataSource.UpdatedAt);

    private static DataSourceFileResponse MapToResponse(DataSourceFile file)
        => new(
            file.Id,
            file.DataSourceId,
            file.FileName,
            file.StoragePath,
            file.Path,
            file.ContentType,
            file.Size,
            file.TableName,
            file.FileType,
            file.HasHeader,
            file.Delimiter,
            file.Sheet,
            file.CreatedAt);

    private static string FileTypeFromFileName(string fileName)
        => Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();

    private static string TableNameFromFileName(string fileName)
    {
        var raw = Path.GetFileNameWithoutExtension(fileName);
        var chars = raw
            .Select(character => char.IsAsciiLetterOrDigit(character) || character == '_' ? character : '_')
            .ToArray();
        var tableName = new string(chars).Trim('_');
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return "data";
        }

        return char.IsAsciiDigit(tableName[0])
            ? $"t_{tableName}"
            : tableName;
    }
}
