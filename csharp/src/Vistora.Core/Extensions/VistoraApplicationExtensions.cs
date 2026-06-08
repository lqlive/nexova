namespace Vistora.Core.Extensions;

using Microsoft.Extensions.DependencyInjection;

public static class VistoraApplicationExtensions
{
    public static VistoraApplication AddVistoraCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var app = new VistoraApplication(services);

        services.AddVistoraConfiguration();
        services.AddVistoraStore();
        services.AddVistoraStorage();

        return app;
    }

    public static VistoraApplication AddVistoraCore(
        this IServiceCollection services,
        Action<VistoraApplication> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var app = services.AddVistoraCore();
        configure(app);

        return app;
    }
}
