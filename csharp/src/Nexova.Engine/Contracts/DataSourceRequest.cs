namespace Nexova.Engine.Contracts;

/// <summary>Body for connection test and table listing endpoints.</summary>
public sealed class DataSourceRequest
{
    public DataSourceConnection DataSource { get; init; } = new();
}
