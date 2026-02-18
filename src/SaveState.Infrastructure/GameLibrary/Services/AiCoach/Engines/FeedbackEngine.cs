using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Models.AiCoach;

namespace SaveState.Infrastructure.GameLibrary.Services.AiCoach.Engines;

/// <summary>
/// Implementation of feedback engine.
/// </summary>
public sealed class FeedbackEngine : IFeedbackEngine
{
    private readonly ILogger<FeedbackEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public FeedbackEngine(ILogger<FeedbackEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public bool IsRealTimeFeedbackEnabled(CoachingSession session)
    {
        return session.Preferences.EnableRealTimeFeedback;
    }

    public Task<Result<CoachingFeedback>> GenerateRealTimeFeedbackAsync(
        CoachingSession session,
        GameStateSnapshot gameState,
        SessionMetrics metrics,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Generating real-time feedback for session {SessionId}",
            session.Id);

        var feedback = new CoachingFeedback(
            Type: FeedbackType.Encouragement,
            Message: "Keep up the good work!",
            Priority: FeedbackPriority.Low,
            Suggestions: new List<string> { "Stay focused", "Watch your positioning" },
            ContextData: new Dictionary<string, object>());

        return Task.FromResult(Result.Success(feedback));
    }

    public Task<Result<CoachingFeedback>> ProvideActionFeedbackAsync(
        CoachingSession session,
        GameAction action,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Providing action feedback for session {SessionId}",
            session.Id);

        var feedbackType = action.Outcome switch
        {
            ActionOutcome.Success => FeedbackType.Positive,
            ActionOutcome.Failure => FeedbackType.Constructive,
            ActionOutcome.Partial => FeedbackType.Analysis,
            _ => FeedbackType.Encouragement
        };

        var feedback = new CoachingFeedback(
            Type: feedbackType,
            Message: $"Action '{action.ActionType}' resulted in {action.Outcome}",
            Priority: FeedbackPriority.Medium,
            Suggestions: new List<string>(),
            ContextData: new Dictionary<string, object>());

        return Task.FromResult(Result.Success(feedback));
    }
}
