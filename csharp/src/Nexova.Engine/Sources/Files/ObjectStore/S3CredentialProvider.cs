using Apache.DataFusion;
using Microsoft.Extensions.Options;

namespace Nexova.Engine.Sources.Files.ObjectStore;

/// <summary>
/// Resolves per-bucket S3 credentials from configuration and builds the DataFusion
/// <see cref="ObjectStoreOptions"/> registration for a given bucket.
/// </summary>
public sealed class S3CredentialProvider
{
    private readonly EngineOptions options;

    public S3CredentialProvider(IOptions<EngineOptions> options)
    {
        this.options = options.Value;
    }

    public ObjectStoreOptions BuildObjectStore(string bucket)
    {
        var builder = ObjectStoreOptions.S3()
            .WithUrl($"s3://{bucket}")
            .WithBucket(bucket);

        if (options.S3.TryGetValue(bucket, out var credentials))
        {
            if (!string.IsNullOrWhiteSpace(credentials.Region)) builder.WithRegion(credentials.Region);
            if (!string.IsNullOrWhiteSpace(credentials.Endpoint)) builder.WithEndpoint(credentials.Endpoint);
            if (!string.IsNullOrWhiteSpace(credentials.AccessKeyId)) builder.WithAccessKeyId(credentials.AccessKeyId);
            if (!string.IsNullOrWhiteSpace(credentials.SecretAccessKey)) builder.WithSecretAccessKey(credentials.SecretAccessKey);
            if (!string.IsNullOrWhiteSpace(credentials.SessionToken)) builder.WithSessionToken(credentials.SessionToken);
            if (credentials.AllowHttp.HasValue) builder.WithAllowHttp(credentials.AllowHttp.Value);
        }

        return builder.Build();
    }
}
