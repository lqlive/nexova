namespace Vistora.Core.Entities;

public class DatasetColumn
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DatasetId { get; set; }

    public Dataset? Dataset { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public bool Nullable { get; set; }

    public int? Precision { get; set; }

    public int? Scale { get; set; }

    public int Ordinal { get; set; }
}
