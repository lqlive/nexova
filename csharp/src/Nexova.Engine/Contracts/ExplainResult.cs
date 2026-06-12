namespace Nexova.Engine.Contracts;

public sealed class ExplainResult
{
    public string? LogicalPlan { get; init; }

    public string? PhysicalPlan { get; init; }

    public IReadOnlyList<ExplainPlanInfo> Plans { get; init; } = [];

    public ulong DurationMs { get; init; }
}
