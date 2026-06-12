namespace Nexova.Engine.Contracts;

/// <summary>Body for federated (cross-source) query and explain.</summary>
public sealed class FederatedQueryRequest
{
    public IReadOnlyList<DataSourceConnection> DataSources { get; init; } = [];

    public string Sql { get; init; } = string.Empty;

    public uint? Limit { get; init; }

    public ulong? TimeoutMs { get; init; }
}
