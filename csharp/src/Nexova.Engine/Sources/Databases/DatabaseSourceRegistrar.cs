using Apache.Arrow;
using Apache.DataFusion;
using SqlParser;
using SqlParser.Ast;
using SqlParser.Dialects;
using Nexova.Engine.Contracts;
using Nexova.Engine.Exceptions;

namespace Nexova.Engine.Sources.Databases;

/// <summary>
/// Registers database-backed tables (Postgres/MySQL/ClickHouse/MongoDB/SQLite) into a DataFusion
/// <see cref="SessionContext"/> and answers schema-browsing requests. Mirrors the Rust engine's
/// <c>providers</c> and <c>federated/databases</c> modules.
///
/// The binding registers tables by name (no catalog auto-discovery), so single-source queries
/// register exactly the tables referenced in the SQL; schema browsing for Postgres/MySQL is served
/// via <c>information_schema</c>.
/// </summary>
public sealed class DatabaseSourceRegistrar
{
    // ---------------------------------------------------------------- registration

    /// <summary>Registers a single explicit table under an alias (federated path).</summary>
    public void RegisterTable(SessionContext context, DataSourceConnection source, string table, string alias) =>
        Register(context, source, alias, source.Schema, table);

    /// <summary>Registers every table referenced by <paramref name="sql"/> (single-source path).</summary>
    public void RegisterReferencedTables(SessionContext context, DataSourceConnection source, string sql)
    {
        foreach (var reference in ReferencedTables.Extract(sql))
        {
            Register(context, source, reference.Name, reference.Schema ?? source.Schema, reference.Table);
        }
    }

    private static void Register(
        SessionContext context, DataSourceConnection source, string name, string? schema, string table)
    {
        var connection = source.Kind() is DataSourceKind.Sqlite ? null : source.BuildConnectionString();

        switch (source.Kind())
        {
            case DataSourceKind.Postgres:
                context.RegisterPostgres(name, new PostgresTableOptions(connection!, table)
                {
                    SchemaName = Trim(schema) ?? "public",
                });
                break;

            case DataSourceKind.MySql:
                context.RegisterMySql(name, new MySqlTableOptions(connection!, table)
                {
                    SchemaName = Trim(schema) ?? Trim(source.Database),
                });
                break;

            case DataSourceKind.ClickHouse:
                context.RegisterClickHouse(name, new ClickHouseTableOptions(connection!, table)
                {
                    Database = Trim(source.Database),
                    User = Trim(source.Username),
                    Password = Trim(source.Password),
                });
                break;

            case DataSourceKind.MongoDb:
                context.RegisterMongoDb(name, new MongoDbTableOptions(connection!, table));
                break;

            case DataSourceKind.Sqlite:
                context.RegisterSqlite(name, new SqliteTableOptions(RequireSqlitePath(source), table));
                break;

            default:
                throw EngineException.UnsupportedDataSource();
        }
    }

    // ---------------------------------------------------------------- schema browsing

    public async Task<IReadOnlyList<TableInfo>> ListTablesAsync(
        DataSourceConnection source, CancellationToken cancellationToken)
    {
        var where = source.Kind() switch
        {
            DataSourceKind.Postgres => "table_schema NOT IN ('pg_catalog', 'information_schema')",
            DataSourceKind.MySql => $"table_schema = '{Escape(source.Database)}'",
            _ => throw BrowsingUnsupported(source, "table"),
        };

        using var context = new SessionContext();
        Register(context, source, "__nexova_tables", "information_schema", "tables");

        var rows = await RunStringRowsAsync(context,
            $"SELECT table_schema, table_name, table_type FROM __nexova_tables WHERE {where}", cancellationToken);

        return rows.Select(row => new TableInfo
        {
            Schema = row[0],
            Name = row[1] ?? string.Empty,
            TableType = row[2] ?? "BASE TABLE",
        }).ToList();
    }

    public async Task<IReadOnlyList<ColumnInfo>> ListColumnsAsync(
        DataSourceConnection source, string? schema, string table, CancellationToken cancellationToken)
    {
        var kind = source.Kind();
        if (kind is not (DataSourceKind.Postgres or DataSourceKind.MySql))
        {
            throw BrowsingUnsupported(source, "column");
        }

        var defaultSchema = kind == DataSourceKind.Postgres ? "public" : source.Database;
        var resolvedSchema = Escape(schema ?? source.Schema ?? defaultSchema);

        using var context = new SessionContext();
        Register(context, source, "__nexova_columns", "information_schema", "columns");

        var rows = await RunStringRowsAsync(context,
            "SELECT column_name, data_type, is_nullable FROM __nexova_columns " +
            $"WHERE table_schema = '{resolvedSchema}' AND table_name = '{Escape(table)}' ORDER BY ordinal_position",
            cancellationToken);

        return rows.Select(row => new ColumnInfo
        {
            Name = row[0] ?? string.Empty,
            ColumnType = row[1] ?? string.Empty,
            Nullable = string.Equals(row[2], "YES", StringComparison.OrdinalIgnoreCase),
        }).ToList();
    }

    public async Task TestConnectionAsync(DataSourceConnection source, CancellationToken cancellationToken)
    {
        using var context = new SessionContext();

        var table = Trim(source.Table);
        if (table is not null)
        {
            Register(context, source, "__nexova_probe", source.Schema, table);
        }
        else if (source.Kind() is DataSourceKind.Postgres or DataSourceKind.MySql)
        {
            Register(context, source, "__nexova_probe", "information_schema", "tables");
        }
        else
        {
            throw EngineException.InvalidConnection("provide a table to test this connection");
        }

        try
        {
            await RunStringRowsAsync(context, "SELECT * FROM __nexova_probe LIMIT 0", cancellationToken);
        }
        catch (Exception exception) when (exception is not EngineException)
        {
            throw EngineErrorClassifier.ClassifyConnection(exception);
        }
    }

    // ---------------------------------------------------------------- helpers

    private static async Task<List<string?[]>> RunStringRowsAsync(
        SessionContext context, string sql, CancellationToken cancellationToken)
    {
        DataFrame dataFrame;
        try
        {
            dataFrame = context.Sql(sql);
        }
        catch (Exception exception) when (exception is not EngineException)
        {
            throw EngineErrorClassifier.Classify(exception);
        }

        var rows = new List<string?[]>();
        using var reader = dataFrame.Collect();
        while (await reader.ReadNextRecordBatchAsync(cancellationToken) is { } batch)
        {
            using (batch)
            {
                for (var r = 0; r < batch.Length; r++)
                {
                    var row = new string?[batch.ColumnCount];
                    for (var c = 0; c < batch.ColumnCount; c++)
                    {
                        row[c] = (batch.Column(c) as StringArray)?.GetString(r);
                    }

                    rows.Add(row);
                }
            }
        }

        return rows;
    }

    private static string RequireSqlitePath(DataSourceConnection source) =>
        Trim(source.Path) ?? Trim(source.ConnectionString)
            ?? throw EngineException.InvalidConnection("missing sqlite path");

    private static EngineException BrowsingUnsupported(DataSourceConnection source, string what) =>
        EngineException.QueryExecution(
            $"{what} browsing is not supported for {source.Kind().TableType()} via the in-process engine");

    private static string Escape(string? value) => (value ?? string.Empty).Replace("'", "''");

    private static string? Trim(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>
    /// Self-contained scanner that extracts the physical tables referenced by a single SELECT
    /// (excluding CTE names) from the SqlParserCS AST, so the registrar above stays focused.
    /// </summary>
    private static class ReferencedTables
    {
        public readonly record struct Reference(string Name, string? Schema, string Table);

        public static IReadOnlyList<Reference> Extract(string sql)
        {
            Sequence<Statement> statements;
            try
            {
                statements = new Parser().ParseSql(sql, new GenericDialect());
            }
            catch
            {
                return [];
            }

            if (statements.Count != 1 || statements[0] is not Statement.Select select)
            {
                return [];
            }

            var scanner = new Scanner();
            scanner.Visit(select.Query);
            return scanner.References;
        }

        private sealed class Scanner
        {
            private readonly HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

            public List<Reference> References { get; } = [];

            public void Visit(Query query)
            {
                var ctes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (query.With is not null)
                {
                    foreach (var cte in query.With.CteTables)
                    {
                        ctes.Add(cte.Alias.Name.Value);
                        Visit(cte.Query);
                    }
                }

                Visit(query.Body, ctes);
            }

            private void Visit(SetExpression body, HashSet<string> ctes)
            {
                switch (body)
                {
                    case SetExpression.SelectExpression { Select.From: { } from }:
                        foreach (var item in from)
                        {
                            VisitRelation(item.Relation, ctes);
                            VisitJoins(item.Joins, ctes);
                        }

                        break;

                    case SetExpression.QueryExpression nested:
                        Visit(nested.Query);
                        break;

                    case SetExpression.SetOperation operation:
                        Visit(operation.Left, ctes);
                        Visit(operation.Right, ctes);
                        break;
                }
            }

            private void VisitJoins(Sequence<Join>? joins, HashSet<string> ctes)
            {
                if (joins is null)
                {
                    return;
                }

                foreach (var join in joins)
                {
                    VisitRelation(join.Relation, ctes);
                }
            }

            private void VisitRelation(TableFactor? relation, HashSet<string> ctes)
            {
                switch (relation)
                {
                    case TableFactor.Table table:
                        Add(table.Name, ctes);
                        break;

                    case TableFactor.Derived derived:
                        Visit(derived.SubQuery);
                        break;

                    case TableFactor.NestedJoin { TableWithJoins: { } inner }:
                        VisitRelation(inner.Relation, ctes);
                        VisitJoins(inner.Joins, ctes);
                        break;
                }
            }

            private void Add(ObjectName name, HashSet<string> ctes)
            {
                var parts = name.Values.Select(ident => ident.Value).ToList();
                if (parts.Count == 0 || ctes.Contains(parts[0]))
                {
                    return;
                }

                var reference = string.Join('.', parts);
                if (seen.Add(reference))
                {
                    References.Add(new Reference(reference, parts.Count >= 2 ? parts[^2] : null, parts[^1]));
                }
            }
        }
    }
}
