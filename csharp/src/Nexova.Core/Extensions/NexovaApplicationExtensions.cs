namespace Nexova.Core.Extensions;

using Microsoft.Extensions.DependencyInjection;

public static class NexovaApplicationExtensions
{
    public static NexovaApplication AddNexovaCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var app = new NexovaApplication(services);

        services.AddNexovaConfiguration();
        services.AddNexovaStore();
        services.AddNexovaStorage();

        return app;
    }

    public static NexovaApplication AddNexovaCore(
        this IServiceCollection services,
        Action<NexovaApplication> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var app = services.AddNexovaCore();
        configure(app);

        return app;
    }
}
