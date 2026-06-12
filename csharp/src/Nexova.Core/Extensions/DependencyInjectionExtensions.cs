using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexova.Core.Configuration;
using Nexova.Core.Storage;
using Nexova.Core.Store;

namespace Nexova.Core.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddNexovaConfiguration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<FileSystemStorageOptions>()
            .BindConfiguration("Storage");

        return services;
    }

    public static IServiceCollection AddNexovaStore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IDataSourceStore, InMemoryDataSourceStore>();
        services.TryAddSingleton<IDatasetStore, InMemoryDatasetStore>();

        return services;
    }

    public static IServiceCollection AddNexovaStorage(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddTransient<FileStorageService>();
        services.TryAddTransient<IStorageService>(
            provider => provider.GetRequiredService<FileStorageService>());

        return services;
    }
}
