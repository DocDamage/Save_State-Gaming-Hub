using SaveState.Core.Common;
using SaveState.Application.Mugen.Models.Educational;

namespace SaveState.Application.Mugen.Services.Educational;

/// <summary>
/// Interface for educational content service.
/// </summary>
public interface IEducationalContentService
{
    // Tutorial operations
    Task<Result<IReadOnlyList<Tutorial>>> GetTutorialsAsync(TutorialQuery query, CancellationToken ct = default);
    Task<Result<Tutorial>> GetTutorialAsync(string tutorialId, CancellationToken ct = default);
    Task<Result<TutorialSession>> StartTutorialAsync(string tutorialId, string userId, CancellationToken ct = default);
    Task<Result<TutorialStep>> GetCurrentTutorialStepAsync(string sessionId, CancellationToken ct = default);
    Task<Result<TutorialResponse>> ProcessTutorialActionAsync(string sessionId, TutorialAction action, CancellationToken ct = default);
    Task<Result<Tutorial>> CreateTutorialAsync(TutorialCreationRequest request, CancellationToken ct = default);

    // Guide operations
    Task<Result<IReadOnlyList<StrategyGuide>>> GetStrategyGuidesAsync(StrategyGuideQuery query, CancellationToken ct = default);
    Task<Result<StrategyGuide>> GetStrategyGuideAsync(string guideId, CancellationToken ct = default);
    Task<Result<MechanicsGuide>> GetMechanicsGuideAsync(string topic, CancellationToken ct = default);

    // Learning path operations
    Task<Result<LearningPath>> GetLearningPathAsync(string pathId, CancellationToken ct = default);

    // Progress operations
    Task<Result<LearningProgress>> GetUserProgressAsync(string userId, CancellationToken ct = default);
    Task<Result<decimal>> CalculateLearningProgressAsync(string userId, string category, CancellationToken ct = default);

    // Practice operations
    Task<Result<PracticeSession>> CreatePracticeSessionAsync(PracticeRequest request, CancellationToken ct = default);

    // Dashboard and recommendations
    Task<Result<UserDashboard>> GetUserDashboardAsync(string userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<RecommendedContent>>> GetRecommendationsAsync(string userId, CancellationToken ct = default);

    // Analytics
    Task<Result<ContentAnalytics>> GetContentAnalyticsAsync(TimeSpan period, CancellationToken ct = default);
    Task<IReadOnlyList<string>> AnalyzeMatchAsync(string matchId, string playerId, CancellationToken ct = default);

    // Maintenance
    Task UpdateKnowledgeBaseAsync(CancellationToken ct = default);
}

// Legacy alias
public interface EducationalContentServiceIEducationalContentService : IEducationalContentService { }
