using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Models.AiCoach;

namespace SaveState.Infrastructure.GameLibrary.Services.AiCoach.Engines;

/// <summary>
/// Engine responsible for generating real-time feedback.
/// </summary>
public interface IFeedbackEngine
{
    /// <summary>
    /// Checks if real-time feedback is enabled for the session.
    /// </summary>
    bool IsRealTimeFeedbackEnabled(CoachingSession session);

    /// <summary>
    /// Generates real-time feedback based on current game state.
    /// </summary>
    Task<Result<CoachingFeedback>> GenerateRealTimeFeedbackAsync(CoachingSession session, GameStateSnapshot gameState, SessionMetrics metrics, CancellationToken ct = default);

    /// <summary>
    /// Provides feedback on a specific action.
    /// </summary>
    Task<Result<CoachingFeedback>> ProvideActionFeedbackAsync(CoachingSession session, GameAction action, CancellationToken ct = default);
}
