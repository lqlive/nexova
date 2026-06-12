using System.Diagnostics;
using Apache.Arrow;
using Apache.DataFusion;
using Nexova.Engine.Arrow;
using Nexova.Engine.Contracts;
using Nexova.Engine.Exceptions;

namespace Nexova.Engine.Execution;

/// <summary>
/// Executes a (already validated and limited) SQL statement against a context and materialises the
/// result, mirroring the Rust engine's <c>files::query</c> / <c>providers::query</c> execution path.
/// </summary>
public static class QueryExecutor
{
    public static async Task<QueryResult> ExecuteAsync(
        SessionContext context, string sql, TimeSpan timeout, Stopwatch started, CancellationToken cancellationToken)
    {
        try
        {
            using var dataFrame = context.Sql(sql);
            var columns = ArrowResultMapper.Columns(dataFrame.Schema());
            var batches = await CollectAsync(dataFrame, timeout, cancellationToken);
            try
            {
                var rows = ArrowResultMapper.Rows(columns, batches);
                return new QueryResult
                {
                    Columns = columns,
                    Rows = rows,
                    RowCount = (ulong)rows.Count,
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

    internal static async Task<List<RecordBatch>> CollectAsync(
        DataFrame dataFrame, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var collect = Task.Run(async () =>
        {
            var batches = new List<RecordBatch>();
            using var reader = dataFrame.Collect();
            while (await reader.ReadNextRecordBatchAsync(cancellationToken) is { } batch)
            {
                batches.Add(batch);
            }

            return batches;
        }, cancellationToken);

        try
        {
            return await collect.WaitAsync(timeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            throw EngineException.Timeout();
        }
    }
}
