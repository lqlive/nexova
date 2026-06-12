using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexova.Engine.Execution;
using Nexova.Engine.Sessions;
using Nexova.Engine.Sources.Databases;
using Nexova.Engine.Sources.Files;
using Nexova.Engine.Sources.Files.ObjectStore;

namespace Nexova.Engine.Extensions;

/// <summary>
/// Registers the engine and its collaborators so the host (Nexova) can depend on
/// <see cref="IQueryEngine"/> only.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNexovaEngine(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EngineOptions>().Bind(configuration.GetSection(EngineOptions.SectionName));

        services.AddSingleton<S3CredentialProvider>();
        services.AddSingleton<S3SourceRegistrar>();
        services.AddSingleton<FileSourceRegistrar>();
        services.AddSingleton<DatabaseSourceRegistrar>();
        services.AddSingleton<SessionContextFactory>();
        services.AddSingleton<SessionRegistry>();
        services.AddSingleton<FederatedContextBuilder>();
        services.AddSingleton<IQueryEngine, QueryEngine>();

        return services;
    }
}
