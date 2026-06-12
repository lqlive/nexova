using Apache.DataFusion;
using Nexova.Engine.Contracts;
using Nexova.Engine.Sources.Databases;
using Nexova.Engine.Sources.Files;

namespace Nexova.Engine.Sessions;

/// <summary>
/// Builds a ready-to-query <see cref="SessionContext"/> for a single source, dispatching by
/// <see cref="DataSourceKind"/>. Mirrors the Rust engine's <c>source_context</c> + <c>backend_for</c>.
/// </summary>
public sealed class SessionContextFactory
{
    private readonly FileSourceRegistrar files;
    private readonly DatabaseSourceRegistrar databases;

    public SessionContextFactory(FileSourceRegistrar files, DatabaseSourceRegistrar databases)
    {
        this.files = files;
        this.databases = databases;
    }

    /// <summary>Builds a context for a file source with all discovered tables registered.</summary>
    public SessionContext CreateForFile(DataSourceConnection source) => files.BuildContext(source);

    /// <summary>
    /// Builds a context for a database source with the tables referenced by the SQL registered
    /// (the binding has no catalog auto-discovery, so referenced tables are registered explicitly).
    /// </summary>
    public SessionContext CreateForDatabaseQuery(DataSourceConnection source, string sql)
    {
        var context = new SessionContext();
        databases.RegisterReferencedTables(context, source, sql);
        return context;
    }
}
