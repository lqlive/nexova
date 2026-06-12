namespace Nexova.Storage.Aws;

public class S3StorageOptions
{
    public const string Name = "AwsS3";
    public const string SectionName = "S3Storage";
    public required string AccessKey { get; set; }
    public required string SecretKey { get; set; }
    public required string Region { get; set; }
    public required string Bucket { get; set; }
    public required string Prefix { get; set; }
}
