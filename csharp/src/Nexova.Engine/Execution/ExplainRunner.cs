using System.Diagnostics;
using Apache.Arrow;
using Apache.DataFusion;
using Nexova.Engine.Contracts;
using Nexova.Engine.Exceptions;

namespace Nexova.Engine.Execution;

/// <summary>
/// Runs <c>EXPLAIN VERBOSE</c> and parses the two-column (plan_type, plan) output, mirroring the
/// Rust engine's <c>explain_context</c>.
/// </summary>
public static class ExplainRunner
{
    public static async Task<ExplainResult> ExplainAsync(
        SessionContext context, string sql, TimeSpan timeout, Stopwatch started, CancellationToken cancellationToken)
    {
        try
        {
            using var dataFrame = context.Sql($"EXPLAIN VERBOSE {sql.Trim()}");
            var batches = await QueryExecutor.CollectAsync(dataFrame, timeout, cancellationToken);
            try
            {
                var plans = ParsePlans(batches);
                return new ExplainResult
                {
                    LogicalPlan = FindPlan(plans, "logical_plan"),
                    PhysicalPlan = FindPlan(plans, "physical_plan"),
                    Plans = plans,
                    DurationMs = (ulong)started.ElapsedMilliseconds,
                };
            }
            finally
            {
                foreach (var batch in batches)
                {
                    batch.Dispose();
                }
            }
        }
        catch (EngineException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw EngineErrorClassifier.Classify(exception);
        }
    }

    private static List<ExplainPlanInfo> ParsePlans(IReadOnlyList<RecordBatch> batches)
    {
        var plans = new List<ExplainPlanInfo>();
        foreach (var batch in batches)
        {
            if (batch.ColumnCount < 2 || batch.Column(0) is not StringArray planTypes || batch.Column(1) is not StringArray planValues)
            {
                continue;
            }

            for (var row = 0; row < batch.Length; row++)
            {
                var planType = planTypes.GetString(row);
                var plan = planValues.GetString(row);
                if (planType is null || plan is null)
                {
                    continue;
                }

                plans.Add(new ExplainPlanInfo { PlanType = planType, Plan = plan });
            }
        }

        return plans;
    }

    private static string? FindPlan(IEnumerable<ExplainPlanInfo> plans, string planType) =>
        plans.FirstOrDefault(plan => plan.PlanType == planType)?.Plan;
}
