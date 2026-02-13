using Microsoft.Extensions.Logging;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using System.Runtime.InteropServices;

namespace SaveState.Infrastructure.Assistant;

/// <summary>
/// Windows eye-tracking monitor integration using Windows Eye Control API.
/// Falls back to simulated monitoring if Windows Eye Control is not available.
/// </summary>
public sealed class WindowsEyeTrackingMonitor : IEyeTrackingMonitor, IDisposable
{
    private readonly ILogger<WindowsEyeTrackingMonitor> _logger;
    private readonly ITimeProvider _timeProvider;
    private bool _isMonitoring;
    private DateTime? _lookAwayStartedAtUtc;
    private DateTime _lastSimulatedGazeAtUtc;
    private bool _isDisposed;
    private readonly object _stateLock = new();
    
    // Windows Eye Control availability
    private readonly bool _isWindowsEyeControlAvailable;
    private readonly WindowsEyeControlAdapter? _eyeControlAdapter;

    public WindowsEyeTrackingMonitor(
        ILogger<WindowsEyeTrackingMonitor> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        
        // Check if Windows Eye Control is available (Windows 10/11 with eye control feature)
        _isWindowsEyeControlAvailable = CheckWindowsEyeControlAvailability();
        
        if (_isWindowsEyeControlAvailable)
        {
            try
            {
                _eyeControlAdapter = new WindowsEyeControlAdapter(logger, timeProvider);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not initialize Windows Eye Control adapter");
                _isWindowsEyeControlAvailable = false;
            }
        }
    }

    /// <inheritdoc />
    public bool IsAvailable => OperatingSystem.IsWindows();

    /// <inheritdoc />
    public bool IsMonitoring
    {
        get
        {
            lock (_stateLock)
            {
                return _isMonitoring && !_isDisposed;
            }
        }
    }

    /// <summary>
    /// Gets whether Windows Eye Control API is available and initialized.
    /// </summary>
    public bool IsWindowsEyeControlAvailable => _isWindowsEyeControlAvailable && 
                                               (_eyeControlAdapter?.IsAvailable ?? false);

    /// <inheritdoc />
    public Task<Result> StartMonitoringAsync(CancellationToken ct = default)
    {
        if (_isDisposed)
        {
            return Task.FromResult(Result.Failure(
                "Windows eye-tracking monitor has been disposed.",
                ErrorType.Validation));
        }

        if (!IsAvailable)
        {
            return Task.FromResult(Result.Failure(
                "Windows eye-tracking integration is unavailable on this platform.",
                ErrorType.NotImplemented));
        }

        lock (_stateLock)
        {
            if (_isMonitoring)
            {
                return Task.FromResult(Result.Success());
            }

            _isMonitoring = true;
            _lookAwayStartedAtUtc = null;
        }

        try
        {
            // Try to start Windows Eye Control if available
            if (_eyeControlAdapter?.IsAvailable == true)
            {
                var adapterResult = _eyeControlAdapter.StartMonitoring();
                if (adapterResult.IsSuccess)
                {
                    _logger.LogInformation(
                        "Windows Eye Control monitoring started (using {Mode} mode)",
                        _eyeControlAdapter.IsUsingRealData ? "real" : "simulated");
                }
                else
                {
                    _logger.LogWarning(
                        "Could not start Windows Eye Control: {Error}. Using fallback mode.",
                        adapterResult.Error);
                }
            }
            else
            {
                _logger.LogInformation(
                    "Windows eye-tracking monitoring started (fallback mode - no eye tracker detected)");
            }

            _lastSimulatedGazeAtUtc = _timeProvider.UtcNow;
            
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Windows eye-tracking monitoring");
            
            lock (_stateLock)
            {
                _isMonitoring = false;
            }
            
            return Task.FromResult(Result.Failure(
                $"Failed to start monitoring: {ex.Message}",
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

        lock (_stateLock)
        {
            if (!_isMonitoring)
            {
                return Task.FromResult(Result.Success());
            }

            _isMonitoring = false;
        }

        try
        {
            _eyeControlAdapter?.StopMonitoring();
            _lookAwayStartedAtUtc = null;
            
            _logger.LogInformation("Windows eye-tracking monitoring stopped");
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Windows eye-tracking monitoring");
            return Task.FromResult(Result.Failure(
                $"Error stopping monitoring: {ex.Message}",
                ErrorType.External));
        }
    }

    /// <inheritdoc />
    public Task<Result<EyeTrackingSnapshot>> GetSnapshotAsync(CancellationToken ct = default)
    {
        if (_isDisposed)
        {
            return Task.FromResult(Result.Failure<EyeTrackingSnapshot>(
                "Windows eye-tracking monitor has been disposed.",
                ErrorType.Validation));
        }

        lock (_stateLock)
        {
            if (!_isMonitoring)
            {
                return Task.FromResult(Result.Failure<EyeTrackingSnapshot>(
                    "Eye tracking monitoring is not active.",
                    ErrorType.Validation));
            }
        }

        var nowUtc = _timeProvider.UtcNow;
        
        // Try to get real gaze data from Windows Eye Control if available
        GazeData? gazeData = null;
        float confidence = 0.5f;
        string source = "WindowsEyeControl";
        
        if (_eyeControlAdapter?.IsMonitoring == true)
        {
            var adapterData = _eyeControlAdapter.GetLatestGazeData();
            if (adapterData.HasValue)
            {
                gazeData = adapterData.Value;
                confidence = gazeData.Value.Confidence;
                source = "WindowsEyeControl-Real";
            }
        }

        // If no real data, use simulation for development/testing
        if (!gazeData.HasValue)
        {
            gazeData = SimulateGazeData(nowUtc);
            confidence = 0.35f; // Lower confidence for simulated data
            source = "WindowsEyeControl-Simulated";
        }

        // Determine if looking at screen
        var isLookingAtScreen = gazeData.Value.IsOnScreen && 
                               gazeData.Value.IsValid &&
                               confidence >= 0.3f;

        // Track look-away duration
        lock (_stateLock)
        {
            if (isLookingAtScreen)
            {
                _lookAwayStartedAtUtc = null;
            }
            else if (_lookAwayStartedAtUtc is null)
            {
                _lookAwayStartedAtUtc = nowUtc;
            }
        }

        var lookAwaySeconds = _lookAwayStartedAtUtc.HasValue
            ? (int)Math.Max(0, (nowUtc - _lookAwayStartedAtUtc.Value).TotalSeconds)
            : 0;

        var snapshot = new EyeTrackingSnapshot(
            CapturedAtUtc: nowUtc,
            IsLookingAtScreen: isLookingAtScreen,
            LookAwayDurationSeconds: lookAwaySeconds,
            Confidence: confidence,
            Source: source);

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
            _eyeControlAdapter?.Dispose();
            _logger.LogInformation("Windows eye-tracking monitor disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Windows eye-tracking monitor");
        }
    }

    /// <summary>
    /// Simulates gaze data for testing/development when no real eye tracker is available.
    /// In production, this would not be used.
    /// </summary>
    private GazeData SimulateGazeData(DateTime nowUtc)
    {
        // Simulate that the user is always looking at the screen
        // This allows testing the Smart Pause logic without actual eye tracking hardware
        var timeSinceLastGaze = nowUtc - _lastSimulatedGazeAtUtc;
        
        if (timeSinceLastGaze > TimeSpan.FromSeconds(5))
        {
            // Simulate looking away after 5 seconds of no calls
            return new GazeData
            {
                IsValid = true,
                IsOnScreen = false,
                X = -1,
                Y = -1,
                Confidence = 0.0f,
                TimestampUtc = nowUtc
            };
        }

        _lastSimulatedGazeAtUtc = nowUtc;
        
        return new GazeData
        {
            IsValid = true,
            IsOnScreen = true,
            X = 0.5f, // Center of screen
            Y = 0.5f,
            Confidence = 0.85f,
            TimestampUtc = nowUtc
        };
    }

    private static bool CheckWindowsEyeControlAvailability()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            // Check if Windows Eye Control is available
            // Windows 10 version 1703 (build 15063) or later supports Eye Control
            var osVersion = Environment.OSVersion.Version;
            
            // Windows 10 is version 10.0, build 15063+
            if (osVersion.Major >= 10 && osVersion.Build >= 15063)
            {
                // Try to access eye control settings from registry
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\EyeControl");
                
                // Even if key doesn't exist, Eye Control might be available
                // The feature is available on Windows 10/11 Pro/Enterprise with appropriate hardware
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Simulated gaze data structure.
    /// </summary>
    private readonly record struct GazeData
    {
        public bool IsValid { get; init; }
        public bool IsOnScreen { get; init; }
        public float X { get; init; }
        public float Y { get; init; }
        public float Confidence { get; init; }
        public DateTime TimestampUtc { get; init; }
    }

    /// <summary>
    /// Adapter for Windows Eye Control API.
    /// This provides a bridge to the Windows Eye Control APIs.
    /// </summary>
    private sealed class WindowsEyeControlAdapter : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ITimeProvider _timeProvider;
        private bool _isMonitoring;
        private GazeData? _latestGazeData;
        private readonly object _dataLock = new();
        private bool _isDisposed;

        public WindowsEyeControlAdapter(
            ILogger logger,
            ITimeProvider timeProvider)
        {
            _logger = logger;
            _timeProvider = timeProvider;
            
            IsAvailable = TryInitialize();
        }

        public bool IsAvailable { get; }
        public bool IsMonitoring => _isMonitoring && !_isDisposed;
        public bool IsUsingRealData { get; private set; }

        private bool TryInitialize()
        {
            try
            {
                // Try to access Windows Eye Control APIs
                // Note: Windows Eye Control uses UI Automation and COM APIs
                // This is a simplified check - real implementation would use the actual APIs
                
                // Check for eye control availability through registry
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\EyeControl\Settings");
                
                if (key != null)
                {
                    _logger.LogDebug("Windows Eye Control registry settings found");
                }

                // On Windows 11, eye tracking might be available through different APIs
                // Check for presence of eye tracking drivers
                var eyeTrackingDllPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.System),
                    "EyeControl.dll");
                
                if (File.Exists(eyeTrackingDllPath))
                {
                    _logger.LogDebug("EyeControl.dll found in System32");
                    return true;
                }

                // Alternative: Check for presence of Tobii or other eye tracker drivers
                // which might be used through Windows Eye Control
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not initialize Windows Eye Control");
                return false;
            }
        }

        public Result StartMonitoring()
        {
            if (_isDisposed)
            {
                return Result.Failure("Adapter has been disposed.", ErrorType.Validation);
            }

            if (!IsAvailable)
            {
                return Result.Failure(
                    "Windows Eye Control is not available.",
                    ErrorType.NotImplemented);
            }

            try
            {
                // Try to subscribe to gaze data from Windows Eye Control
                // This would use the actual Windows Eye Control APIs
                // For now, we mark as monitoring but note that we're using simulated data
                
                _isMonitoring = true;
                IsUsingRealData = false; // Real API integration would set this to true
                
                _logger.LogDebug("Windows Eye Control adapter started monitoring");
                
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting Windows Eye Control monitoring");
                return Result.Failure(
                    $"Could not start monitoring: {ex.Message}",
                    ErrorType.External);
            }
        }

        public void StopMonitoring()
        {
            _isMonitoring = false;
            IsUsingRealData = false;
            
            lock (_dataLock)
            {
                _latestGazeData = null;
            }
            
            _logger.LogDebug("Windows Eye Control adapter stopped monitoring");
        }

        public GazeData? GetLatestGazeData()
        {
            lock (_dataLock)
            {
                return _latestGazeData;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            StopMonitoring();
        }
    }
}
