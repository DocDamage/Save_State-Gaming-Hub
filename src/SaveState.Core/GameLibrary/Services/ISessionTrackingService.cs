using SaveState.Core.Common;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;

namespace SaveState.Core.GameLibrary.Services;

/// <summary>
/// Service for tracking and managing game play sessions.
/// </summary>
public interface ISessionTrackingService
{
    /// <summary>
    /// Starts a new session for the specified game.
    /// </summary>
    Task<Result<GameSession>> StartSessionAsync(Guid gameId, CancellationToken ct = default);

    /// <summary>
    /// Ends an active session with the specified reason.
    /// </summary>
    Task<Result> EndSessionAsync(Guid sessionId, SessionEndReason reason, CancellationToken ct = default);

    /// <summary>
    /// Ends any active session for the specified game.
    /// </summary>
    Task<Result> EndActiveSessionAsync(Guid gameId, SessionEndReason reason, CancellationToken ct = default);

    /// <summary>
    /// Gets the currently active session for a game, if any.
    /// </summary>
    Task<Result<GameSession?>> GetActiveSessionAsync(Guid gameId, CancellationToken ct = default);

    /// <summary>
    /// Gets all active sessions across all games.
    /// </summary>
    Task<Result<IReadOnlyList<GameSession>>> GetAllActiveSessionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the session history for a game.
    /// </summary>
    Task<Result<IReadOnlyList<GameSession>>> GetSessionHistoryAsync(
        Guid gameId,
        int limit = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Gets playtime statistics for a game.
    /// </summary>
    Task<Result<PlaytimeStatistics>> GetStatisticsAsync(Guid gameId, CancellationToken ct = default);

    /// <summary>
    /// Event raised when a game session starts.
    /// </summary>
    event EventHandler<GameSessionEventArgs>? SessionStarted;

    /// <summary>
    /// Event raised when a game session ends.
    /// </summary>
    event EventHandler<GameSessionEventArgs>? SessionEnded;
}

/// <summary>
/// Event arguments for game session events.
/// </summary>
public sealed class GameSessionEventArgs : EventArgs
{
    public Guid GameId { get; init; }
    public Guid SessionId { get; init; }
    public SessionEndReason? EndReason { get; init; }
}
