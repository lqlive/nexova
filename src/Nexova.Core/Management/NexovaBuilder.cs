using Microsoft.Extensions.DependencyInjection;

namespace Nexova.Core.Management;

internal sealed class NexovaBuilder : INexovaBuilder
{
    public NexovaBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    public IServiceCollection Services { get; }
}