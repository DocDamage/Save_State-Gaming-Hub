using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.RetroArch;
using SaveState.Core.RetroArch.Services;
using SaveState.Infrastructure.RetroArch.RetroArchCloudSync;
using SaveState.Infrastructure.RetroArch.Services.RetroArch;

namespace SaveState.Infrastructure.RetroArch;

/// <summary>
/// Extension methods for registering RetroArch services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds RetroArch services and engines to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddRetroArchServices(this IServiceCollection services)
    {
        // Register main service
        services.AddScoped<IRetroArchService, RetroArchService>();

        // Register specialized engines
        services.AddScoped<IPathDetectionEngine, PathDetectionEngine>();
        services.AddScoped<IGameManagementEngine, GameManagementEngine>();
        services.AddScoped<ICoreManagementEngine, CoreManagementEngine>();
        services.AddScoped<IConfigurationEngine, ConfigurationEngine>();
        services.AddScoped<INetworkCommandEngine, NetworkCommandEngine>();
        services.AddScoped<ISaveStateEngine, SaveStateEngine>();
        services.AddScoped<IRetroAchievementsEngine, RetroAchievementsEngine>();

        // Register cloud sync engines (as factory pattern)
        services.AddScoped<AwsS3SyncEngine>();
        services.AddScoped<AzureBlobSyncEngine>();
        services.AddScoped<GoogleCloudSyncEngine>();

        return services;
    }

    /// <summary>
    /// Adds RetroArch services with custom options configuration.
    /// </summary>
    public static IServiceCollection AddRetroArchServices(
        this IServiceCollection services,
        Action<RetroArchOptions> configureOptions)
    {
        services.Configure(configureOptions);
        return AddRetroArchServices(services);
    }
}
