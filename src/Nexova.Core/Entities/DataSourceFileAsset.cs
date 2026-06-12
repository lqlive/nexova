namespace Nexova.Core.Entities;

public class DataSourceFileAsset
{
    public Guid Id { get; set; }
    public Guid DataSourceId { get; set; }
    public required DataSource DataSource { get; set; }
    public required string FileName { get; set; }
    public required string StoragePath { get; set; }
    public required string ContentType { get; set; }
    public long Size { get; set; }
    public bool? HasHeader { get; set; }
    public string? Delimiter { get; set; }
    public string? Sheet { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
