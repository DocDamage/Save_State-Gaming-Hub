using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.SpectatorModeService.Engines;

/// <summary>
/// Engine for generating match highlights and managing replays.
/// </summary>
public class HighlightEngine : IHighlightEngine
{
    private readonly ILogger<HighlightEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, List<ReplayRequest>> _replayRequests = new();

    public HighlightEngine(ILogger<HighlightEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Generates highlights for a match.
    /// </summary>
    public Task<Result<MatchHighlights>> GenerateHighlightsAsync(string matchId, CancellationToken ct = default)
    {
        // In a real implementation, this would analyze match data to find highlights
        // For now, generate sample highlights
        var highlights = new List<SpectatorHighlightMoment>
        {
            new()
            {
                TimeStamp = TimeSpan.FromSeconds(30),
                Title = "First Blood",
                Description = "Player 1 takes the first round",
                HighlightType = HighlightType.Combo
            },
            new()
            {
                TimeStamp = TimeSpan.FromSeconds(65),
                Title = "Epic Combo",
                Description = "15-hit combo by Player 2",
                HighlightType = HighlightType.Combo
            },
            new()
            {
                TimeStamp = TimeSpan.FromSeconds(120),
                Title = "Comeback",
                Description = "Player 1 wins from low health",
                HighlightType = HighlightType.Comeback
            }
        };

        var matchHighlights = new MatchHighlights
        {
            MatchId = matchId,
            Highlights = highlights,
            GeneratedAt = _timeProvider.UtcNow
        };

        _logger.LogInformation("Generated highlights for match {MatchId}", matchId);
        return Task.FromResult(Result.Success(matchHighlights));
    }

    /// <summary>
    /// Creates a replay request for a specific time range.
    /// </summary>
    public Result<ReplayRequest> CreateReplayRequest(
        string sessionId,
        string matchId,
        TimeSpan startTime,
        TimeSpan endTime)
    {
        if (startTime < TimeSpan.Zero)
        {
            return Result.Failure<ReplayRequest>("Start time cannot be negative");
        }

        if (endTime <= startTime)
        {
            return Result.Failure<ReplayRequest>("End time must be after start time");
        }

        if (endTime - startTime > TimeSpan.FromMinutes(5))
        {
            return Result.Failure<ReplayRequest>("Replay cannot exceed 5 minutes");
        }

        var request = new ReplayRequest
        {
            RequestId = Guid.NewGuid().ToString("N"),
            MatchId = matchId,
            SessionId = sessionId,
            StartTime = startTime,
            EndTime = endTime,
            RequestedAt = _timeProvider.UtcNow,
            Status = ReplayStatus.Queued
        };

        if (!_replayRequests.TryGetValue(matchId, out var requests))
        {
            requests = new List<ReplayRequest>();
            _replayRequests[matchId] = requests;
        }
        requests.Add(request);

        _logger.LogInformation(
            "Created replay request {RequestId} for match {MatchId} from {StartTime} to {EndTime}",
            request.RequestId,
            matchId,
            startTime,
            endTime);

        return Result.Success(request);
    }
}
