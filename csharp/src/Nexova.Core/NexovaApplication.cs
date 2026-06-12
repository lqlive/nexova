namespace Nexova.Core;

using Microsoft.Extensions.DependencyInjection;

public sealed class NexovaApplication
{
    public NexovaApplication(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IServiceCollection Services { get; }
}
