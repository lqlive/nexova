using System.Text.Json.Serialization;

namespace Nexova.Engine.Contracts;

public sealed class TableInfo
{
    public string? Schema { get; init; }

    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string TableType { get; init; } = string.Empty;
}
