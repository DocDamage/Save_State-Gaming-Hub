using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Models.AiCoach;

namespace SaveState.Infrastructure.GameLibrary.Services.AiCoach.Engines;

/// <summary>
/// Implementation of coaching engine.
/// </summary>
public sealed class CoachingEngine : ICoachingEngine
{
    private readonly ConcurrentDictionary<Guid, CoachingSession> _sessions = new();
    private readonly ILogger<CoachingEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public CoachingEngine(ILogger<CoachingEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public Task<Result<CoachingSession>> CreateSessionAsync(Guid gameId, CoachingPreferences preferences, CancellationToken ct = default)
    {
        var session = new CoachingSession(
            Id: Guid.NewGuid(),
            GameId: gameId,
            Preferences: preferences,
            StartedAt: _timeProvider.UtcNow,
            CurrentPhase: CoachingPhase.Assessment);

        _sessions[session.Id] = session;

        _logger.LogInformation(
            "Created coaching session {SessionId} for game {GameId}",
            session.Id, gameId);

        return Task.FromResult(Result.Success(session));
    }

    public Task<Result> EndSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (_sessions.TryRemove(sessionId, out _))
        {
            _logger.LogInformation("Ended coaching session {SessionId}", sessionId);
            return Task.FromResult(Result.Success());
        }

        return Task.FromResult(Result.Failure("Session not found", ErrorType.NotFound));
    }

    public Result<CoachingSession> GetSession(Guid sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return Result.Success(session);
        }

        return Result.Failure<CoachingSession>("Session not found", ErrorType.NotFound);
    }

    public Task<Result<CoachingReport>> CompileSessionReportAsync(CoachingSession session, CancellationToken ct = default)
    {
        var report = new CoachingReport(
            SessionId: session.Id,
            SessionStart: session.StartedAt,
            SessionEnd: _timeProvider.UtcNow,
            Duration: _timeProvider.UtcNow - session.StartedAt,
            FeedbackGiven: Array.Empty<CoachingFeedback>(),
            StrategyAnalyses: Array.Empty<StrategyAnalysis>(),
            SkillAssessments: Array.Empty<SkillAssessment>(),
            GoalsAchieved: Array.Empty<ImprovementGoal>(),
            OverallAssessment: "Session completed successfully",
            Recommendations: Array.Empty<string>());

        return Task.FromResult(Result.Success(report));
    }
}
