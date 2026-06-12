using System.Diagnostics;
using Apache.DataFusion;
using Nexova.Engine.Arrow;
using Nexova.Engine.Contracts;
using Nexova.Engine.Exceptions;
using Nexova.Engine.Execution;
using Nexova.Engine.Sessions;
using Nexova.Engine.Sources.Databases;
using Nexova.Engine.Sources.Files;
using Nexova.Engine.Sql;

namespace Nexova.Engine;

/// <summary>
/// Orchestrates validation, context building, execution and result mapping. Faithful port of the
/// Rust engine's <c>executor.rs</c> coordination logic.
/// </summary>
public sealed class QueryEngine : IQueryEngine
{
    private readonly SessionRegistry registry;
    private readonly SessionContextFactory contexts;
    private readonly DatabaseSourceRegistrar databases;
    private readonly FederatedContextBuilder federated;

    public QueryEngine(
        SessionRegistry registry,
        SessionContextFactory contexts,
        DatabaseSourceRegistrar databases,
        FederatedContextBuilder federated)
    {
        this.registry = registry;
        this.contexts = contexts;
        this.databases = databases;
        this.federated = federated;
    }

    public Task TestConnectionAsync(DataSourceConnection source, CancellationToken cancellationToken) =>
        Guard(async () =>
        {
            if (source.Kind().IsFileKind())
            {
                var name = FileDiscovery.Discover(source)[0].Name;
                using var context = contexts.CreateForFile(source);
                await QueryExecutor.ExecuteAsync(
                    context, ProbeSql(name), ShortTimeout, Stopwatch.StartNew(), cancellationToken);
            }
            else
            {
                await databases.TestConnectionAsync(source, cancellationToken);
            }
        });

    public Task<IReadOnlyList<TableInfo>> ListTablesAsync(DataSourceConnection source, CancellationToken cancellationToken) =>
        Guard(() =>
        {
            if (source.Kind().IsFileKind())
            {
                IReadOnlyList<TableInfo> tables = FileDiscovery.Discover(source)
                    .Select(table => new TableInfo { Schema = null, Name = table.Name, TableType = table.Kind.TableType() })
                    .ToList();
                return Task.FromResult(tables);
            }

            return databases.ListTablesAsync(source, cancellationToken);
        });

    public Task<IReadOnlyList<ColumnInfo>> ListColumnsAsync(
        DataSourceConnection source, string? schema, string table, CancellationToken cancellationToken) =>
        Guard(async () =>
        {
            if (source.Kind().IsFileKind())
            {
                var resolved = FileDiscovery.ResolveTableName(source, table);
                var context = registry.GetOrCreateFileContext(source);
                using var dataFrame = context.Sql(ProbeSql(resolved));
                return ArrowResultMapper.Columns(dataFrame.Schema());
            }

            return await databases.ListColumnsAsync(source, schema, table, cancellationToken);
        });

    public Task<QueryResult> QueryAsync(QueryRequest request, CancellationToken cancellationToken) =>
        Guard(async () =>
        {
            var (limited, timeout, started) = Prepare(request.Sql, request.Limit, request.TimeoutMs);
            var source = request.DataSource;

            if (source.Kind().IsFileKind())
            {
                var context = registry.GetOrCreateFileContext(source);
                return await QueryExecutor.ExecuteAsync(context, limited, timeout, started, cancellationToken);
            }

            using var dbContext = contexts.CreateForDatabaseQuery(source, request.Sql);
            return await QueryExecutor.ExecuteAsync(dbContext, limited, timeout, started, cancellationToken);
        });

    public Task<ExplainResult> ExplainAsync(QueryRequest request, CancellationToken cancellationToken) =>
        Guard(async () =>
        {
            var (limited, timeout, started) = Prepare(request.Sql, request.Limit, request.TimeoutMs);
            var source = request.DataSource;

            if (source.Kind().IsFileKind())
            {
                var context = registry.GetOrCreateFileContext(source);
                return await ExplainRunner.ExplainAsync(context, limited, timeout, started, cancellationToken);
            }

            using var dbContext = contexts.CreateForDatabaseQuery(source, request.Sql);
            return await ExplainRunner.ExplainAsync(dbContext, limited, timeout, started, cancellationToken);
        });

    public Task<QueryResult> FederatedQueryAsync(FederatedQueryRequest request, CancellationToken cancellationToken) =>
        Guard(async () =>
        {
            var (limited, timeout, started) = Prepare(request.Sql, request.Limit, request.TimeoutMs);
            using var context = federated.Build(request.DataSources);
            return await QueryExecutor.ExecuteAsync(context, limited, timeout, started, cancellationToken);
        });

    public Task<ExplainResult> ExplainFederatedAsync(FederatedQueryRequest request, CancellationToken cancellationToken) =>
        Guard(async () =>
        {
            var (limited, timeout, started) = Prepare(request.Sql, request.Limit, request.TimeoutMs);
            using var context = federated.Build(request.DataSources);
            return await ExplainRunner.ExplainAsync(context, limited, timeout, started, cancellationToken);
        });

    private static (string Sql, TimeSpan Timeout, Stopwatch Started) Prepare(string sql, uint? limit, ulong? timeoutMs)
    {
        var started = Stopwatch.StartNew();
        ReadOnlySqlValidator.Validate(sql);
        var limited = LimitRewriter.ApplyLimit(sql, limit);
        var timeout = TimeSpan.FromMilliseconds(timeoutMs ?? EngineConstants.DefaultTimeoutMs);
        return (limited, timeout, started);
    }

    private static readonly TimeSpan ShortTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Builds a schema-only probe (<c>SELECT * ... LIMIT 0</c>) with a safely quoted identifier.</summary>
    private static string ProbeSql(string table)
    {
        var identifier = table.Replace("\"", "\"\"");
        return $"""SELECT * FROM "{identifier}" LIMIT 0""";
    }

    private static async Task<T> Guard<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception exception)
        {
            throw EngineErrorClassifier.Classify(exception);
        }
    }

    private static async Task Guard(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            throw EngineErrorClassifier.Classify(exception);
        }
    }
}
