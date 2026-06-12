namespace Nexova.Core.Entities;

public class Dataset
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Sql { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<DatasetDataSource> DataSources { get; set; } = new List<DatasetDataSource>();

    public ICollection<DatasetColumn> Columns { get; set; } = new List<DatasetColumn>();

    public ICollection<Chart> Charts { get; set; } = new List<Chart>();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
