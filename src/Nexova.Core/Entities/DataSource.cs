namespace Nexova.Core.Entities;

public class DataSource
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DataSourceType Type { get; set; }
    public DataSourceConfiguration Configuration { get; set; } = new();
    public ICollection<DataSourceFileAsset> FileAssets { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
