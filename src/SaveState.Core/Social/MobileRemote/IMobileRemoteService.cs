using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Core.Social.MobileRemote;

/// <summary>
/// Service for managing mobile remote control functionality.
/// </summary>
public interface IMobileRemoteService
{
    /// <summary>
    /// Starts the mobile remote server.
    /// </summary>
    Task<Result<RemoteServerInfo>> StartServerAsync(RemoteServerConfiguration config, CancellationToken ct = default);

    /// <summary>
    /// Stops the mobile remote server.
    /// </summary>
    Task<Result> StopServerAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current server status.
    /// </summary>
    Task<Result<RemoteServerStatus>> GetServerStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Generates a pairing code for a new device.
    /// </summary>
    Task<Result<PairingCode>> GeneratePairingCodeAsync(TimeSpan? expiry = null, CancellationToken ct = default);

    /// <summary>
    /// Validates a pairing code and creates a device connection.
    /// </summary>
    Task<Result<PairedDevice>> ValidatePairingCodeAsync(string code, string deviceName, string deviceType, CancellationToken ct = default);

    /// <summary>
    /// Revokes pairing for a device.
    /// </summary>
    Task<Result> RevokeDeviceAsync(string deviceId, CancellationToken ct = default);

    /// <summary>
    /// Lists all paired devices.
    /// </summary>
    Task<Result<IReadOnlyList<PairedDevice>>> GetPairedDevicesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the currently connected controller device.
    /// </summary>
    Task<Result<PairedDevice?>> GetActiveControllerAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the active controller device.
    /// </summary>
    Task<Result> SetActiveControllerAsync(string deviceId, CancellationToken ct = default);

    /// <summary>
    /// Sends haptic feedback to a device.
    /// </summary>
    Task<Result> SendHapticFeedbackAsync(string deviceId, HapticPattern pattern, CancellationToken ct = default);

    /// <summary>
    /// Event raised when a device connects.
    /// </summary>
    event EventHandler<DeviceConnectedEventArgs>? DeviceConnected;

    /// <summary>
    /// Event raised when a device disconnects.
    /// </summary>
    event EventHandler<DeviceDisconnectedEventArgs>? DeviceDisconnected;

    /// <summary>
    /// Event raised when controller input is received.
    /// </summary>
    event EventHandler<ControllerInputReceivedEventArgs>? ControllerInputReceived;
}

/// <summary>
/// Remote server configuration.
/// </summary>
public sealed record RemoteServerConfiguration(
    int Port,
    bool UseHttps,
    bool RequireAuthentication,
    int MaxConnectedDevices,
    TimeSpan PairingCodeExpiry,
    string? CustomHostname = null);

/// <summary>
/// Remote server information.
/// </summary>
public sealed record RemoteServerInfo(
    string ServerId,
    string Address,
    int Port,
    bool IsRunning,
    DateTime StartedAt);

/// <summary>
/// Remote server status.
/// </summary>
public sealed record RemoteServerStatus(
    bool IsRunning,
    string Address,
    int Port,
    int ConnectedDevices,
    int PairedDevices,
    DateTime? StartedAt,
    TimeSpan Uptime);

/// <summary>
/// Pairing code for device pairing.
/// </summary>
public sealed record PairingCode(
    string Code,
    DateTime ExpiresAt,
    TimeSpan ValidFor,
    bool IsUsed);

/// <summary>
/// Paired device information.
/// </summary>
public sealed record PairedDevice(
    string Id,
    string Name,
    string DeviceType,
    string? DeviceModel,
    bool IsConnected,
    bool IsActiveController,
    DeviceCapabilities Capabilities,
    DateTime PairedAt,
    DateTime? LastConnectedAt = null,
    string? IpAddress = null);

/// <summary>
/// Device capabilities.
/// </summary>
public sealed record DeviceCapabilities(
    bool SupportsTouchInput,
    bool SupportsMotionControls,
    bool SupportsHapticFeedback,
    bool SupportsSecondScreen,
    bool SupportsVoiceInput,
    int ScreenWidth,
    int ScreenHeight);

/// <summary>
/// Haptic feedback patterns.
/// </summary>
public enum HapticPattern
{
    Light,
    Medium,
    Heavy,
    Success,
    Error,
    Warning,
    Selection
}

/// <summary>
/// Event args for device connected events.
/// </summary>
public sealed class DeviceConnectedEventArgs : EventArgs
{
    public string DeviceId { get; }
    public string DeviceName { get; }
    public string IpAddress { get; }
    public DateTime ConnectedAt { get; }

    public DeviceConnectedEventArgs(string deviceId, string deviceName, string ipAddress, ITimeProvider? timeProvider = null)
    {
        DeviceId = deviceId;
        DeviceName = deviceName;
        IpAddress = ipAddress;
        ConnectedAt = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}

/// <summary>
/// Event args for device disconnected events.
/// </summary>
public sealed class DeviceDisconnectedEventArgs : EventArgs
{
    public string DeviceId { get; }
    public DisconnectReason Reason { get; }
    public DateTime DisconnectedAt { get; }

    public DeviceDisconnectedEventArgs(string deviceId, DisconnectReason reason, ITimeProvider? timeProvider = null)
    {
        DeviceId = deviceId;
        Reason = reason;
        DisconnectedAt = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}

/// <summary>
/// Event args for controller input received events.
/// </summary>
public sealed class ControllerInputReceivedEventArgs : EventArgs
{
    public string DeviceId { get; }
    public ControllerInput Input { get; }
    public DateTime ReceivedAt { get; }

    public ControllerInputReceivedEventArgs(string deviceId, ControllerInput input, ITimeProvider? timeProvider = null)
    {
        DeviceId = deviceId;
        Input = input;
        ReceivedAt = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}

/// <summary>
/// Disconnect reasons.
/// </summary>
public enum DisconnectReason
{
    ClientDisconnected,
    Timeout,
    ServerShutdown,
    Revoked,
    Error
}
