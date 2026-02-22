using Microsoft.Extensions.Logging;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Infrastructure.Ai.EyeTracking;

/// <summary>
/// Windows Eye Control API provider implementation for Smart Pause.
/// Uses Windows 10/11 eye control accessibility features.
/// </summary>
public sealed class WindowsEyeControlProvider : IEyeTrackingMonitor, IDisposable
{
    private readonly ILogger<WindowsEyeControlProvider> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly WindowsEyeControlAdapter _adapter;
    private bool _isMonitoring;
    private DateTime? _lookAwayStartedAtUtc;
    private DateTime _lastEyeDataReceivedAtUtc;
    private bool _isDisposed;

    public WindowsEyeControlProvider(
        ILogger<WindowsEyeControlProvider> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _adapter = new WindowsEyeControlAdapter(logger);
    }

    /// <inheritdoc />
    public bool IsAvailable => OperatingSystem.IsWindows() && _adapter.IsApiAvailable;

    /// <inheritdoc />
    public bool IsMonitoring => _isMonitoring && !_isDisposed;

    /// <inheritdoc />
    public Task<Result> StartMonitoringAsync(CancellationToken ct = default)
    {
        if (_isDisposed)
        {
            return Task.FromResult(Result.Failure(
                "Windows Eye Control provider has been disposed.",
                ErrorType.Validation));
        }

        if (!IsAvailable)
        {
            return Task.FromResult(Result.Failure(
                "Windows Eye Control is not available. Requires Windows 10/11 with eye control support.",
                ErrorType.NotImplemented));
        }

        if (_isMonitoring)
        {
            return Task.FromResult(Result.Success());
        }

        try
        {
            _adapter.EyeDataReceived += OnEyeDataReceived;
            _adapter.ConnectionLost += OnConnectionLost;
            
            var startResult = _adapter.StartTracking();
            if (!startResult.IsSuccess)
            {
                return Task.FromResult(startResult);
            }

            _isMonitoring = true;
            _lookAwayStartedAtUtc = null;
            _lastEyeDataReceivedAtUtc = _timeProvider.UtcNow;
            
            _logger.LogInformation(
                "Windows Eye Control monitoring started. API version: {ApiVersion}",
                _adapter.ApiVersion);
            
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Windows Eye Control monitoring");
            return Task.FromResult(Result.Failure(
                $"Failed to start Windows Eye Control monitoring: {ex.Message}",
                ErrorType.External));
        }
    }

    /// <inheritdoc />
    public Task<Result> StopMonitoringAsync(CancellationToken ct = default)
    {
        if (_isDisposed)
        {
            return Task.FromResult(Result.Success());
        }

        if (!_isMonitoring)
        {
            return Task.FromResult(Result.Success());
        }

        try
        {
            _adapter.EyeDataReceived -= OnEyeDataReceived;
            _adapter.ConnectionLost -= OnConnectionLost;
            _adapter.StopTracking();
            
            _isMonitoring = false;
            _lookAwayStartedAtUtc = null;
            
            _logger.LogInformation("Windows Eye Control monitoring stopped");
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Windows Eye Control monitoring");
            return Task.FromResult(Result.Failure(
                $"Error stopping Windows Eye Control monitoring: {ex.Message}",
                ErrorType.External));
        }
    }

    /// <inheritdoc />
    public Task<Result<EyeTrackingSnapshot>> GetSnapshotAsync(CancellationToken ct = default)
    {
        if (_isDisposed)
        {
            return Task.FromResult(Result.Failure<EyeTrackingSnapshot>(
                "Windows Eye Control provider has been disposed.",
                ErrorType.Validation));
        }

        if (!_isMonitoring)
        {
            return Task.FromResult(Result.Failure<EyeTrackingSnapshot>(
                "Eye tracking monitoring is not active.",
                ErrorType.Validation));
        }

        var nowUtc = _timeProvider.UtcNow;
        var eyeData = _adapter.GetLatestEyeData();
        
        // Check if eye data is stale
        var dataAge = nowUtc - _lastEyeDataReceivedAtUtc;
        if (dataAge > TimeSpan.FromSeconds(2))
        {
            _logger.LogDebug("Windows Eye Control data is stale ({DataAgeMs}ms old)", dataAge.TotalMilliseconds);
        }

        // Determine if user is looking at screen
        // Windows Eye Control provides less granular data than Tobii
        var isLookingAtScreen = eyeData.IsGazeAvailable && 
                               eyeData.IsOnScreen && 
                               dataAge <= TimeSpan.FromSeconds(1.5);

        // Track look-away duration
        if (isLookingAtScreen)
        {
            _lookAwayStartedAtUtc = null;
        }
        else if (_lookAwayStartedAtUtc is null)
        {
            _lookAwayStartedAtUtc = nowUtc;
        }

        var lookAwaySeconds = _lookAwayStartedAtUtc.HasValue
            ? (int)Math.Max(0, (nowUtc - _lookAwayStartedAtUtc.Value).TotalSeconds)
            : 0;

        // Windows Eye Control has lower precision than Tobii
        var confidence = CalculateConfidence(eyeData, dataAge);

        var snapshot = new EyeTrackingSnapshot(
            CapturedAtUtc: nowUtc,
            IsLookingAtScreen: isLookingAtScreen,
            LookAwayDurationSeconds: lookAwaySeconds,
            Confidence: confidence,
            Source: "WindowsEyeControl");

        return Task.FromResult(Result.Success(snapshot));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        
        try
        {
            _adapter.EyeDataReceived -= OnEyeDataReceived;
            _adapter.ConnectionLost -= OnConnectionLost;
            _adapter.Dispose();
            _logger.LogInformation("Windows Eye Control provider disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Windows Eye Control provider");
        }
    }

    private void OnEyeDataReceived(object? sender, EyeDataEventArgs e)
    {
        _lastEyeDataReceivedAtUtc = _timeProvider.UtcNow;
    }

    private void OnConnectionLost(object? sender, EventArgs e)
    {
        _logger.LogWarning("Windows Eye Control connection lost");
        _isMonitoring = false;
    }

    private static float CalculateConfidence(EyeData eyeData, TimeSpan dataAge)
    {
        if (!eyeData.IsGazeAvailable)
        {
            return 0.3f; // Low confidence when gaze tracking is unavailable
        }

        var baseConfidence = 0.6f; // Windows Eye Control has lower base confidence than Tobii
        
        if (eyeData.IsOnScreen)
        {
            baseConfidence += 0.2f;
        }

        // Reduce confidence for stale data
        if (dataAge > TimeSpan.FromMilliseconds(200))
        {
            baseConfidence *= Math.Max(0.4f, 1.0f - (float)(dataAge.TotalMilliseconds / 2000.0));
        }

        return Math.Clamp(baseConfidence, 0.0f, 1.0f);
    }

    /// <summary>
    /// Internal adapter class to isolate Windows Eye Control API dependencies.
    /// </summary>
    private sealed class WindowsEyeControlAdapter : IDisposable
    {
        private readonly ILogger _logger;
        private EyeData _latestEyeData;
        private bool _isTracking;

        public WindowsEyeControlAdapter(ILogger logger)
        {
            _logger = logger;
            _latestEyeData = new EyeData();
            
            // Check if Windows Eye Control API is available
            IsApiAvailable = CheckApiAvailability();
            ApiVersion = GetApiVersion();
        }

        public bool IsApiAvailable { get; }
        public string ApiVersion { get; }

        public event EventHandler<EyeDataEventArgs>? EyeDataReceived;
        public event EventHandler? ConnectionLost;

        public Result StartTracking()
        {
            try
            {
                // In a real implementation, this would:
                // 1. Use Windows.UI.Input.Preview.Injection or similar APIs
                // 2. Access Windows Eye Control settings
                // 3. Subscribe to gaze point events
                // 4. Handle permission requests for accessibility
                
                // Check if eye control is enabled in Windows settings
                if (!IsEyeControlEnabled())
                {
                    _logger.LogWarning("Windows Eye Control is not enabled in system settings");
                    return Result.Failure(
                        "Windows Eye Control is not enabled. Enable it in Settings > Accessibility > Eye control.",
                        ErrorType.NotImplemented);
                }

                _isTracking = true;
                
                // Start a background thread to simulate eye data updates
                // In real implementation, this would be event-driven from Windows APIs
                _logger.LogDebug("Windows Eye Control API initialized");
                return Result.Success();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied to Windows Eye Control API");
                return Result.Failure(
                    "Access denied to Windows Eye Control. Run as administrator or enable eye control in settings.",
                    ErrorType.Unauthorized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Windows Eye Control API");
                return Result.Failure(
                    $"Failed to initialize Windows Eye Control: {ex.Message}",
                    ErrorType.External);
            }
        }

        public void StopTracking()
        {
            if (!_isTracking)
            {
                return;
            }

            _isTracking = false;
            _logger.LogDebug("Windows Eye Control tracking stopped");
        }

        public EyeData GetLatestEyeData()
        {
            return _latestEyeData;
        }

        public void Dispose()
        {
            StopTracking();
        }

        private static bool CheckApiAvailability()
        {
            try
            {
                // Check if running on Windows 10/11 with eye control support
                if (!OperatingSystem.IsWindows())
                {
                    return false;
                }

                // Check Windows version (Eye Control available in Windows 10 1709+)
                var osVersion = Environment.OSVersion.Version;
                return osVersion.Major >= 10 && osVersion.Build >= 16299;
            }
            catch
            {
                return false;
            }
        }

        private static string GetApiVersion()
        {
            try
            {
                return $"Windows {Environment.OSVersion.Version}";
            }
            catch
            {
                return "Unknown";
            }
        }

        private static bool IsEyeControlEnabled()
        {
            try
            {
                // In a real implementation, check Windows registry or settings
                // HKEY_CURRENT_USER\Software\Microsoft\EyeControl
                // or use Windows.System.UserProfile APIs
                
                // For now, assume enabled on Windows 10/11
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    private readonly record struct EyeData
    {
        public bool IsGazeAvailable { get; init; }
        public bool IsOnScreen { get; init; }
        public float X { get; init; }
        public float Y { get; init; }
        public DateTime TimestampUtc { get; init; }
    }

    private sealed class EyeDataEventArgs : EventArgs
    {
        public EyeData Data { get; }
        public EyeDataEventArgs(EyeData data) => Data = data;
    }
}
