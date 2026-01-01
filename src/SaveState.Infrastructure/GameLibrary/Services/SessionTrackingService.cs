using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Core.GameLibrary.Services;


namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Implementation of session tracking service with playtime statistics.
/// </summary>
public class SessionTrackingService : ISessionTrackingService
{
    private readonly IGameSessionRepository _sessionRepository;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<SessionTrackingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionTrackingService"/> class.
    /// </summary>
    /// <param name="sessionRepository">Repository for accessing game sessions.</param>
    /// <param name="gameRepository">Repository for accessing games.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public SessionTrackingService(
        IGameSessionRepository sessionRepository,
        IGameRepository gameRepository,
        ILogger<SessionTrackingService> logger)
    {
        _sessionRepository = sessionRepository;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    /// <summary>
    /// Starts a new gaming session for the specified game.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the created session or an error.</returns>
    public async Task<Result<GameSession>> StartSessionAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            // Verify game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result<GameSession>.Failure($"Game with ID {gameId} not found");
            }

            // Check for existing active session
            var existingSession = await _sessionRepository.GetActiveSessionAsync(gameId, ct)
                .ConfigureAwait(false);

            if (existingSession != null)
            {
                _logger.LogWarning("Game {GameId} already has an active session {SessionId}",
                    gameId, existingSession.Id);
                return Result<GameSession>.Success(existingSession);
            }

            // Create new session
            var session = GameSession.Create(gameId);
            await _sessionRepository.AddAsync(session, ct).ConfigureAwait(false);

            // Update game status
            game.MarkAsRunning();
            await _gameRepository.UpdateAsync(game, ct).ConfigureAwait(false);

            _logger.LogInformation("Started session {SessionId} for game {GameTitle}",
                session.Id, game.Title);

            return Result<GameSession>.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start session for game {GameId}", gameId);
            return Result<GameSession>.Failure($"Failed to start session: {ex.Message}");
        }
    }

    /// <summary>
    /// Ends an active gaming session.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the session to end.</param>
    /// <param name="reason">The reason the session is ending.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> EndSessionAsync(Guid sessionId, SessionEndReason reason, CancellationToken ct = default)
    {
        try
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId, ct).ConfigureAwait(false);

            if (session == null)
            {
                return Result.Failure($"Session {sessionId} not found");
            }

            if (!session.IsActive)
            {
                return Result.Success(); // Already ended
            }

            session.End(reason);
            await _sessionRepository.UpdateAsync(session, ct).ConfigureAwait(false);

            // Update game with session playtime
            var game = await _gameRepository.GetByIdAsync(GameId.From(session.GameId), ct)
                .ConfigureAwait(false);

            if (game != null)
            {
                game.MarkAsNotRunning();
                await _gameRepository.UpdateAsync(game, ct).ConfigureAwait(false);
            }

            _logger.LogInformation("Ended session {SessionId} after {Duration} - Reason: {Reason}",
                sessionId, session.Duration, reason);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end session {SessionId}", sessionId);
            return Result.Failure($"Failed to end session: {ex.Message}");
        }
    }

    /// <summary>
    /// Ends the active session for a specific game.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="reason">The reason the session is ending.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> EndActiveSessionAsync(Guid gameId, SessionEndReason reason, CancellationToken ct = default)
    {
        try
        {
            var session = await _sessionRepository.GetActiveSessionAsync(gameId, ct).ConfigureAwait(false);

            if (session == null)
            {
                return Result.Success(); // No active session
            }

            return await EndSessionAsync(session.Id, reason, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end active session for game {GameId}", gameId);
            return Result.Failure($"Failed to end active session: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the currently active session for a game, if any.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the active session or null.</returns>
    public async Task<Result<GameSession?>> GetActiveSessionAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var session = await _sessionRepository.GetActiveSessionAsync(gameId, ct).ConfigureAwait(false);
            return Result<GameSession?>.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active session for game {GameId}", gameId);
            return Result<GameSession?>.Failure($"Failed to get active session: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all currently active gaming sessions.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the list of active sessions.</returns>
    public async Task<Result<IReadOnlyList<GameSession>>> GetAllActiveSessionsAsync(CancellationToken ct = default)
    {
        try
        {
            var sessions = await _sessionRepository.GetAllActiveSessionsAsync(ct).ConfigureAwait(false);
            return Result<IReadOnlyList<GameSession>>.Success(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all active sessions");
            return Result<IReadOnlyList<GameSession>>.Failure($"Failed to get active sessions: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the session history for a specific game.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="limit">Maximum number of sessions to return.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the list of historical sessions.</returns>
    public async Task<Result<IReadOnlyList<GameSession>>> GetSessionHistoryAsync(
        Guid gameId,
        int limit = 50,
        CancellationToken ct = default)
    {
        try
        {
            var sessions = await _sessionRepository.GetByGameIdAsync(gameId, limit, ct).ConfigureAwait(false);
            return Result<IReadOnlyList<GameSession>>.Success(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get session history for game {GameId}", gameId);
            return Result<IReadOnlyList<GameSession>>.Failure($"Failed to get session history: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets playtime statistics for a specific game.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the playtime statistics.</returns>
    public async Task<Result<PlaytimeStatistics>> GetStatisticsAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var totalSessions = await _sessionRepository.CountByGameIdAsync(gameId, ct).ConfigureAwait(false);
            var totalPlaytime = await _sessionRepository.GetTotalPlaytimeAsync(gameId, ct).ConfigureAwait(false);
            var firstSession = await _sessionRepository.GetFirstSessionAsync(gameId, ct).ConfigureAwait(false);
            var longestSession = await _sessionRepository.GetLongestSessionAsync(gameId, ct).ConfigureAwait(false);

            var now = DateTime.UtcNow;
            var weekAgo = now.AddDays(-7);
            var monthAgo = now.AddDays(-30);

            var sessionsThisWeek = await _sessionRepository.CountByGameIdSinceAsync(gameId, weekAgo, ct)
                .ConfigureAwait(false);
            var sessionsThisMonth = await _sessionRepository.CountByGameIdSinceAsync(gameId, monthAgo, ct)
                .ConfigureAwait(false);

            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct)
                .ConfigureAwait(false);

            var averageDuration = totalSessions > 0
                ? TimeSpan.FromTicks(totalPlaytime.Ticks / totalSessions)
                : TimeSpan.Zero;

            var stats = new PlaytimeStatistics(
                GameId: gameId,
                TotalPlaytime: totalPlaytime,
                TotalSessions: totalSessions,
                FirstPlayedAt: firstSession?.StartedAt,
                LastPlayedAt: game?.LastPlayedAt,
                AverageSessionDuration: averageDuration,
                LongestSessionDuration: longestSession?.Duration ?? TimeSpan.Zero,
                SessionsThisWeek: sessionsThisWeek,
                SessionsThisMonth: sessionsThisMonth
            );

            return Result<PlaytimeStatistics>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statistics for game {GameId}", gameId);
            return Result<PlaytimeStatistics>.Failure($"Failed to get statistics: {ex.Message}");
        }
    }
}
