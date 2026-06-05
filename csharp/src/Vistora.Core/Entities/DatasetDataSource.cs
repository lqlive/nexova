namespace Vistora.Core.Entities;

public class DatasetDataSource
{
    public Guid DatasetId { get; set; }

    public Dataset? Dataset { get; set; }

    public Guid DataSourceId { get; set; }

    public DataSource? DataSource { get; set; }

    public string? Alias { get; set; }

    public int Order { get; set; }
}
