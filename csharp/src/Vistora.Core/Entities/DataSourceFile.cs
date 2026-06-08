namespace Vistora.Core.Entities;

public class DataSourceFile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DataSourceId { get; set; }

    public DataSource DataSource { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;

    public string StoragePath { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long Size { get; set; }

    public string TableName { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;

    public bool? HasHeader { get; set; }

    public string? Delimiter { get; set; }

    public string? Sheet { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
