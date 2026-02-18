using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Models.AiCoach;

namespace SaveState.Infrastructure.GameLibrary.Services.AiCoach.Engines;

/// <summary>
/// Engine responsible for generating AI-powered recommendations.
/// </summary>
public interface IAiRecommendationEngine
{
    /// <summary>
    /// Assesses player skill level based on session metrics.
    /// </summary>
    Task<Result<SkillAssessment>> AssessSkillAsync(CoachingSession session, SessionMetrics metrics, CancellationToken ct = default);

    /// <summary>
    /// Creates a personalized improvement plan based on skill assessment.
    /// </summary>
    Task<Result<ImprovementPlan>> CreateImprovementPlanAsync(CoachingSession session, SkillAssessment assessment, CancellationToken ct = default);

    /// <summary>
    /// Generates recommendations for player improvement.
    /// </summary>
    Task<Result<IReadOnlyList<Recommendation>>> GenerateRecommendationsAsync(CoachingSession session, CancellationToken ct = default);
}
