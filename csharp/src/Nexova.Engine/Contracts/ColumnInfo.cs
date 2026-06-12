using System.Text.Json.Serialization;

namespace Nexova.Engine.Contracts;

public sealed class ColumnInfo
{
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string ColumnType { get; init; } = string.Empty;

    public bool Nullable { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public byte? Precision { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public sbyte? Scale { get; init; }
}
