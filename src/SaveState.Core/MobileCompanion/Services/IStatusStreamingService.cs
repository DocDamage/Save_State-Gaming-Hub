using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Models;

namespace SaveState.Core.MobileCompanion.Services;

/// <summary>
/// Interface for streaming real-time status updates to connected mobile devices.
/// </summary>
public interface IStatusStreamingService
{
    /// <summary>
    /// Starts streaming status updates for a specific session.
    /// </summary>
    /// <param name="sessionId">The session ID to start streaming for.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> StartStreamingAsync(Guid sessionId);

    /// <summary>
    /// Stops streaming status updates for a specific session.
    /// </summary>
    /// <param name="sessionId">The session ID to stop streaming for.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> StopStreamingAsync(Guid sessionId);

    /// <summary>
    /// Broadcasts the current system status to all connected devices.
    /// </summary>
    /// <param name="status">The system status to broadcast.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task BroadcastStatusAsync(SystemStatus status);

    /// <summary>
    /// Broadcasts a game change event to all connected devices.
    /// </summary>
    /// <param name="game">The game summary, or null if no game is active.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task BroadcastGameChangeAsync(GameSummary? game);

    /// <summary>
    /// Broadcasts a notification to all connected devices.
    /// </summary>
    /// <param name="notification">The notification to broadcast.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task BroadcastNotificationAsync(CompanionNotification notification);

    /// <summary>
    /// Broadcasts a library sync update to all connected devices.
    /// </summary>
    /// <param name="syncInfo">The library sync information.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task BroadcastLibrarySyncAsync(LibrarySyncInfo syncInfo);

    /// <summary>
    /// Gets a list of currently active streaming sessions.
    /// </summary>
    /// <returns>A list of active session IDs.</returns>
    IReadOnlyList<Guid> GetActiveSessions();

    /// <summary>
    /// Event raised when a device subscribes to streaming.
    /// </summary>
    event EventHandler<StreamingSessionEventArgs>? OnSessionStarted;

    /// <summary>
    /// Event raised when a device unsubscribes from streaming.
    /// </summary>
    event EventHandler<StreamingSessionEventArgs>? OnSessionEnded;
}

/// <summary>
/// Event arguments for streaming session events.
/// </summary>
public class StreamingSessionEventArgs : EventArgs
{
    public Guid SessionId { get; set; }
    public Guid DeviceId { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Configuration options for status streaming.
/// </summary>
public class StatusStreamingOptions
{
    /// <summary>
    /// The interval in seconds between system status updates.
    /// </summary>
    public int StatusUpdateIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// The maximum number of devices that can connect simultaneously.
    /// </summary>
    public int MaxConnectedDevices { get; set; } = 5;

    /// <summary>
    /// Whether to batch multiple updates together.
    /// </summary>
    public bool EnableBatching { get; set; } = true;

    /// <summary>
    /// The batch size for batched updates.
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// The batch timeout in milliseconds.
    /// </summary>
    public int BatchTimeoutMs { get; set; } = 100;
}
