using Apache.DataFusion;
using Nexova.Engine.Contracts;
using Nexova.Engine.Exceptions;
using Nexova.Engine.Sources.Databases;
using Nexova.Engine.Sources.Files;
using Nexova.Engine.Sources.Files.ObjectStore;

namespace Nexova.Engine.Execution;

/// <summary>
/// Builds a single <see cref="SessionContext"/> spanning multiple sources for cross-source queries,
/// mirroring the Rust engine's <c>federated::build_federated_context</c>. Each database source must
/// carry an explicit table (and optional alias); file sources contribute their discovered tables.
/// </summary>
public sealed class FederatedContextBuilder
{
    private readonly FileSourceRegistrar files;
    private readonly DatabaseSourceRegistrar databases;
    private readonly S3SourceRegistrar objectStores;

    public FederatedContextBuilder(
        FileSourceRegistrar files, DatabaseSourceRegistrar databases, S3SourceRegistrar objectStores)
    {
        this.files = files;
        this.databases = databases;
        this.objectStores = objectStores;
    }

    public SessionContext Build(IReadOnlyList<DataSourceConnection> sources)
    {
        if (sources.Count == 0)
        {
            throw EngineException.InvalidConnection("at least one data source is required");
        }

        var buckets = sources
            .Where(source => source.Kind().IsFileKind())
            .SelectMany(files.CollectBuckets)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        SessionContext context;
        if (buckets.Count > 0)
        {
            var builder = SessionContext.CreateBuilder();
            objectStores.RegisterStores(builder, buckets);
            context = builder.Build();
        }
        else
        {
            context = new SessionContext();
        }

        try
        {
            var registeredNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in sources)
            {
                if (source.Kind().IsFileKind())
                {
                    files.RegisterTables(context, source, registeredNames);
                }
                else
                {
                    RegisterDatabaseTable(context, source, registeredNames);
                }
            }

            return context;
        }
        catch
        {
            context.Dispose();
            throw;
        }
    }

    private void RegisterDatabaseTable(
        SessionContext context, DataSourceConnection source, ISet<string> registeredNames)
    {
        var table = (source.Table ?? string.Empty).Trim();
        if (table.Length == 0)
        {
            throw EngineException.InvalidConnection("federated database sources require an explicit table");
        }

        var alias = (source.Alias ?? string.Empty).Trim();
        var registeredName = alias.Length > 0 ? alias : table;
        if (!registeredNames.Add(registeredName))
        {
            throw EngineException.InvalidConnection($"duplicate federated table name '{registeredName}'");
        }

        databases.RegisterTable(context, source, table, registeredName);
    }
}
