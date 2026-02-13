using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Social.Netplay;

namespace SaveState.Infrastructure.Social.Netplay;

/// <summary>
/// Matchmaking engine with ROM hash verification and skill-based matching.
/// </summary>
public sealed class MatchmakingEngine : IMatchmakingEngine
{
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<MatchmakingEngine> _logger;
    private readonly Dictionary<string, MatchmakingRequest> _queue = new();
    private readonly Dictionary<string, MatchCandidate> _activeMatches = new();
    private readonly Random _random = new();

    public MatchmakingEngine(ITimeProvider timeProvider, ILogger<MatchmakingEngine> logger)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result<MatchmakingTicket>> EnqueueAsync(MatchmakingRequest request, CancellationToken ct = default)
    {
        try
        {
            Guard.Against.Null(request, nameof(request));

            var ticketId = Guid.NewGuid().ToString();
            var estimatedWait = CalculateEstimatedWait(request.Region, request.RomHash);

            var ticket = new MatchmakingTicket(
                Id: ticketId,
                RomHash: request.RomHash,
                Region: request.Region,
                Status: MatchmakingStatus.Queued,
                QueueTime: _timeProvider.UtcNow,
                EstimatedWaitSeconds: estimatedWait);

            lock (_queue)
            {
                _queue[ticketId] = request;
            }

            _logger.LogInformation("Player enqueued: {TicketId}, Region: {Region}, Rom: {RomHash}",
                ticketId, request.Region, request.RomHash);

            return Task.FromResult(Result<MatchmakingTicket>.Success(ticket));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue player");
            return Task.FromResult(Result<MatchmakingTicket>.Failure($"Failed to enqueue: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> DequeueAsync(string ticketId, CancellationToken ct = default)
    {
        try
        {
            Guard.Against.NullOrEmpty(ticketId, nameof(ticketId));

            lock (_queue)
            {
                if (!_queue.Remove(ticketId))
                {
                    return Task.FromResult(Result.Failure("Ticket not found", ErrorType.NotFound));
                }
            }

            _logger.LogInformation("Player dequeued: {TicketId}", ticketId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dequeue player");
            return Task.FromResult(Result.Failure($"Failed to dequeue: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<MatchCandidate?>> FindMatchAsync(string ticketId, CancellationToken ct = default)
    {
        try
        {
            Guard.Against.NullOrEmpty(ticketId, nameof(ticketId));

            MatchmakingRequest? request;
            lock (_queue)
            {
                if (!_queue.TryGetValue(ticketId, out request))
                {
                    return Task.FromResult(Result<MatchCandidate?>.Failure("Ticket not found", ErrorType.NotFound));
                }
            }

            var candidates = FindCompatiblePlayers(request);

            if (candidates.Count == 0)
            {
                return Task.FromResult(Result<MatchCandidate?>.Success(null));
            }

            var bestMatch = candidates.OrderByDescending(c => c.Compatibility).First();

            var matchId = Guid.NewGuid().ToString();
            var match = new MatchCandidate(
                MatchId: matchId,
                Player1Id: request.PlayerId,
                Player2Id: bestMatch.Request.PlayerId,
                RomHash: request.RomHash,
                SkillDifference: Math.Abs(request.SkillRating - bestMatch.Request.SkillRating),
                EstimatedQuality: bestMatch.Compatibility,
                FoundAt: _timeProvider.UtcNow,
                ExpiresAt: _timeProvider.UtcNow.AddSeconds(30));

            lock (_activeMatches)
            {
                _activeMatches[matchId] = match;
            }

            _logger.LogInformation("Match found: {MatchId}, Players: {Player1} vs {Player2}, Quality: {Quality}",
                matchId, request.PlayerId, bestMatch.Request.PlayerId, bestMatch.Compatibility);

            return Task.FromResult(Result<MatchCandidate?>.Success(match));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find match");
            return Task.FromResult(Result<MatchCandidate?>.Failure($"Failed to find match: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<MatchConfirmation>> AcceptMatchAsync(string ticketId, string matchId, CancellationToken ct = default)
    {
        try
        {
            Guard.Against.NullOrEmpty(ticketId, nameof(ticketId));
            Guard.Against.NullOrEmpty(matchId, nameof(matchId));

            MatchCandidate? match;
            lock (_activeMatches)
            {
                if (!_activeMatches.TryGetValue(matchId, out match))
                {
                    return Task.FromResult(Result<MatchConfirmation>.Failure("Match not found", ErrorType.NotFound));
                }
                _activeMatches.Remove(matchId);
            }

            var sessionId = Guid.NewGuid().ToString();
            var isHost = match.Player1Id == ticketId;

            var confirmation = new MatchConfirmation(
                MatchId: matchId,
                SessionId: sessionId,
                HostAddress: isHost ? "127.0.0.1" : "remote.host",
                Port: 55400 + _random.Next(100),
                IsHost: isHost,
                ConfirmedAt: _timeProvider.UtcNow);

            _logger.LogInformation("Match confirmed: {MatchId}, Session: {SessionId}, Host: {IsHost}",
                matchId, sessionId, isHost);

            return Task.FromResult(Result<MatchConfirmation>.Success(confirmation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to accept match");
            return Task.FromResult(Result<MatchConfirmation>.Failure($"Failed to accept match: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> DeclineMatchAsync(string ticketId, string matchId, CancellationToken ct = default)
    {
        try
        {
            Guard.Against.NullOrEmpty(ticketId, nameof(ticketId));
            Guard.Against.NullOrEmpty(matchId, nameof(matchId));

            lock (_activeMatches)
            {
                _activeMatches.Remove(matchId);
            }

            _logger.LogInformation("Match declined: {MatchId}", matchId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decline match");
            return Task.FromResult(Result.Failure($"Failed to decline match: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<QueueStatistics>> GetQueueStatisticsAsync(string region, CancellationToken ct = default)
    {
        try
        {
            Guard.Against.NullOrEmpty(region, nameof(region));

            lock (_queue)
            {
                var playersInRegion = _queue.Values.Count(r => r.Region == region);

                var stats = new QueueStatistics(
                    Region: region,
                    PlayersInQueue: playersInRegion,
                    ActiveMatches: _activeMatches.Count,
                    AverageWaitTimeSeconds: 45.0,
                    PeakHourPlayers: playersInRegion * 2,
                    CalculatedAt: _timeProvider.UtcNow);

                return Task.FromResult(Result<QueueStatistics>.Success(stats));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get queue statistics");
            return Task.FromResult(Result<QueueStatistics>.Failure($"Failed to get statistics: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<RomCompatibility>> ValidateRomCompatibilityAsync(string romHash1, string romHash2, CancellationToken ct = default)
    {
        try
        {
            Guard.Against.NullOrEmpty(romHash1, nameof(romHash1));
            Guard.Against.NullOrEmpty(romHash2, nameof(romHash2));

            var isCompatible = romHash1.Equals(romHash2, StringComparison.OrdinalIgnoreCase);
            var level = isCompatible ? RomCompatibilityLevel.Identical : RomCompatibilityLevel.Incompatible;

            var compatibility = new RomCompatibility(
                RomHash1: romHash1,
                RomHash2: romHash2,
                IsCompatible: isCompatible,
                CompatibilityLevel: level,
                WarningMessage: isCompatible ? null : "ROMs are not identical");

            return Task.FromResult(Result<RomCompatibility>.Success(compatibility));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate ROM compatibility");
            return Task.FromResult(Result<RomCompatibility>.Failure($"Failed to validate: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<SkillMatchResult>> CalculateSkillMatchAsync(int player1Rating, int player2Rating, int maxDifference)
    {
        try
        {
            var difference = Math.Abs(player1Rating - player2Rating);
            var isAcceptable = difference <= maxDifference;

            var maxRating = Math.Max(player1Rating, player2Rating);
            var qualityScore = maxRating > 0 ? 1.0 - (double)difference / maxRating : 1.0;

            var result = new SkillMatchResult(
                Player1Rating: player1Rating,
                Player2Rating: player2Rating,
                Difference: difference,
                IsAcceptable: isAcceptable,
                QualityScore: Math.Max(0, qualityScore));

            return Task.FromResult(Result<SkillMatchResult>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate skill match");
            return Task.FromResult(Result<SkillMatchResult>.Failure($"Failed to calculate: {ex.Message}", ErrorType.Internal));
        }
    }

    private int CalculateEstimatedWait(string region, string romHash)
    {
        lock (_queue)
        {
            var playersInRegion = _queue.Values.Count(r => r.Region == region);
            var playersWithSameRom = _queue.Values.Count(r => r.RomHash == romHash);
            return Math.Max(30, (playersInRegion + 1) * 15 - playersWithSameRom * 5);
        }
    }

    private List<(MatchmakingRequest Request, int Compatibility)> FindCompatiblePlayers(MatchmakingRequest request)
    {
        var candidates = new List<(MatchmakingRequest Request, int Compatibility)>();

        lock (_queue)
        {
            foreach (var other in _queue.Values)
            {
                if (other.PlayerId == request.PlayerId)
                    continue;

                if (other.RomHash != request.RomHash)
                    continue;

                var compatibility = CalculateCompatibility(request, other);
                if (compatibility > 50)
                {
                    candidates.Add((other, compatibility));
                }
            }
        }

        return candidates;
    }

    private int CalculateCompatibility(MatchmakingRequest player1, MatchmakingRequest player2)
    {
        var score = 100;

        if (player1.Region != player2.Region)
            score -= 20;

        var skillDiff = Math.Abs(player1.SkillRating - player2.SkillRating);
        score -= skillDiff / 50;

        var waitTime = (_timeProvider.UtcNow - player1.RequestedAt).TotalSeconds;
        score += (int)(waitTime / 10);

        return Math.Max(0, Math.Min(100, score));
    }
}
