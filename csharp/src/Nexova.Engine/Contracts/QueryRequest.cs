namespace Nexova.Engine.Contracts;

/// <summary>Body for single-source query and explain.</summary>
public sealed class QueryRequest
{
    public DataSourceConnection DataSource { get; init; } = new();

    public string Sql { get; init; } = string.Empty;

    public uint? Limit { get; init; }

    public ulong? TimeoutMs { get; init; }
}
