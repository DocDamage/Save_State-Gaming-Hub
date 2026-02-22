using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common.Services;
using SaveState.Infrastructure.Ai.EyeTracking;

namespace SaveState.Infrastructure.Assistant;

/// <summary>
/// Factory for creating platform-appropriate eye-tracking monitor instances.
/// </summary>
public static class EyeTrackingMonitorFactory
{
    /// <summary>
    /// Creates an eye-tracking monitor for the current platform.
    /// Returns a composite provider that tries multiple providers in order of preference.
    /// </summary>
    public static IEyeTrackingMonitor Create(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var timeProvider = serviceProvider.GetRequiredService<ITimeProvider>();
        var compositeLogger = loggerFactory.CreateLogger<CompositeEyeTrackingProvider>();

        var providers = new List<IEyeTrackingMonitor>();

        // Try to create Tobii provider first (most accurate)
        try
        {
            var tobiiLogger = loggerFactory.CreateLogger<TobiiEyeTrackingProvider>();
            var tobiiProvider = new TobiiEyeTrackingProvider(tobiiLogger, timeProvider);
            
            if (tobiiProvider.IsAvailable)
            {
                providers.Add(tobiiProvider);
            }
            else
            {
                // Dispose if not available
                tobiiProvider.Dispose();
            }
        }
        catch (Exception ex)
        {
            compositeLogger.LogDebug(ex, "Could not create Tobii provider");
        }

        // Add Windows Eye Control provider (fallback)
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var windowsLogger = loggerFactory.CreateLogger<WindowsEyeTrackingMonitor>();
                var windowsProvider = new WindowsEyeTrackingMonitor(windowsLogger, timeProvider);
                providers.Add(windowsProvider);
            }
            catch (Exception ex)
            {
                compositeLogger.LogDebug(ex, "Could not create Windows Eye Control provider");
            }
        }

        // Always add no-op provider as final fallback
        var noOpLogger = loggerFactory.CreateLogger<NoOpEyeTrackingMonitor>();
        providers.Add(new NoOpEyeTrackingMonitor(noOpLogger));

        // Create composite provider
        return new CompositeEyeTrackingProvider(compositeLogger, timeProvider, providers);
    }

    /// <summary>
    /// Creates a specific eye-tracking provider by type.
    /// </summary>
    public static IEyeTrackingMonitor? CreateProvider<T>(
        IServiceProvider serviceProvider) where T : class, IEyeTrackingMonitor
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var timeProvider = serviceProvider.GetRequiredService<ITimeProvider>();

        if (typeof(T) == typeof(TobiiEyeTrackingProvider))
        {
            var logger = loggerFactory.CreateLogger<TobiiEyeTrackingProvider>();
            return new TobiiEyeTrackingProvider(logger, timeProvider);
        }

        if (typeof(T) == typeof(WindowsEyeTrackingMonitor) && OperatingSystem.IsWindows())
        {
            var logger = loggerFactory.CreateLogger<WindowsEyeTrackingMonitor>();
            return new WindowsEyeTrackingMonitor(logger, timeProvider);
        }

        if (typeof(T) == typeof(NoOpEyeTrackingMonitor))
        {
            var logger = loggerFactory.CreateLogger<NoOpEyeTrackingMonitor>();
            return new NoOpEyeTrackingMonitor(logger);
        }

        return null;
    }

    /// <summary>
    /// Adds eye-tracking monitor services to the DI container.
    /// </summary>
    public static IServiceCollection AddEyeTrackingMonitor(this IServiceCollection services)
    {
        // Register as singleton since eye-tracking is a hardware resource
        services.AddSingleton<IEyeTrackingMonitor>(Create);
        return services;
    }
}
