namespace Nexova.Engine.Contracts;

public sealed class QueryResult
{
    public IReadOnlyList<ColumnInfo> Columns { get; init; } = [];

    public IReadOnlyList<IReadOnlyList<object?>> Rows { get; init; } = [];

    public ulong RowCount { get; init; }

    public ulong DurationMs { get; init; }
}
