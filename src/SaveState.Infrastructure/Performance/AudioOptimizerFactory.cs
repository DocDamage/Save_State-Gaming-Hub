using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Core.Performance.Services;

namespace SaveState.Infrastructure.Performance;

/// <summary>
/// Factory for creating platform-appropriate audio optimizer instances.
/// </summary>
public static class AudioOptimizerFactory
{
    /// <summary>
    /// Creates the appropriate audio optimizer service based on the current platform.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <returns>A platform-appropriate audio optimizer service.</returns>
    public static IAudioOptimizer Create(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        if (OperatingSystem.IsWindows())
        {
            var logger = loggerFactory.CreateLogger<AudioOptimizer>();
            return new AudioOptimizer(logger);
        }

        // For Linux, macOS, and other platforms, return no-op implementation
        var noOpLogger = loggerFactory.CreateLogger<NoOpAudioOptimizer>();
        return new NoOpAudioOptimizer(noOpLogger);
    }

    /// <summary>
    /// Adds audio optimizer services to the service collection using factory pattern.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAudioOptimizer(this IServiceCollection services)
    {
        services.AddSingleton<IAudioOptimizer>(Create);
        return services;
    }
}
