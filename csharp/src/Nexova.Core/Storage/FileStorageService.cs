using Microsoft.Extensions.Options;
using Nexova.Core.Configuration;

namespace Nexova.Core.Storage;

public class FileStorageService : IStorageService
{
    private const int DefaultCopyBufferSize = 81920;

    private readonly string _storePath;

    public FileStorageService(IOptionsSnapshot<FileSystemStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Value.Path))
        {
            throw new ArgumentException("Storage path is required.", nameof(options));
        }

        _storePath = Path.GetFullPath(options.Value.Path);
        if (!_storePath.EndsWith(Path.DirectorySeparatorChar))
        {
            _storePath += Path.DirectorySeparatorChar;
        }
    }

    public Task<Stream> GetAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = GetFullPath(path);
        Stream content = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        return Task.FromResult(content);
    }

    public Task<Uri> GetDownloadUriAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = new Uri(GetFullPath(path));

        return Task.FromResult(result);
    }

    public async Task<StoragePutResult> PutAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type is required.", nameof(contentType));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException("Path must include a directory.", nameof(path));

        Directory.CreateDirectory(directory);

        try
        {
            await using var fileStream = File.Open(fullPath, FileMode.CreateNew);
            await content.CopyToAsync(fileStream, DefaultCopyBufferSize, cancellationToken);

            return StoragePutResult.Success;
        }
        catch (IOException) when (File.Exists(fullPath))
        {
            await using var targetStream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            ResetPositionIfSupported(content);

            return await ContentMatchesAsync(content, targetStream, cancellationToken)
                ? StoragePutResult.AlreadyExists
                : StoragePutResult.Conflict;
        }
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            File.Delete(GetFullPath(path));
        }
        catch (DirectoryNotFoundException)
        {
        }

        return Task.CompletedTask;
    }

    private string GetFullPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        var fullPath = Path.GetFullPath(Path.Combine(_storePath, path));

        if (!fullPath.StartsWith(_storePath, PathComparison) ||
            fullPath.Length == _storePath.Length)
        {
            throw new ArgumentException("Path resolves outside store path.", nameof(path));
        }

        return fullPath;
    }

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    private static void ResetPositionIfSupported(Stream stream)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
    }

    private static async Task<bool> ContentMatchesAsync(
        Stream source,
        Stream target,
        CancellationToken cancellationToken)
    {
        if (source.CanSeek && target.CanSeek && source.Length != target.Length)
        {
            return false;
        }

        var sourceBuffer = new byte[DefaultCopyBufferSize];
        var targetBuffer = new byte[DefaultCopyBufferSize];

        while (true)
        {
            var sourceRead = await source.ReadAsync(sourceBuffer, cancellationToken);
            var targetRead = await target.ReadAsync(targetBuffer, cancellationToken);

            if (sourceRead != targetRead)
            {
                return false;
            }

            if (sourceRead == 0)
            {
                return true;
            }

            if (!sourceBuffer.AsSpan(0, sourceRead).SequenceEqual(targetBuffer.AsSpan(0, targetRead)))
            {
                return false;
            }
        }
    }
}
