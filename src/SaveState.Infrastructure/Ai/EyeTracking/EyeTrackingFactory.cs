using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common.Services;
using SaveState.Infrastructure.Assistant;

namespace SaveState.Infrastructure.Ai.EyeTracking;

/// <summary>
/// Factory for creating the best available eye-tracking provider.
/// Selects from: Tobii (highest quality) → Windows Eye Control → NoOp (fallback).
/// </summary>
public static class EyeTrackingFactory
{
    /// <summary>
    /// Creates the best available eye-tracking monitor for the current platform.
    /// Priority: Tobii > Windows Eye Control > NoOp
    /// </summary>
    public static IEyeTrackingMonitor CreateBestAvailable(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var timeProvider = serviceProvider.GetRequiredService<ITimeProvider>();

        // Try Tobii first (highest quality, most precise)
        var tobiiLogger = loggerFactory.CreateLogger<TobiiEyeTrackingProvider>();
        var tobiiProvider = new TobiiEyeTrackingProvider(tobiiLogger, timeProvider);
        
        if (tobiiProvider.IsAvailable)
        {
            tobiiLogger.LogInformation("Using Tobii eye-tracking provider");
            return tobiiProvider;
        }
        
        tobiiProvider.Dispose();

        // Try Windows Eye Control on Windows
        if (OperatingSystem.IsWindows())
        {
            var windowsLogger = loggerFactory.CreateLogger<WindowsEyeControlProvider>();
            var windowsProvider = new WindowsEyeControlProvider(windowsLogger, timeProvider);
            
            if (windowsProvider.IsAvailable)
            {
                windowsLogger.LogInformation("Using Windows Eye Control provider");
                return windowsProvider;
            }
            
            windowsProvider.Dispose();
        }

        // Fall back to NoOp provider
        var noOpLogger = loggerFactory.CreateLogger<NoOpEyeTrackingMonitor>();
        noOpLogger.LogInformation("No eye-tracking hardware available. Using NoOp provider.");
        return new NoOpEyeTrackingMonitor(noOpLogger);
    }

    /// <summary>
    /// Creates a specific eye-tracking provider by type.
    /// </summary>
    public static IEyeTrackingMonitor CreateProvider(
        EyeTrackingProviderType providerType,
        IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var timeProvider = serviceProvider.GetRequiredService<ITimeProvider>();

        return providerType switch
        {
            EyeTrackingProviderType.Tobii => new TobiiEyeTrackingProvider(
                loggerFactory.CreateLogger<TobiiEyeTrackingProvider>(),
                timeProvider),
            
            EyeTrackingProviderType.WindowsEyeControl => OperatingSystem.IsWindows()
                ? new WindowsEyeControlProvider(
                    loggerFactory.CreateLogger<WindowsEyeControlProvider>(),
                    timeProvider)
                : throw new PlatformNotSupportedException("Windows Eye Control is only available on Windows"),
            
            EyeTrackingProviderType.NoOp => new NoOpEyeTrackingMonitor(
                loggerFactory.CreateLogger<NoOpEyeTrackingMonitor>()),
            
            _ => throw new ArgumentOutOfRangeException(nameof(providerType), $"Unknown provider type: {providerType}")
        };
    }

    /// <summary>
    /// Gets all available eye-tracking providers on the current system.
    /// </summary>
    public static IEnumerable<EyeTrackingProviderInfo> GetAvailableProviders(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var timeProvider = serviceProvider.GetRequiredService<ITimeProvider>();
        var providers = new List<EyeTrackingProviderInfo>();

        // Check Tobii
        var tobiiLogger = loggerFactory.CreateLogger<TobiiEyeTrackingProvider>();
        using (var tobii = new TobiiEyeTrackingProvider(tobiiLogger, timeProvider))
        {
            providers.Add(new EyeTrackingProviderInfo(
                EyeTrackingProviderType.Tobii,
                "Tobii Eye Tracking",
                tobii.IsAvailable,
                1)); // Highest priority
        }

        // Check Windows Eye Control
        if (OperatingSystem.IsWindows())
        {
            var windowsLogger = loggerFactory.CreateLogger<WindowsEyeControlProvider>();
            using (var windows = new WindowsEyeControlProvider(windowsLogger, timeProvider))
            {
                providers.Add(new EyeTrackingProviderInfo(
                    EyeTrackingProviderType.WindowsEyeControl,
                    "Windows Eye Control",
                    windows.IsAvailable,
                    2));
            }
        }

        // NoOp is always available
        providers.Add(new EyeTrackingProviderInfo(
            EyeTrackingProviderType.NoOp,
            "No Eye Tracking (Software Only)",
            true,
            99));

        return providers.OrderBy(p => p.Priority);
    }

    /// <summary>
    /// Adds eye-tracking services to the DI container with automatic provider selection.
    /// </summary>
    public static IServiceCollection AddEyeTrackingServices(this IServiceCollection services)
    {
        services.AddSingleton<IEyeTrackingMonitor>(sp => CreateBestAvailable(sp));
        return services;
    }

    /// <summary>
    /// Adds eye-tracking services with a specific provider type.
    /// </summary>
    public static IServiceCollection AddEyeTrackingServices(
        this IServiceCollection services,
        EyeTrackingProviderType providerType)
    {
        services.AddSingleton<IEyeTrackingMonitor>(sp => CreateProvider(providerType, sp));
        return services;
    }
}

/// <summary>
/// Available eye-tracking provider types.
/// </summary>
public enum EyeTrackingProviderType
{
    Tobii,
    WindowsEyeControl,
    NoOp
}

/// <summary>
/// Information about an eye-tracking provider.
/// </summary>
public sealed record EyeTrackingProviderInfo(
    EyeTrackingProviderType Type,
    string Name,
    bool IsAvailable,
    int Priority);
