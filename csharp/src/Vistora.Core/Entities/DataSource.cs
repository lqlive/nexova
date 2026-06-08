namespace Vistora.Core.Entities;

public class DataSource
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public DataSourceConfiguration Configuration { get; set; } = new();

    public ICollection<DataSourceFile> Files { get; set; } = [];

    public ICollection<DatasetDataSource> Datasets { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}