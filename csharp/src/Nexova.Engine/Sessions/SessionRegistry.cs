using System.Collections.Concurrent;
using Apache.DataFusion;
using Nexova.Engine.Contracts;
using Nexova.Engine.Sources.Files;

namespace Nexova.Engine.Sessions;

/// <summary>
/// Caches file-source <see cref="SessionContext"/> instances keyed by source identity, invalidating
/// when the backing files change (via <see cref="FileDiscovery.Signature"/>). Mirrors the Rust
/// engine's <c>BackendRegistry.file_context</c>. Database contexts are cheap to register per query
/// and are not cached here.
/// </summary>
public sealed class SessionRegistry : IDisposable
{
    private readonly SessionContextFactory factory;
    private readonly ConcurrentDictionary<string, CacheEntry> fileContexts = new();
    private readonly object buildLock = new();

    public SessionRegistry(SessionContextFactory factory)
    {
        this.factory = factory;
    }

    /// <summary>Returns a cached context for a file source, rebuilding when its signature changed.</summary>
    public SessionContext GetOrCreateFileContext(DataSourceConnection source)
    {
        var key = FileCacheKey(source);
        var signature = FileDiscovery.Signature(source);

        if (fileContexts.TryGetValue(key, out var existing) && existing.Signature == signature)
        {
            return existing.Context;
        }

        lock (buildLock)
        {
            if (fileContexts.TryGetValue(key, out var current) && current.Signature == signature)
            {
                return current.Context;
            }

            var context = factory.CreateForFile(source);
            if (fileContexts.TryRemove(key, out var stale))
            {
                stale.Context.Dispose();
            }

            fileContexts[key] = new CacheEntry(signature, context);
            return context;
        }
    }

    private static string FileCacheKey(DataSourceConnection source) => string.Join(';',
        $"type={source.SourceType.ToLowerInvariant()}",
        $"path={source.Path}",
        $"table={source.Table}",
        $"hasHeader={source.HasHeader}",
        $"delimiter={source.Delimiter}",
        $"sheet={source.Sheet}");

    public void Dispose()
    {
        foreach (var entry in fileContexts.Values)
        {
            entry.Context.Dispose();
        }

        fileContexts.Clear();
    }

    private readonly record struct CacheEntry(string Signature, SessionContext Context);
}
