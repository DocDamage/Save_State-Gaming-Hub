using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Models.AiCoach;

namespace SaveState.Core.GameLibrary.Services;

/// <summary>
/// Service interface for AI-powered coaching functionality.
/// Provides real-time feedback, strategy analysis, and personalized coaching.
/// </summary>
public interface IAiCoachService
{
    /// <summary>
    /// Starts a new AI coaching session for a game.
    /// </summary>
    Task<Result<CoachingSession>> StartCoachingSessionAsync(
        Guid gameId,
        CoachingPreferences preferences,
        CancellationToken ct = default);

    /// <summary>
    /// Ends an AI coaching session.
    /// </summary>
    Task<Result> EndCoachingSessionAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Gets real-time coaching feedback based on current game state.
    /// </summary>
    Task<Result<CoachingFeedback>> GetRealTimeFeedbackAsync(
        Guid sessionId,
        GameStateSnapshot gameState,
        CancellationToken ct = default);

    /// <summary>
    /// Analyzes the player's strategy based on recent game actions.
    /// </summary>
    Task<Result<StrategyAnalysis>> AnalyzePlayerStrategyAsync(
        Guid sessionId,
        IReadOnlyList<GameAction> recentActions,
        CancellationToken ct = default);

    /// <summary>
    /// Analyzes opponent patterns and behavior.
    /// </summary>
    Task<Result<OpponentAnalysis>> AnalyzeOpponentPatternsAsync(
        Guid sessionId,
        IReadOnlyList<GameAction> opponentActions,
        CancellationToken ct = default);

    /// <summary>
    /// Assesses player skill based on performance metrics.
    /// </summary>
    Task<Result<SkillAssessment>> AssessPlayerSkillAsync(
        Guid sessionId,
        SessionMetrics metrics,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a personalized improvement plan.
    /// </summary>
    Task<Result<ImprovementPlan>> GenerateImprovementPlanAsync(
        Guid sessionId,
        SkillAssessment assessment,
        CancellationToken ct = default);

    /// <summary>
    /// Gets contextual tips for the current game situation.
    /// </summary>
    Task<Result<IReadOnlyList<CoachingTip>>> GetContextualTipsAsync(
        Guid sessionId,
        string context,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a comprehensive session report.
    /// </summary>
    Task<Result<CoachingReport>> GenerateSessionReportAsync(
        Guid sessionId,
        CancellationToken ct = default);
}
