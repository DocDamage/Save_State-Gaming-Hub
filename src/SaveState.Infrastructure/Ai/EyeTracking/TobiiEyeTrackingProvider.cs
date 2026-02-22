using Microsoft.Extensions.Logging;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Infrastructure.Ai.EyeTracking;

/// <summary>
/// Tobii eye-tracking SDK provider implementation for Smart Pause.
/// Uses Tobii.Interaction or Tobii.Research SDK when available.
/// </summary>
public sealed class TobiiEyeTrackingProvider : IEyeTrackingMonitor, IDisposable
{
    private readonly ILogger<TobiiEyeTrackingProvider> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly TobiiSdkAdapter _adapter;
    private bool _isMonitoring;
    private DateTime? _lookAwayStartedAtUtc;
    private DateTime _lastGazeDataReceivedAtUtc;
    private bool _isDisposed;
    private readonly object _stateLock = new();

    public TobiiEyeTrackingProvider(
        ILogger<TobiiEyeTrackingProvider> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _adapter = new TobiiSdkAdapter(logger, timeProvider);
    }

    /// <inheritdoc />
    public bool IsAvailable => _adapter.IsSdkAvailable && !_isDisposed;

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
    /// Gets information about the connected device, if any.
    /// </summary>
    public EyeTrackerDeviceInfo? DeviceInfo => _adapter.DeviceInfo;

    /// <inheritdoc />
    public Task<Result> StartMonitoringAsync(CancellationToken ct = default)
    {
        if (_isDisposed)
        {
            return Task.FromResult(Result.Failure(
                "Tobii provider has been disposed.",
                ErrorType.Validation));
        }

        if (!IsAvailable)
        {
            return Task.FromResult(Result.Failure(
                "Tobii eye tracking is not available. Ensure Tobii Eye Tracking Core Software is installed.",
                ErrorType.NotImplemented));
        }

        lock (_stateLock)
        {
            if (_isMonitoring)
            {
                return Task.FromResult(Result.Success());
            }
        }

        try
        {
            var initResult = _adapter.Initialize();
            if (initResult.IsFailure)
            {
                return Task.FromResult(initResult);
            }

            _adapter.GazeDataReceived += OnGazeDataReceived;
            _adapter.ConnectionStateChanged += OnConnectionStateChanged;
            
            var startResult = _adapter.StartTracking();
            if (!startResult.IsSuccess)
            {
                _adapter.GazeDataReceived -= OnGazeDataReceived;
                _adapter.ConnectionStateChanged -= OnConnectionStateChanged;
                return Task.FromResult(startResult);
            }

            lock (_stateLock)
            {
                _isMonitoring = true;
                _lookAwayStartedAtUtc = null;
            }
            
            _lastGazeDataReceivedAtUtc = _timeProvider.UtcNow;
            
            _logger.LogInformation(
                "Tobii eye-tracking monitoring started. Device: {DeviceName}, Sample rate: {SampleRate}Hz, Firmware: {FirmwareVersion}",
                _adapter.DeviceInfo?.DeviceName ?? "Unknown",
                _adapter.DeviceInfo?.SampleRate ?? 0,
                _adapter.DeviceInfo?.FirmwareVersion ?? "Unknown");
            
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Tobii eye-tracking monitoring");
            
            lock (_stateLock)
            {
                _isMonitoring = false;
            }
            
            return Task.FromResult(Result.Failure(
                $"Failed to start Tobii monitoring: {ex.Message}",
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
            _adapter.GazeDataReceived -= OnGazeDataReceived;
            _adapter.ConnectionStateChanged -= OnConnectionStateChanged;
            _adapter.StopTracking();
            
            _lookAwayStartedAtUtc = null;
            
            _logger.LogInformation("Tobii eye-tracking monitoring stopped");
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Tobii eye-tracking monitoring");
            return Task.FromResult(Result.Failure(
                $"Error stopping Tobii monitoring: {ex.Message}",
                ErrorType.External));
        }
    }

    /// <inheritdoc />
    public Task<Result<EyeTrackingSnapshot>> GetSnapshotAsync(CancellationToken ct = default)
    {
        if (_isDisposed)
        {
            return Task.FromResult(Result.Failure<EyeTrackingSnapshot>(
                "Tobii provider has been disposed.",
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
        var gazeData = _adapter.GetLatestGazeData();
        
        // Check if gaze data is stale (no data for > 2 seconds)
        var dataAge = nowUtc - _lastGazeDataReceivedAtUtc;
        if (dataAge > TimeSpan.FromSeconds(2))
        {
            _logger.LogWarning("Tobii gaze data is stale ({DataAgeMs}ms old)", dataAge.TotalMilliseconds);
        }

        // Determine if user is looking at screen based on gaze data
        // A gaze point is considered "on screen" if:
        // 1. The data is valid (eyes detected)
        // 2. The gaze point is within normalized screen bounds [0,1]
        // 3. The data is not stale
        var isLookingAtScreen = gazeData.IsValid && 
                               gazeData.IsOnScreen && 
                               dataAge <= TimeSpan.FromSeconds(1) &&
                               gazeData.Confidence >= 0.5f;

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

        // Calculate confidence based on data quality
        var confidence = CalculateConfidence(gazeData, dataAge);

        var snapshot = new EyeTrackingSnapshot(
            CapturedAtUtc: nowUtc,
            IsLookingAtScreen: isLookingAtScreen,
            LookAwayDurationSeconds: lookAwaySeconds,
            Confidence: confidence,
            Source: $"Tobii-{_adapter.DeviceInfo?.DeviceName ?? "Unknown"}");

        return Task.FromResult(Result.Success(snapshot));
    }

    /// <summary>
    /// Gets diagnostic information about the eye tracker state.
    /// </summary>
    public EyeTrackerDiagnostics GetDiagnostics()
    {
        var nowUtc = _timeProvider.UtcNow;
        var gazeData = _adapter.GetLatestGazeData();
        
        return new EyeTrackerDiagnostics(
            IsAvailable: IsAvailable,
            IsMonitoring: IsMonitoring,
            DeviceInfo: _adapter.DeviceInfo,
            LastGazeDataReceivedAtUtc: _lastGazeDataReceivedAtUtc,
            DataAgeMs: (nowUtc - _lastGazeDataReceivedAtUtc).TotalMilliseconds,
            LatestGazeData: gazeData,
            ConnectionState: _adapter.ConnectionState,
            SdkVersion: _adapter.SdkVersion);
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
            _adapter.GazeDataReceived -= OnGazeDataReceived;
            _adapter.ConnectionStateChanged -= OnConnectionStateChanged;
            _adapter.Dispose();
            
            _logger.LogInformation("Tobii eye-tracking provider disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Tobii eye-tracking provider");
        }
    }

    private void OnGazeDataReceived(object? sender, GazeDataEventArgs e)
    {
        _lastGazeDataReceivedAtUtc = _timeProvider.UtcNow;
        
        // Log warnings for low confidence data
        if (e.Data.IsValid && e.Data.Confidence < 0.6f)
        {
            _logger.LogDebug("Low confidence gaze data: {Confidence:F2}", e.Data.Confidence);
        }
    }

    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        if (e.IsConnected)
        {
            _logger.LogInformation("Tobii eye tracker connected: {DeviceName}", e.DeviceName ?? "Unknown");
        }
        else
        {
            _logger.LogWarning("Tobii eye tracker disconnected");
            
            lock (_stateLock)
            {
                _isMonitoring = false;
            }
        }
    }

    private static float CalculateConfidence(GazeData gazeData, TimeSpan dataAge)
    {
        if (!gazeData.IsValid)
        {
            return 0.0f;
        }

        var baseConfidence = gazeData.Confidence;
        
        // Reduce confidence for stale data
        if (dataAge > TimeSpan.FromMilliseconds(100))
        {
            var ageFactor = Math.Max(0.5f, 1.0f - (float)(dataAge.TotalMilliseconds / 2000.0));
            baseConfidence *= ageFactor;
        }

        // Reduce confidence if gaze is near screen edges
        if (gazeData.IsOnScreen)
        {
            var edgeDistanceX = Math.Min(gazeData.X, 1.0f - gazeData.X);
            var edgeDistanceY = Math.Min(gazeData.Y, 1.0f - gazeData.Y);
            var minEdgeDistance = Math.Min(edgeDistanceX, edgeDistanceY);
            
            if (minEdgeDistance < 0.05f)
            {
                baseConfidence *= 0.8f; // Near edge, slightly less confident
            }
        }

        return Math.Clamp(baseConfidence, 0.0f, 1.0f);
    }

    /// <summary>
    /// Internal adapter class that abstracts the actual Tobii SDK.
    /// This allows the provider to work both with and without the SDK installed.
    /// </summary>
    private sealed class TobiiSdkAdapter : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ITimeProvider _timeProvider;
        private GazeData _latestGazeData;
        private bool _isTracking;
        private readonly object _dataLock = new();
        
        // SDK-related fields - these are lazy-initialized
        private dynamic? _host;
        private dynamic? _gazePointDataStream;
        private bool _isSdkLoaded;

        public TobiiSdkAdapter(ILogger logger, ITimeProvider timeProvider)
        {
            _logger = logger;
            _timeProvider = timeProvider;
            _latestGazeData = new GazeData();
            
            // Check SDK availability
            IsSdkAvailable = CheckSdkAvailability();
            SdkVersion = GetSdkVersion();
        }

        public bool IsSdkAvailable { get; }
        public string SdkVersion { get; }
        public EyeTrackerDeviceInfo? DeviceInfo { get; private set; }
        public EyeTrackerConnectionState ConnectionState { get; private set; }

        public event EventHandler<GazeDataEventArgs>? GazeDataReceived;
        public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

        public Result Initialize()
        {
            if (!IsSdkAvailable)
            {
                return Result.Failure(
                    "Tobii SDK is not available.",
                    ErrorType.NotImplemented);
            }

            try
            {
                InitializeSdk();
                
                // Query device information
                DeviceInfo = QueryDeviceInfo();
                
                if (DeviceInfo == null)
                {
                    return Result.Failure(
                        "No Tobii eye-tracking device found.",
                        ErrorType.NotFound);
                }

                ConnectionState = EyeTrackerConnectionState.Connected;
                
                _logger.LogDebug(
                    "Tobii SDK initialized. Device: {Device}, Firmware: {Firmware}, Sample Rate: {SampleRate}Hz",
                    DeviceInfo.DeviceName,
                    DeviceInfo.FirmwareVersion,
                    DeviceInfo.SampleRate);
                
                return Result.Success();
            }
            catch (DllNotFoundException ex)
            {
                _logger.LogError(ex, "Tobii SDK DLL not found. Install Tobii Eye Tracking Core Software.");
                return Result.Failure(
                    "Tobii SDK not found. Please install Tobii Eye Tracking Core Software.",
                    ErrorType.NotImplemented);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Tobii SDK");
                return Result.Failure(
                    $"Failed to initialize Tobii SDK: {ex.Message}",
                    ErrorType.External);
            }
        }

        public Result StartTracking()
        {
            if (!IsSdkAvailable || _host == null)
            {
                return Result.Failure(
                    "Tobii SDK is not initialized.",
                    ErrorType.Validation);
            }

            try
            {
                // Start gaze data stream
                StartGazeDataStream();
                
                _isTracking = true;
                
                _logger.LogDebug("Tobii gaze data stream started");
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start Tobii gaze data stream");
                return Result.Failure(
                    $"Failed to start gaze tracking: {ex.Message}",
                    ErrorType.External);
            }
        }

        public void StopTracking()
        {
            if (!_isTracking)
            {
                return;
            }

            try
            {
                StopGazeDataStream();
                _isTracking = false;
                
                _logger.LogDebug("Tobii gaze data stream stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping Tobii gaze data stream");
            }
        }

        public GazeData GetLatestGazeData()
        {
            lock (_dataLock)
            {
                return _latestGazeData;
            }
        }

        public void Dispose()
        {
            StopTracking();
            
            try
            {
                if (_gazePointDataStream != null)
                {
                    _gazePointDataStream = null;
                }
                
                if (_host != null)
                {
                    // Disable the host
                    try { _host.DisableConnection(); } catch { /* Ignore */ }
                    _host = null;
                }
                
                _isSdkLoaded = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing Tobii SDK adapter");
            }
        }

        private bool CheckSdkAvailability()
        {
            try
            {
                // Try to load the Tobii.Interaction assembly
                var assembly = System.Reflection.Assembly.Load("Tobii.Interaction");
                return assembly != null;
            }
            catch
            {
                return false;
            }
        }

        private string GetSdkVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.Load("Tobii.Interaction");
                return assembly?.GetName()?.Version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Not Available";
            }
        }

        private void InitializeSdk()
        {
            if (_isSdkLoaded)
            {
                return;
            }

            // Use dynamic to avoid compile-time dependency on Tobii SDK
            // This allows the code to compile even without the SDK installed
            var interactionAssembly = System.Reflection.Assembly.Load("Tobii.Interaction");
            var hostType = interactionAssembly.GetType("Tobii.Interaction.Host");
            
            if (hostType == null)
            {
                throw new InvalidOperationException("Could not find Tobii.Interaction.Host type.");
            }

            _host = Activator.CreateInstance(hostType);
            
            if (_host == null)
            {
                throw new InvalidOperationException("Failed to create Tobii host instance.");
            }

            _isSdkLoaded = true;
        }

        private EyeTrackerDeviceInfo? QueryDeviceInfo()
        {
            try
            {
                if (_host == null)
                {
                    return null;
                }

                // Try to get eye tracker device information
                // This uses reflection to access Tobii SDK properties
                var eyeTrackingDevice = _host.EyeTrackingDevice;
                
                if (eyeTrackingDevice == null)
                {
                    return null;
                }

                var deviceName = eyeTrackingDevice.DeviceName ?? "Tobii Eye Tracker";
                var firmwareVersion = eyeTrackingDevice.FirmwareVersion ?? "Unknown";
                var sampleRate = 90; // Default sample rate

                // Try to get the actual sample rate if available
                try
                {
                    var capabilities = eyeTrackingDevice.Capabilities;
                    if (capabilities != null)
                    {
                        // Sample rate might be available in capabilities
                        sampleRate = capabilities.SampleRate;
                    }
                }
                catch { /* Use default */ }

                return new EyeTrackerDeviceInfo(
                    DeviceName: deviceName,
                    FirmwareVersion: firmwareVersion,
                    SampleRate: sampleRate,
                    SerialNumber: eyeTrackingDevice.SerialNumber ?? "Unknown");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not query Tobii device info");
                return null;
            }
        }

        private void StartGazeDataStream()
        {
            if (_host == null)
            {
                throw new InvalidOperationException("Tobii host is not initialized.");
            }

            var interactionAssembly = System.Reflection.Assembly.Load("Tobii.Interaction");
            var gazePointDataStreamType = interactionAssembly.GetType("Tobii.Interaction.GazePointDataStream");
            
            if (gazePointDataStreamType == null)
            {
                throw new InvalidOperationException("Could not find GazePointDataStream type.");
            }

            // Create gaze point data stream
            _gazePointDataStream = Activator.CreateInstance(
                gazePointDataStreamType, 
                _host,
                GazePointDataMode.LightlyFiltered);

            if (_gazePointDataStream == null)
            {
                throw new InvalidOperationException("Failed to create gaze point data stream.");
            }

            // Subscribe to gaze data events using reflection
            var gazePointEvent = gazePointDataStreamType.GetEvent("GazePoint");
            if (gazePointEvent != null)
            {
                var handler = new EventHandler<dynamic>((sender, e) =>
                {
                    try
                    {
                        var x = (float)e.X;
                        var y = (float)e.Y;
                        var timestamp = (long)e.Timestamp;
                        
                        // Convert to normalized coordinates (0-1)
                        // Tobii provides screen coordinates, we need to normalize
                        var screenWidth = GetPrimaryScreenWidth();
                        var screenHeight = GetPrimaryScreenHeight();
                        
                        var normalizedX = screenWidth > 0 ? x / screenWidth : 0.5f;
                        var normalizedY = screenHeight > 0 ? y / screenHeight : 0.5f;
                        
                        // Check if on screen
                        var isOnScreen = normalizedX >= 0 && normalizedX <= 1 && 
                                        normalizedY >= 0 && normalizedY <= 1;
                        
                        var gazeData = new GazeData
                        {
                            IsValid = true,
                            IsOnScreen = isOnScreen,
                            X = Math.Clamp(normalizedX, 0f, 1f),
                            Y = Math.Clamp(normalizedY, 0f, 1f),
                            Confidence = 0.85f, // Tobii SDK doesn't provide direct confidence, use default
                            TimestampUtc = _timeProvider.UtcNow
                        };

                        lock (_dataLock)
                        {
                            _latestGazeData = gazeData;
                        }

                        GazeDataReceived?.Invoke(this, new GazeDataEventArgs(gazeData));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing gaze data");
                    }
                });

                gazePointEvent.AddEventHandler(_gazePointDataStream, handler);
            }

            // Enable the gaze point data stream
            try
            {
                var enableMethod = gazePointDataStreamType.GetMethod("Enable");
                enableMethod?.Invoke(_gazePointDataStream, null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not enable gaze point data stream");
            }
        }

        private void StopGazeDataStream()
        {
            if (_gazePointDataStream == null)
            {
                return;
            }

            try
            {
                var interactionAssembly = System.Reflection.Assembly.Load("Tobii.Interaction");
                var gazePointDataStreamType = interactionAssembly.GetType("Tobii.Interaction.GazePointDataStream");
                
                if (gazePointDataStreamType != null)
                {
                    var disableMethod = gazePointDataStreamType.GetMethod("Disable");
                    disableMethod?.Invoke(_gazePointDataStream, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disabling gaze data stream");
            }
            
            _gazePointDataStream = null;
        }

        private static float GetPrimaryScreenWidth()
        {
            try
            {
                // Use reflection to avoid dependency on System.Windows.Forms or similar
                var screenType = Type.GetType("System.Windows.Forms.Screen, System.Windows.Forms");
                if (screenType != null)
                {
                    var primaryScreenProperty = screenType.GetProperty("PrimaryScreen");
                    var primaryScreen = primaryScreenProperty?.GetValue(null);
                    
                    if (primaryScreen != null)
                    {
                        var boundsProperty = screenType.GetProperty("Bounds");
                        var bounds = boundsProperty?.GetValue(primaryScreen);
                        
                        if (bounds != null)
                        {
                            var widthProperty = bounds.GetType().GetProperty("Width");
                            return (float)(widthProperty?.GetValue(bounds) ?? 1920f);
                        }
                    }
                }
                
                // Default fallback
                return 1920f;
            }
            catch
            {
                return 1920f;
            }
        }

        private static float GetPrimaryScreenHeight()
        {
            try
            {
                var screenType = Type.GetType("System.Windows.Forms.Screen, System.Windows.Forms");
                if (screenType != null)
                {
                    var primaryScreenProperty = screenType.GetProperty("PrimaryScreen");
                    var primaryScreen = primaryScreenProperty?.GetValue(null);
                    
                    if (primaryScreen != null)
                    {
                        var boundsProperty = screenType.GetProperty("Bounds");
                        var bounds = boundsProperty?.GetValue(primaryScreen);
                        
                        if (bounds != null)
                        {
                            var heightProperty = bounds.GetType().GetProperty("Height");
                            return (float)(heightProperty?.GetValue(bounds) ?? 1080f);
                        }
                    }
                }
                
                return 1080f;
            }
            catch
            {
                return 1080f;
            }
        }
    }

    /// <summary>
    /// Represents gaze point data from the eye tracker.
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
    /// Event arguments for gaze data received events.
    /// </summary>
    private sealed class GazeDataEventArgs : EventArgs
    {
        public GazeData Data { get; }
        public GazeDataEventArgs(GazeData data) => Data = data;
    }

    /// <summary>
    /// Event arguments for connection state changes.
    /// </summary>
    private sealed class ConnectionStateChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; }
        public string? DeviceName { get; }
        
        public ConnectionStateChangedEventArgs(bool isConnected, string? deviceName = null)
        {
            IsConnected = isConnected;
            DeviceName = deviceName;
        }
    }

    /// <summary>
    /// Gaze data filtering mode.
    /// </summary>
    private enum GazePointDataMode
    {
        Unfiltered,
        LightlyFiltered
    }
}

/// <summary>
/// Information about an eye tracker device.
/// </summary>
public sealed record EyeTrackerDeviceInfo(
    string DeviceName,
    string FirmwareVersion,
    int SampleRate,
    string SerialNumber);

/// <summary>
/// Connection state of the eye tracker.
/// </summary>
public enum EyeTrackerConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Error
}

/// <summary>
/// Diagnostic information about the eye tracker.
/// </summary>
public sealed record EyeTrackerDiagnostics(
    bool IsAvailable,
    bool IsMonitoring,
    EyeTrackerDeviceInfo? DeviceInfo,
    DateTime LastGazeDataReceivedAtUtc,
    double DataAgeMs,
    dynamic LatestGazeData,
    EyeTrackerConnectionState ConnectionState,
    string SdkVersion);
