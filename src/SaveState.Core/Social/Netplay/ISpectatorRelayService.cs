using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Core.Social.Netplay;

/// <summary>
/// Service for managing spectator relay servers for streaming netplay matches.
/// </summary>
public interface ISpectatorRelayService
{
    /// <summary>
    /// Creates a spectator relay for a netplay session.
    /// </summary>
    Task<Result<SpectatorRelay>> CreateRelayAsync(string sessionId, SpectatorRelayConfiguration config, CancellationToken ct = default);

    /// <summary>
    /// Destroys a spectator relay.
    /// </summary>
    Task<Result> DestroyRelayAsync(string relayId, CancellationToken ct = default);

    /// <summary>
    /// Adds a spectator to a relay.
    /// </summary>
    Task<Result<SpectatorConnection>> AddSpectatorAsync(string relayId, string spectatorId, string username, CancellationToken ct = default);

    /// <summary>
    /// Removes a spectator from a relay.
    /// </summary>
    Task<Result> RemoveSpectatorAsync(string relayId, string spectatorId, CancellationToken ct = default);

    /// <summary>
    /// Broadcasts game state to all spectators.
    /// </summary>
    Task<Result> BroadcastStateAsync(string relayId, GameStreamFrame frame, CancellationToken ct = default);

    /// <summary>
    /// Gets the current relay status.
    /// </summary>
    Task<Result<SpectatorRelayStatus>> GetRelayStatusAsync(string relayId, CancellationToken ct = default);

    /// <summary>
    /// Lists all active spectators for a relay.
    /// </summary>
    Task<Result<IReadOnlyList<SpectatorInfo>>> GetSpectatorsAsync(string relayId, CancellationToken ct = default);

    /// <summary>
    /// Sets stream delay for spectators (to prevent cheating).
    /// </summary>
    Task<Result> SetStreamDelayAsync(string relayId, int delayFrames, CancellationToken ct = default);

    /// <summary>
    /// Event raised when a spectator joins.
    /// </summary>
    event EventHandler<SpectatorJoinedEventArgs>? SpectatorJoined;

    /// <summary>
    /// Event raised when a spectator leaves.
    /// </summary>
    event EventHandler<SpectatorLeftEventArgs>? SpectatorLeft;
}

/// <summary>
/// Configuration for a spectator relay.
/// </summary>
public sealed record SpectatorRelayConfiguration(
    int MaxSpectators,
    int DelayFrames,
    int TargetBitrate,
    int KeyFrameInterval,
    bool RecordStream,
    string? RecordingPath = null);

/// <summary>
/// Spectator relay information.
/// </summary>
public sealed record SpectatorRelay(
    string Id,
    string SessionId,
    string StreamUrl,
    string StreamKey,
    SpectatorRelayConfiguration Configuration,
    SpectatorRelayState State,
    DateTime CreatedAt);

/// <summary>
/// Spectator connection details.
/// </summary>
public sealed record SpectatorConnection(
    string SpectatorId,
    string Username,
    string StreamUrl,
    DateTime ConnectedAt);

/// <summary>
/// Game stream frame for spectators.
/// </summary>
public sealed record GameStreamFrame(
    int FrameNumber,
    byte[] FrameData,
    uint Checksum,
    InputFrame[] Inputs,
    bool IsKeyFrame,
    DateTime Timestamp);

/// <summary>
/// Spectator relay status.
/// </summary>
public sealed record SpectatorRelayStatus(
    string RelayId,
    int CurrentSpectators,
    int MaxSpectators,
    double AverageBitrate,
    int CurrentDelayFrames,
    SpectatorRelayState State,
    DateTime StatusAt);

/// <summary>
/// Spectator relay states.
/// </summary>
public enum SpectatorRelayState
{
    Initializing,
    Active,
    Paused,
    ShuttingDown,
    Error
}

/// <summary>
/// Event args for spectator joined events.
/// </summary>
public sealed class SpectatorJoinedEventArgs : EventArgs
{
    public string RelayId { get; }
    public string SpectatorId { get; }
    public string Username { get; }
    public DateTime JoinedAt { get; }

    public SpectatorJoinedEventArgs(string relayId, string spectatorId, string username, ITimeProvider? timeProvider = null)
    {
        RelayId = relayId;
        SpectatorId = spectatorId;
        Username = username;
        JoinedAt = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}

/// <summary>
/// Event args for spectator left events.
/// </summary>
public sealed class SpectatorLeftEventArgs : EventArgs
{
    public string RelayId { get; }
    public string SpectatorId { get; }
    public TimeSpan Duration { get; }
    public DateTime LeftAt { get; }

    public SpectatorLeftEventArgs(string relayId, string spectatorId, TimeSpan duration, ITimeProvider? timeProvider = null)
    {
        RelayId = relayId;
        SpectatorId = spectatorId;
        Duration = duration;
        LeftAt = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}
