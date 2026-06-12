namespace Nexova.Engine.Sources.Files.ObjectStore;

/// <summary>
/// Engine configuration, bound from the <c>NexovaEngine</c> configuration section.
/// </summary>
public sealed class EngineOptions
{
    public const string SectionName = "NexovaEngine";

    /// <summary>
    /// S3 credentials keyed by bucket name. Resolved engine-side; never accepted from the request.
    /// </summary>
    public Dictionary<string, S3BucketCredentials> S3 { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Per-bucket S3 credentials, resolved engine-side from configuration and never carried in the
/// request. Mirrors the engine-side <c>s3.config.json</c> used by the Rust engine.
/// </summary>
public sealed class S3BucketCredentials
{
    public string? Region { get; set; }

    public string? Endpoint { get; set; }

    public string? AccessKeyId { get; set; }

    public string? SecretAccessKey { get; set; }

    public string? SessionToken { get; set; }

    public bool? AllowHttp { get; set; }
}
