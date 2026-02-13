using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Core.Sync.Services;

namespace SaveState.Infrastructure.Sync;

/// <summary>
/// Factory for creating platform-appropriate network optimizer instances.
/// </summary>
public static class NetworkOptimizerFactory
{
    /// <summary>
    /// Creates the appropriate network optimizer service based on the current platform.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <returns>A platform-appropriate network optimizer service.</returns>
    public static INetworkOptimizerService Create(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        if (OperatingSystem.IsWindows())
        {
            var logger = loggerFactory.CreateLogger<WindowsNetworkOptimizerService>();
            return new WindowsNetworkOptimizerService(logger);
        }

        // For Linux, macOS, and other platforms, return no-op implementation
        var noOpLogger = loggerFactory.CreateLogger<NoOpNetworkOptimizerService>();
        return new NoOpNetworkOptimizerService(noOpLogger);
    }

    /// <summary>
    /// Adds network optimizer services to the service collection using factory pattern.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNetworkOptimizer(this IServiceCollection services)
    {
        services.AddSingleton<INetworkOptimizerService>(Create);
        return services;
    }
}
