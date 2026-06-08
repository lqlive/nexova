using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vistora.Core.Configuration;
using Vistora.Core.Storage;
using Vistora.Core.Store;

namespace Vistora.Core.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddVistoraConfiguration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<FileSystemStorageOptions>()
            .BindConfiguration("Storage");

        return services;
    }

    public static IServiceCollection AddVistoraStore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IDataSourceStore, InMemoryDataSourceStore>();
        services.TryAddSingleton<IDatasetStore, InMemoryDatasetStore>();

        return services;
    }

    public static IServiceCollection AddVistoraStorage(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddTransient<FileStorageService>();
        services.TryAddTransient<IStorageService>(
            provider => provider.GetRequiredService<FileStorageService>());

        return services;
    }
}
