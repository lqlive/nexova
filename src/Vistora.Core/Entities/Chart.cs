namespace Vistora.Core.Entities;

public class Chart
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public Guid DatasetId { get; set; }

    public Dataset? Dataset { get; set; }

    public string VisualizationType { get; set; } = string.Empty;

    public Dictionary<string, object?> Configuration { get; set; } = new();
    public ICollection<DashboardChart> DashboardCharts { get; set; } = new List<DashboardChart>();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
