namespace Vistora.Core;

using Microsoft.Extensions.DependencyInjection;

public sealed class VistoraApplication
{
    public VistoraApplication(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IServiceCollection Services { get; }
}
