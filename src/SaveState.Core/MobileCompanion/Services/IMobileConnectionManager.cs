using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Models;

namespace SaveState.Core.MobileCompanion.Services;

/// <summary>
/// Interface for managing mobile companion connections via SignalR.
/// </summary>
public interface IMobileConnectionManager
{
    /// <summary>
    /// Event raised when a command is received from the mobile device.
    /// </summary>
    event EventHandler<RemoteCommandMessage>? OnCommandReceived;

    /// <summary>
    /// Event raised when the connection status changes.
    /// </summary>
    event EventHandler<ConnectionStatusChangedEventArgs>? OnStatusChanged;

    /// <summary>
    /// Event raised when a notification is received from the server.
    /// </summary>
    event EventHandler<CompanionNotification>? OnNotificationReceived;

    /// <summary>
    /// Event raised when the system status is updated.
    /// </summary>
    event EventHandler<SystemStatus>? OnStatusUpdateReceived;

    /// <summary>
    /// Event raised when the game changes.
    /// </summary>
    event EventHandler<GameSummary?>? OnGameChanged;

    /// <summary>
    /// Connects to the mobile companion hub using a pairing code.
    /// </summary>
    /// <param name="pairingCode">The pairing code to use.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ConnectAsync(string pairingCode);

    /// <summary>
    /// Connects using explicit connection information.
    /// </summary>
    /// <param name="info">The pairing information.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ConnectAsync(PairingInfo info);

    /// <summary>
    /// Disconnects from the mobile companion hub.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DisconnectAsync();

    /// <summary>
    /// Sends a command to the server.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <param name="parameters">Optional command parameters.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SendCommandAsync(RemoteControlCommand command, Dictionary<string, object>? parameters = null);

    /// <summary>
    /// Sends gamepad input to the server.
    /// </summary>
    /// <param name="input">The gamepad input.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SendInputAsync(GamepadInput input);

    /// <summary>
    /// Sends touchpad input to the server.
    /// </summary>
    /// <param name="input">The touchpad input.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SendInputAsync(TouchpadInput input);

    /// <summary>
    /// Sends keyboard input to the server.
    /// </summary>
    /// <param name="input">The keyboard input.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SendInputAsync(KeyboardInput input);

    /// <summary>
    /// Requests the current system status from the server.
    /// </summary>
    /// <returns>The current system status.</returns>
    Task<Result<SystemStatus>> GetSystemStatusAsync();

    /// <summary>
    /// Requests library synchronization from the server.
    /// </summary>
    /// <returns>The library sync information.</returns>
    Task<Result<LibrarySyncInfo>> SyncLibraryAsync();

    /// <summary>
    /// Gets the current connection status.
    /// </summary>
    ConnectionStatus Status { get; }

    /// <summary>
    /// Gets whether the connection is established and authenticated.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the current remote session information.
    /// </summary>
    RemoteSession? CurrentSession { get; }

    /// <summary>
    /// Gets the connection latency in milliseconds.
    /// </summary>
    int LatencyMs { get; }
}

/// <summary>
/// Event arguments for connection status changes.
/// </summary>
public class ConnectionStatusChangedEventArgs : EventArgs
{
    public ConnectionStatus OldStatus { get; set; }
    public ConnectionStatus NewStatus { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Configuration options for the mobile connection manager.
/// </summary>
public class MobileConnectionOptions
{
    /// <summary>
    /// Initial reconnection delay in milliseconds.
    /// </summary>
    public int ReconnectDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum reconnection delay in milliseconds.
    /// </summary>
    public int MaxReconnectDelayMs { get; set; } = 30000;

    /// <summary>
    /// Maximum number of reconnection attempts.
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 10;

    /// <summary>
    /// Heartbeat interval in seconds.
    /// </summary>
    public int HeartbeatIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to automatically reconnect on disconnection.
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// Whether to use message pack protocol for better performance.
    /// </summary>
    public bool UseMessagePack { get; set; } = true;

    /// <summary>
    /// Whether to use Server-Sent Events as a fallback.
    /// </summary>
    public bool EnableSseFallback { get; set; } = true;
}
