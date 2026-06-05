namespace Vistora.Core.Entities;

public class DashboardChart
{
    public Guid DashboardId { get; set; }

    public Dashboard? Dashboard { get; set; }

    public Guid ChartId { get; set; }

    public Chart? Chart { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int Order { get; set; }
}
