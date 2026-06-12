using Apache.DataFusion;

namespace Nexova.Engine.Sources.Files.ObjectStore;

/// <summary>
/// Registers S3 object stores onto a <see cref="SessionContextBuilder"/>. Object stores must be
/// registered at context-build time (the binding only exposes registration on the builder), so
/// callers collect the buckets used by a source's tables and register them before building.
/// Mirrors the engine-side object-store wiring in the Rust engine's <c>files/remote.rs</c>.
/// </summary>
public sealed class S3SourceRegistrar
{
    private readonly S3CredentialProvider credentials;

    public S3SourceRegistrar(S3CredentialProvider credentials)
    {
        this.credentials = credentials;
    }

    public void RegisterStores(SessionContextBuilder builder, IEnumerable<string> buckets)
    {
        foreach (var bucket in buckets.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            builder.RegisterObjectStore(credentials.BuildObjectStore(bucket));
        }
    }
}
