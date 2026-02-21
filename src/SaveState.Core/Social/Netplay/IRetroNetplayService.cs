using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.RomManagement.Entities;

namespace SaveState.Core.Social.Netplay;

/// <summary>
/// Service for managing retro game netplay sessions with matchmaking and rollback netcode.
/// </summary>
public interface IRetroNetplayService
{
    /// <summary>
    /// Gets the current matchmaking session status.
    /// </summary>
    Task<Result<MatchmakingSession?>> GetCurrentSessionAsync(CancellationToken ct = default);

    /// <summary>
    /// Joins the matchmaking queue for a specific ROM.
    /// </summary>
    Task<Result<MatchmakingTicket>> JoinQueueAsync(RomFile romFile, MatchmakingPreferences preferences, CancellationToken ct = default);

    /// <summary>
    /// Leaves the matchmaking queue.
    /// </summary>
    Task<Result> LeaveQueueAsync(string ticketId, CancellationToken ct = default);

    /// <summary>
    /// Accepts a found match.
    /// </summary>
    Task<Result<NetplaySession>> AcceptMatchAsync(string matchId, CancellationToken ct = default);

    /// <summary>
    /// Declines a found match.
    /// </summary>
    Task<Result> DeclineMatchAsync(string matchId, CancellationToken ct = default);

    /// <summary>
    /// Connects to a netplay session.
    /// </summary>
    Task<Result<NetplayConnection>> ConnectToSessionAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Disconnects from the current netplay session.
    /// </summary>
    Task<Result> DisconnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current connection quality metrics.
    /// </summary>
    Task<Result<ConnectionQuality>> GetConnectionQualityAsync(CancellationToken ct = default);

    /// <summary>
    /// Event raised when a match is found.
    /// </summary>
    event EventHandler<MatchFoundEventArgs>? MatchFound;

    /// <summary>
    /// Event raised when the matchmaking queue status changes.
    /// </summary>
    event EventHandler<QueueStatusChangedEventArgs>? QueueStatusChanged;

    /// <summary>
    /// Event raised when the connection quality changes.
    /// </summary>
    event EventHandler<ConnectionQualityChangedEventArgs>? ConnectionQualityChanged;
}

/// <summary>
/// Matchmaking preferences for finding opponents.
/// </summary>
public sealed record MatchmakingPreferences(
    string Region,
    int? SkillRating = null,
    int MaxSkillDifference = 300,
    int MaxWaitTimeSeconds = 300,
    bool AllowSpectators = true,
    string? PreferredOpponent = null);

/// <summary>
/// Matchmaking ticket representing a queue entry.
/// </summary>
public sealed record MatchmakingTicket(
    string Id,
    string RomHash,
    string Region,
    MatchmakingStatus Status,
    DateTime QueueTime,
    int EstimatedWaitSeconds);

/// <summary>
/// Matchmaking session information.
/// </summary>
public sealed record MatchmakingSession(
    string TicketId,
    string RomHash,
    MatchmakingStatus Status,
    DateTime QueueTime,
    int EstimatedWaitSeconds,
    int PlayersInQueue);

/// <summary>
/// Netplay session information.
/// </summary>
public sealed record NetplaySession(
    string Id,
    string RomHash,
    string HostAddress,
    int Port,
    PlayerInfo LocalPlayer,
    PlayerInfo RemotePlayer,
    bool IsHost,
    DateTime StartedAt,
    SpectatorInfo? SpectatorInfo = null);

/// <summary>
/// Player information for netplay.
/// </summary>
public sealed record PlayerInfo(
    string Id,
    string Username,
    string Region,
    int SkillRating,
    DateTime? JoinedAt = null);

/// <summary>
/// Netplay connection details.
/// </summary>
public sealed record NetplayConnection(
    string SessionId,
    ConnectionState State,
    RollbackConfig RollbackConfig,
    InputDelayConfig InputDelay,
    DateTime ConnectedAt);

/// <summary>
/// Rollback netcode configuration.
/// </summary>
public sealed record RollbackConfig(
    int MaxRollbackFrames,
    int InputDelayFrames,
    bool PredictiveInputs,
    bool DesyncDetection,
    int ResyncIntervalMs);

/// <summary>
/// Input delay configuration.
/// </summary>
public sealed record InputDelayConfig(
    int LocalDelay,
    int RemoteDelay,
    int TotalDelay,
    bool AutoAdjust);

/// <summary>
/// Connection quality metrics.
/// </summary>
public sealed record ConnectionQuality(
    int PingMs,
    int JitterMs,
    double PacketLossPercent,
    int RollbackFrames,
    ConnectionQualityRating Rating);

/// <summary>
/// Spectator information for a netplay session.
/// </summary>
public sealed record SpectatorInfo(
    bool Enabled,
    string RelayAddress,
    int MaxSpectators,
    int CurrentSpectators,
    string? StreamKey = null);

/// <summary>
/// Matchmaking status states.
/// </summary>
public enum MatchmakingStatus
{
    None,
    Queued,
    Searching,
    MatchFound,
    Accepted,
    Connecting,
    Connected,
    Failed,
    Cancelled
}

/// <summary>
/// Connection states for netplay.
/// </summary>
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Handshaking,
    Synchronizing,
    Connected,
    Paused,
    Desynced,
    Reconnecting,
    Failed
}

/// <summary>
/// Connection quality rating.
/// </summary>
public enum ConnectionQualityRating
{
    Excellent,
    Good,
    Fair,
    Poor,
    Unplayable
}

/// <summary>
/// Event args for match found events.
/// </summary>
public sealed class MatchFoundEventArgs : EventArgs
{
    public string MatchId { get; }
    public string RomHash { get; }
    public PlayerInfo Opponent { get; }
    public int AcceptTimeoutSeconds { get; }
    public DateTime FoundAt { get; }

    public MatchFoundEventArgs(string matchId, string romHash, PlayerInfo opponent, int acceptTimeoutSeconds, ITimeProvider? timeProvider = null)
    {
        MatchId = matchId;
        RomHash = romHash;
        Opponent = opponent;
        AcceptTimeoutSeconds = acceptTimeoutSeconds;
        FoundAt = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}

/// <summary>
/// Event args for queue status changed events.
/// </summary>
public sealed class QueueStatusChangedEventArgs : EventArgs
{
    public MatchmakingStatus OldStatus { get; }
    public MatchmakingStatus NewStatus { get; }
    public int? EstimatedWaitSeconds { get; }
    public int PlayersInQueue { get; }

    public QueueStatusChangedEventArgs(MatchmakingStatus oldStatus, MatchmakingStatus newStatus, int? estimatedWaitSeconds, int playersInQueue)
    {
        OldStatus = oldStatus;
        NewStatus = newStatus;
        EstimatedWaitSeconds = estimatedWaitSeconds;
        PlayersInQueue = playersInQueue;
    }
}

/// <summary>
/// Event args for connection quality changed events.
/// </summary>
public sealed class ConnectionQualityChangedEventArgs : EventArgs
{
    public ConnectionQuality OldQuality { get; }
    public ConnectionQuality NewQuality { get; }

    public ConnectionQualityChangedEventArgs(ConnectionQuality oldQuality, ConnectionQuality newQuality)
    {
        OldQuality = oldQuality;
        NewQuality = newQuality;
    }
}
