namespace SaveState.Application.Mugen.Services.NetworkFeatures.Engines;

using System.Collections.Concurrent;
using SaveState.Application.Mugen.Models.NetworkFeatures;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// Criteria for matchmaking search.
/// </summary>
public record MatchmakingCriteria(
    string CharacterName,
    MatchmakingMode Mode,
    string Region,
    int? MinRating = null,
    int? MaxRating = null,
    IReadOnlyList<string>? PreferredCharacters = null,
    IReadOnlyList<string>? AvoidedCharacters = null,
    bool AllowCrossplay = true);

/// <summary>
/// Engine for managing matchmaking operations.
/// </summary>
public class MatchmakingEngine
{
    private readonly ILogger<MatchmakingEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, QueuedPlayer> _queue = new();
    private readonly ConcurrentDictionary<string, MatchmakingSession> _activeSessions = new();
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(5);
    private const int RatingToleranceDefault = 200;
    private const int MaxRatingTolerance = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="MatchmakingEngine"/> class.
    /// </summary>
    public MatchmakingEngine(ILogger<MatchmakingEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Finds an opponent for a player based on matchmaking criteria.
    /// </summary>
    /// <param name="playerId">The ID of the player seeking a match.</param>
    /// <param name="criteria">The matchmaking criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the matchmaking result.</returns>
    public async Task<Result<MatchmakingResult>> FindOpponentAsync(
        string playerId,
        MatchmakingCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            return Result.Failure<MatchmakingResult>("Player ID is required.", ErrorType.Validation);
        }

        if (criteria is null)
        {
            return Result.Failure<MatchmakingResult>("Matchmaking criteria is required.", ErrorType.Validation);
        }

        if (string.IsNullOrWhiteSpace(criteria.CharacterName))
        {
            return Result.Failure<MatchmakingResult>("Character name is required.", ErrorType.Validation);
        }

        if (string.IsNullOrWhiteSpace(criteria.Region))
        {
            return Result.Failure<MatchmakingResult>("Region is required.", ErrorType.Validation);
        }

        // Check if player is already in queue
        if (_queue.ContainsKey(playerId))
        {
            return Result.Failure<MatchmakingResult>(
                "Player is already in the matchmaking queue.",
                ErrorType.Conflict);
        }

        // Check if player is already in an active session
        if (_activeSessions.ContainsKey(playerId))
        {
            return Result.Failure<MatchmakingResult>(
                "Player is already in an active matchmaking session.",
                ErrorType.Conflict);
        }

        // Get player stats (in a real implementation, this would come from a player service)
        var playerStats = await GetPlayerStatsAsync(playerId, cancellationToken);
        if (playerStats is null)
        {
            return Result.Failure<MatchmakingResult>(
                "Unable to retrieve player statistics.",
                ErrorType.NotFound);
        }

        // Create queue entry
        var queuedPlayer = new QueuedPlayer
        {
            PlayerId = playerId,
            PlayerName = playerStats.PlayerId, // Would be player name in real implementation
            CharacterName = criteria.CharacterName,
            Mode = criteria.Mode,
            Preferences = new MatchmakingPreferences(
                criteria.MinRating,
                criteria.MaxRating,
                criteria.PreferredCharacters ?? new List<string>(),
                criteria.AvoidedCharacters ?? new List<string>(),
                criteria.AllowCrossplay,
                criteria.Region),
            PlayerStats = playerStats,
            QueuedAt = _timeProvider.UtcNow
        };

        // Add to queue
        if (!_queue.TryAdd(playerId, queuedPlayer))
        {
            return Result.Failure<MatchmakingResult>(
                "Failed to join matchmaking queue. Please try again.",
                ErrorType.Internal);
        }

        _logger.LogInformation(
            "Player {PlayerId} joined matchmaking queue for mode {Mode} with character {Character}",
            playerId,
            criteria.Mode,
            criteria.CharacterName);

        try
        {
            // Search for opponent
            var searchStartTime = _timeProvider.UtcNow;
            var timeout = _defaultTimeout;
            var currentRatingTolerance = RatingToleranceDefault;

            while (!cancellationToken.IsCancellationRequested && 
                   _timeProvider.UtcNow - searchStartTime < timeout)
            {
                // Try to find a match
                var opponent = FindBestOpponent(queuedPlayer, currentRatingTolerance);
                
                if (opponent is not null)
                {
                    // Found a match!
                    return await CreateMatchAsync(playerId, opponent.PlayerId, searchStartTime, cancellationToken);
                }

                // Expand search criteria over time
                var elapsed = _timeProvider.UtcNow - searchStartTime;
                if (elapsed > TimeSpan.FromSeconds(30) && currentRatingTolerance < MaxRatingTolerance)
                {
                    currentRatingTolerance += 50;
                    _logger.LogDebug(
                        "Expanding rating tolerance to {Tolerance} for player {PlayerId}",
                        currentRatingTolerance,
                        playerId);
                }

                // Wait before next search iteration
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            }

            // Timeout or cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                _queue.TryRemove(playerId, out _);
                return Result.Failure<MatchmakingResult>(
                    "Matchmaking was cancelled.",
                    ErrorType.Cancelled);
            }

            // Remove from queue due to timeout
            _queue.TryRemove(playerId, out _);
            
            _logger.LogInformation(
                "Matchmaking timed out for player {PlayerId} after {Timeout}",
                playerId,
                timeout);

            return Result.Success(new MatchmakingResult(
                MatchFound: false,
                MatchId: null,
                OpponentId: null,
                OpponentName: null,
                WaitTime: _timeProvider.UtcNow - searchStartTime,
                ErrorMessage: "Unable to find an opponent. Please try again later."));
        }
        catch (Exception ex)
        {
            _queue.TryRemove(playerId, out _);
            _logger.LogError(ex, "Error during matchmaking for player {PlayerId}", playerId);
            return Result.Failure<MatchmakingResult>(
                "An error occurred during matchmaking. Please try again.",
                ErrorType.Internal);
        }
    }

    /// <summary>
    /// Removes a player from the matchmaking queue.
    /// </summary>
    /// <param name="playerId">The player ID.</param>
    /// <returns>True if the player was removed; otherwise false.</returns>
    public bool LeaveQueue(string playerId)
    {
        if (_queue.TryRemove(playerId, out _))
        {
            _logger.LogInformation("Player {PlayerId} left the matchmaking queue", playerId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the current queue size.
    /// </summary>
    /// <returns>The number of players in the queue.</returns>
    public int GetQueueSize()
    {
        return _queue.Count;
    }

    /// <summary>
    /// Gets all players in the queue for a specific mode.
    /// </summary>
    /// <param name="mode">The matchmaking mode.</param>
    /// <returns>A read-only list of queued players.</returns>
    public IReadOnlyList<QueuedPlayer> GetQueuedPlayers(MatchmakingMode mode)
    {
        return _queue.Values
            .Where(p => p.Mode == mode)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets a player's active matchmaking session.
    /// </summary>
    /// <param name="playerId">The player ID.</param>
    /// <returns>The session if found; otherwise null.</returns>
    public MatchmakingSession? GetSession(string playerId)
    {
        _activeSessions.TryGetValue(playerId, out var session);
        return session;
    }

    /// <summary>
    /// Ends a matchmaking session.
    /// </summary>
    /// <param name="playerId">The player ID.</param>
    /// <returns>True if the session was ended; otherwise false.</returns>
    public bool EndSession(string playerId)
    {
        if (_activeSessions.TryRemove(playerId, out var session))
        {
            // Also remove opponent's session reference
            if (session.OpponentId is not null)
            {
                _activeSessions.TryRemove(session.OpponentId, out _);
            }

            _logger.LogInformation(
                "Ended matchmaking session {SessionId} for player {PlayerId}",
                session.SessionId,
                playerId);

            return true;
        }

        return false;
    }

    private QueuedPlayer? FindBestOpponent(QueuedPlayer player, int ratingTolerance)
    {
        var potentialOpponents = _queue.Values
            .Where(p => 
                p.PlayerId != player.PlayerId &&
                p.Mode == player.Mode &&
                IsRegionCompatible(p, player) &&
                IsRatingCompatible(p, player, ratingTolerance) &&
                !HasAvoidedCharacters(p, player) &&
                !HasAvoidedCharacters(player, p))
            .ToList();

        if (potentialOpponents.Count == 0)
        {
            return null;
        }

        // Score each potential opponent
        var scoredOpponents = potentialOpponents.Select(opponent =>
        {
            var score = CalculateMatchQuality(player, opponent);
            return (Opponent: opponent, Score: score);
        });

        // Return the best match
        return scoredOpponents
            .OrderByDescending(x => x.Score)
            .FirstOrDefault()
            .Opponent;
    }

    private static bool IsRegionCompatible(QueuedPlayer player1, QueuedPlayer player2)
    {
        // Exact region match
        if (player1.Preferences.Region.Equals(player2.Preferences.Region, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Crossplay allowed by both
        if (player1.Preferences.AllowCrossplay && player2.Preferences.AllowCrossplay)
        {
            return true;
        }

        return false;
    }

    private static bool IsRatingCompatible(QueuedPlayer player1, QueuedPlayer player2, int tolerance)
    {
        var rating1 = player1.PlayerStats.Rating;
        var rating2 = player2.PlayerStats.Rating;

        // Check if ratings are within tolerance
        if (Math.Abs(rating1 - rating2) > tolerance)
        {
            return false;
        }

        // Check custom rating bounds if specified
        if (player1.Preferences.MinRating.HasValue && rating2 < player1.Preferences.MinRating.Value)
        {
            return false;
        }

        if (player1.Preferences.MaxRating.HasValue && rating2 > player1.Preferences.MaxRating.Value)
        {
            return false;
        }

        if (player2.Preferences.MinRating.HasValue && rating1 < player2.Preferences.MinRating.Value)
        {
            return false;
        }

        if (player2.Preferences.MaxRating.HasValue && rating1 > player2.Preferences.MaxRating.Value)
        {
            return false;
        }

        return true;
    }

    private static bool HasAvoidedCharacters(QueuedPlayer player, QueuedPlayer opponent)
    {
        return player.Preferences.AvoidedCharacters.Contains(
            opponent.CharacterName,
            StringComparer.OrdinalIgnoreCase);
    }

    private static float CalculateMatchQuality(QueuedPlayer player1, QueuedPlayer player2)
    {
        var ratingDiff = Math.Abs(player1.PlayerStats.Rating - player2.PlayerStats.Rating);
        var winRateDiff = Math.Abs(player1.PlayerStats.WinRate - player2.PlayerStats.WinRate);
        var queueTimeDiff = Math.Abs((player1.QueuedAt - player2.QueuedAt).TotalSeconds);

        // Calculate individual scores (lower is better for diffs)
        var ratingScore = 1.0f - Math.Min(ratingDiff / 1000.0f, 1.0f);
        var winRateScore = 1.0f - Math.Min((float)winRateDiff, 1.0f);
        var queueTimeScore = Math.Min((float)queueTimeDiff / 60.0f, 1.0f); // Bonus for waiting longer

        // Preferred character bonus
        var preferredBonus = player1.Preferences.PreferredCharacters.Contains(
            player2.CharacterName,
            StringComparer.OrdinalIgnoreCase) ? 0.2f : 0.0f;

        // Weighted total score
        return (ratingScore * 0.4f) + (winRateScore * 0.3f) + (queueTimeScore * 0.2f) + preferredBonus;
    }

    private async Task<Result<MatchmakingResult>> CreateMatchAsync(
        string player1Id,
        string player2Id,
        DateTime searchStartTime,
        CancellationToken cancellationToken)
    {
        var matchId = Guid.NewGuid().ToString("N");
        var waitTime = _timeProvider.UtcNow - searchStartTime;

        // Remove both players from queue
        _queue.TryRemove(player1Id, out var player1);
        _queue.TryRemove(player2Id, out var player2);

        if (player1 is null || player2 is null)
        {
            return Result.Failure<MatchmakingResult>(
                "Failed to create match. Opponent may have left the queue.",
                ErrorType.Conflict);
        }

        // Create sessions for both players
        var session1 = new MatchmakingSession
        {
            SessionId = Guid.NewGuid().ToString("N"),
            PlayerId = player1Id,
            CharacterName = player1.CharacterName,
            Mode = player1.Mode,
            Preferences = player1.Preferences,
            PlayerStats = player1.PlayerStats,
            StartTime = _timeProvider.UtcNow,
            MatchFound = true,
            MatchId = matchId,
            OpponentId = player2Id,
            OpponentName = player2.PlayerName
        };

        var session2 = new MatchmakingSession
        {
            SessionId = Guid.NewGuid().ToString("N"),
            PlayerId = player2Id,
            CharacterName = player2.CharacterName,
            Mode = player2.Mode,
            Preferences = player2.Preferences,
            PlayerStats = player2.PlayerStats,
            StartTime = _timeProvider.UtcNow,
            MatchFound = true,
            MatchId = matchId,
            OpponentId = player1Id,
            OpponentName = player1.PlayerName
        };

        _activeSessions.TryAdd(player1Id, session1);
        _activeSessions.TryAdd(player2Id, session2);

        _logger.LogInformation(
            "Created match {MatchId} between players {Player1Id} and {Player2Id} after {WaitTime}",
            matchId,
            player1Id,
            player2Id,
            waitTime);

        // Simulate some async work (e.g., initializing match server)
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);

        return Result.Success(new MatchmakingResult(
            MatchFound: true,
            MatchId: matchId,
            OpponentId: player2Id,
            OpponentName: player2.PlayerName,
            WaitTime: waitTime,
            ErrorMessage: null));
    }

    private Task<PlayerMatchmakingStats?> GetPlayerStatsAsync(string playerId, CancellationToken cancellationToken)
    {
        // In a real implementation, this would fetch from a player service or database
        // For now, return mock data
        var stats = new PlayerMatchmakingStats
        {
            PlayerId = playerId,
            Rating = 1500, // Default rating
            WinRate = 0.5m,
            TotalMatches = 0,
            PreferredCharacters = new List<string>(),
            RecentPerformance = 0.5m
        };

        return Task.FromResult<PlayerMatchmakingStats?>(stats);
    }
}
