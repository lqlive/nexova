namespace Nexova.Engine.Contracts;

/// <summary>Body for column listing.</summary>
public sealed class SchemaColumnsRequest
{
    public DataSourceConnection DataSource { get; init; } = new();

    public string? Schema { get; init; }

    public string Table { get; init; } = string.Empty;
}
