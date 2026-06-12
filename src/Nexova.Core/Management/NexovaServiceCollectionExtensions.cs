using Nexova.Core.Configuration;
using Nexova.Core.Storage;
using Nexova.Core.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Nexova.Core.Management;

public static class NexovaServiceCollectionExtensions
{
    public static INexovaBuilder AddNexovaCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = new NexovaBuilder(services);
        builder.Services.AddConfiguration();

        return builder;
    }

    public static INexovaBuilder AddFileStorage(this INexovaBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var services = builder.Services;

        services.AddKeyedTransient<IStorageService, FileStorageService>(FileSystemStorageOptions.Name);
        services.TryAddTransient(provider => 
            provider.GetRequiredKeyedService<IStorageService>(FileSystemStorageOptions.Name));

        return builder;
    }

    public static INexovaBuilder AddInMemoryStore(this INexovaBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var services = builder.Services;

        services.AddKeyedScoped<IDataSourceStore, InMemoryDataSourceStore>(InMemoryStoreOptions.Name);
        services.AddKeyedScoped<IDatasetStore, InMemoryDatasetStore>(InMemoryStoreOptions.Name);

        return builder;
    }

    private static void AddConfiguration(this IServiceCollection services)
    {
        services.AddOptions<StorageOptions>()
            .BindConfiguration(StorageOptions.SectionName);
        services.AddOptions<FileSystemStorageOptions>()
            .BindConfiguration(FileSystemStorageOptions.SectionName);
        services.AddOptions<DatabaseOptions>()
                .BindConfiguration(DatabaseOptions.SectionName);
    }
}
