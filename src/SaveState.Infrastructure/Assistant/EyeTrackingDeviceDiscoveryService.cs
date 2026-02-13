using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Infrastructure.AI.EyeTracking;

namespace SaveState.Infrastructure.Assistant;

/// <summary>
/// Discovers and manages available eye-tracking devices.
/// </summary>
public sealed class EyeTrackingDeviceDiscoveryService : IDisposable
{
    private readonly ILogger<EyeTrackingDeviceDiscoveryService> _logger;
    private readonly List<IDisposable> _disposables = new();
    private bool _isDisposed;

    public EyeTrackingDeviceDiscoveryService(ILogger<EyeTrackingDeviceDiscoveryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Discovers all available eye-tracking devices on the system.
    /// </summary>
    public Task<Result<IReadOnlyList<EyeTrackingDevice>>> DiscoverDevicesAsync(
        CancellationToken ct = default)
    {
        if (_isDisposed)
        {
            return Task.FromResult(Result.Failure<IReadOnlyList<EyeTrackingDevice>>(
                "Discovery service has been disposed.",
                ErrorType.Validation));
        }

        try
        {
            var devices = new List<EyeTrackingDevice>();

            // Discover Tobii devices
            DiscoverTobiiDevices(devices);

            // Discover Windows Eye Control
            DiscoverWindowsEyeControlDevices(devices);

            _logger.LogInformation(
                "Discovered {Count} eye-tracking device(s): {Devices}",
                devices.Count,
                string.Join(", ", devices.Select(d => $"{d.Name} ({d.Type})")));

            return Task.FromResult(Result.Success<IReadOnlyList<EyeTrackingDevice>>(devices.AsReadOnly()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering eye-tracking devices");
            return Task.FromResult(Result.Failure<IReadOnlyList<EyeTrackingDevice>>(
                $"Discovery failed: {ex.Message}",
                ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets the preferred eye-tracking provider based on availability and device quality.
    /// </summary>
    public Task<Result<IEyeTrackingMonitor>> GetPreferredProviderAsync(
        IServiceProvider serviceProvider,
        CancellationToken ct = default)
    {
        if (_isDisposed)
        {
            return Task.FromResult(Result.Failure<IEyeTrackingMonitor>(
                "Discovery service has been disposed.",
                ErrorType.Validation));
        }

        try
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var timeProvider = serviceProvider.GetRequiredService<SaveState.Core.Common.Services.ITimeProvider>();

            // Try Tobii first (highest quality)
            var tobiiLogger = loggerFactory.CreateLogger<TobiiEyeTrackingProvider>();
            var tobiiProvider = new TobiiEyeTrackingProvider(tobiiLogger, timeProvider);
            
            if (tobiiProvider.IsAvailable)
            {
                _disposables.Add(tobiiProvider);
                _logger.LogInformation("Selected Tobii as preferred eye-tracking provider");
                return Task.FromResult(Result.Success<IEyeTrackingMonitor>(tobiiProvider));
            }
            
            tobiiProvider.Dispose();

            // Try Windows Eye Control
            if (OperatingSystem.IsWindows())
            {
                var windowsLogger = loggerFactory.CreateLogger<WindowsEyeTrackingMonitor>();
                var windowsProvider = new WindowsEyeTrackingMonitor(windowsLogger, timeProvider);
                
                if (windowsProvider.IsAvailable)
                {
                    _disposables.Add(windowsProvider);
                    _logger.LogInformation("Selected Windows Eye Control as preferred eye-tracking provider");
                    return Task.FromResult(Result.Success<IEyeTrackingMonitor>(windowsProvider));
                }
                
                windowsProvider.Dispose();
            }

            // Fall back to no-op
            var noOpLogger = loggerFactory.CreateLogger<NoOpEyeTrackingMonitor>();
            var noOpProvider = new NoOpEyeTrackingMonitor(noOpLogger);
            
            _logger.LogInformation("No eye-tracking devices available, using no-op provider");
            return Task.FromResult(Result.Success<IEyeTrackingMonitor>(noOpProvider));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preferred eye-tracking provider");
            return Task.FromResult(Result.Failure<IEyeTrackingMonitor>(
                $"Failed to get provider: {ex.Message}",
                ErrorType.Internal));
        }
    }

    /// <summary>
    /// Checks if any eye-tracking device is available.
    /// </summary>
    public bool IsAnyDeviceAvailable()
    {
        if (_isDisposed)
        {
            return false;
        }

        try
        {
            // Quick check for Tobii
            var tobiiAvailable = CheckTobiiAvailable();
            if (tobiiAvailable)
            {
                return true;
            }

            // Check Windows Eye Control
            if (OperatingSystem.IsWindows())
            {
                return CheckWindowsEyeControlAvailable();
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking eye-tracking availability");
            return false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        foreach (var disposable in _disposables)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing eye-tracking provider");
            }
        }

        _disposables.Clear();
    }

    private void DiscoverTobiiDevices(List<EyeTrackingDevice> devices)
    {
        try
        {
            // Check if Tobii SDK is available
            var isTobiiAvailable = CheckTobiiAvailable();
            
            if (!isTobiiAvailable)
            {
                return;
            }

            // Try to get device information from Tobii SDK
            // This would use the actual Tobii SDK to enumerate devices
            var deviceInfo = QueryTobiiDeviceInfo();
            
            if (deviceInfo != null)
            {
                devices.Add(new EyeTrackingDevice(
                    Id: $"tobii-{deviceInfo.SerialNumber}",
                    Name: deviceInfo.DeviceName,
                    Type: EyeTrackingDeviceType.Tobii,
                    IsAvailable: true,
                    SampleRate: deviceInfo.SampleRate,
                    ConnectionType: "USB",
                    Capabilities: new[] { "GazeTracking", "HeadTracking", "SmartPause" }));
            }
            else
            {
                // Add generic Tobii device entry
                devices.Add(new EyeTrackingDevice(
                    Id: "tobii-generic",
                    Name: "Tobii Eye Tracker (Generic)",
                    Type: EyeTrackingDeviceType.Tobii,
                    IsAvailable: true,
                    SampleRate: 90,
                    ConnectionType: "USB",
                    Capabilities: new[] { "GazeTracking", "SmartPause" }));
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error discovering Tobii devices");
        }
    }

    private void DiscoverWindowsEyeControlDevices(List<EyeTrackingDevice> devices)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            var isAvailable = CheckWindowsEyeControlAvailable();
            
            if (!isAvailable)
            {
                return;
            }

            devices.Add(new EyeTrackingDevice(
                Id: "windows-eye-control",
                Name: "Windows Eye Control",
                Type: EyeTrackingDeviceType.WindowsEyeControl,
                IsAvailable: true,
                SampleRate: 30, // Windows Eye Control typically uses lower sample rate
                ConnectionType: "System",
                Capabilities: new[] { "GazeTracking", "SystemIntegration" }));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error discovering Windows Eye Control");
        }
    }

    private static bool CheckTobiiAvailable()
    {
        try
        {
            // Check if Tobii.Interaction assembly is available
            var assembly = System.Reflection.Assembly.Load("Tobii.Interaction");
            return assembly != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckWindowsEyeControlAvailable()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            // Check Windows version (Eye Control requires Windows 10 build 15063+)
            var osVersion = Environment.OSVersion.Version;
            if (osVersion.Major < 10 || osVersion.Build < 15063)
            {
                return false;
            }

            // Check registry for Eye Control settings
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\EyeControl");
            
            return key != null;
        }
        catch
        {
            return false;
        }
    }

    private static EyeTrackerDeviceInfo? QueryTobiiDeviceInfo()
    {
        try
        {
            // This would use the actual Tobii SDK to query device information
            // For now, return null to indicate we couldn't query specific info
            return null;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Represents a discovered eye-tracking device.
/// </summary>
public sealed record EyeTrackingDevice(
    string Id,
    string Name,
    EyeTrackingDeviceType Type,
    bool IsAvailable,
    int SampleRate,
    string ConnectionType,
    IReadOnlyList<string> Capabilities);

/// <summary>
/// Types of eye-tracking devices.
/// </summary>
public enum EyeTrackingDeviceType
{
    Tobii,
    WindowsEyeControl,
    Eyetribe,
    GazePoint,
    Generic
}
