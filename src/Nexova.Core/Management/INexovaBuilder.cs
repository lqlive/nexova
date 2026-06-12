using Microsoft.Extensions.DependencyInjection;

namespace Nexova.Core.Management;

public interface INexovaBuilder
{
    IServiceCollection Services { get; }
}
