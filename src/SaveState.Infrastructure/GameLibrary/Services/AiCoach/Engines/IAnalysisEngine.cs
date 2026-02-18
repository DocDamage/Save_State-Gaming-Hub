using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Models.AiCoach;

namespace SaveState.Infrastructure.GameLibrary.Services.AiCoach.Engines;

/// <summary>
/// Engine responsible for analyzing gameplay and strategies.
/// </summary>
public interface IAnalysisEngine
{
    /// <summary>
    /// Analyzes player strategy based on recent actions.
    /// </summary>
    Task<Result<StrategyAnalysis>> AnalyzeStrategyAsync(CoachingSession session, IReadOnlyList<GameAction> recentActions, CancellationToken ct = default);

    /// <summary>
    /// Analyzes opponent behavior and patterns.
    /// </summary>
    Task<Result<OpponentAnalysis>> AnalyzeOpponentAsync(CoachingSession session, IReadOnlyList<GameAction> opponentActions, CancellationToken ct = default);

    /// <summary>
    /// Performs comprehensive gameplay analysis.
    /// </summary>
    Task<Result<GameplayAnalysis>> AnalyzeGameplayAsync(Guid sessionId, AnalysisType type, CancellationToken ct = default);
}
