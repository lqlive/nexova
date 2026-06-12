using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Nexova.Core.Management;
using Nexova.Core.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Nexova.Storage.Aws;

public static class AwsStorageBuilderExtensions
{
    public static INexovaBuilder AddAwsS3Storage(this INexovaBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var services = builder.Services;

        services.AddOptions<S3StorageOptions>()
               .BindConfiguration(S3StorageOptions.SectionName);

        services.AddKeyedTransient<IStorageService, S3StorageService>(S3StorageOptions.Name);

        services.TryAddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<S3StorageOptions>>().Value;

            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region)
            };

            return new AmazonS3Client(
                new BasicAWSCredentials(options.AccessKey, options.SecretKey),
                config);
        });

        return builder;
    }

    public static INexovaBuilder AddAwsS3Storage(this INexovaBuilder builder, 
        Action<S3StorageOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.PostConfigure(configure);

        return builder.AddAwsS3Storage();
    }
}