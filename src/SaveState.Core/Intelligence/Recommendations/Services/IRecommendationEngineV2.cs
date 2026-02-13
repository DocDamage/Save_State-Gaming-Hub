using SaveState.Core.Common;

namespace SaveState.Core.Intelligence.Recommendations.Services;

/// <summary>
/// Advanced recommendation engine V2 with hybrid approach combining collaborative filtering,
/// content-based recommendations, and contextual factors.
/// </summary>
public interface IRecommendationEngineV2
{
    /// <summary>
    /// Gets personalized game recommendations based on multiple factors.
    /// </summary>
    /// <param name="context">The recommendation context including user preferences and constraints.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of game recommendations ordered by relevance.</returns>
    Task<Result<IReadOnlyList<GameRecommendationV2>>> GetRecommendationsAsync(
        RecommendationContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Gets "Play Next" predictions based on current context (time, mood, available time).
    /// </summary>
    /// <param name="context">The context for play next prediction.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of games recommended to play next.</returns>
    Task<Result<IReadOnlyList<PlayNextRecommendation>>> GetPlayNextAsync(
        PlayNextContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Gets recommendations based on social graph (friends' activities and preferences).
    /// </summary>
    /// <param name="userId">The user ID to get social recommendations for.</param>
    /// <param name="count">Number of recommendations to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Social-based game recommendations.</returns>
    Task<Result<IReadOnlyList<SocialGameRecommendation>>> GetSocialRecommendationsAsync(
        Guid userId,
        int count = 5,
        CancellationToken ct = default);

    /// <summary>
    /// Refreshes the recommendation model with latest user data.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the refresh operation.</returns>
    Task<Result> RefreshModelAsync(CancellationToken ct = default);

    /// <summary>
    /// Provides feedback on a recommendation to improve future recommendations.
    /// </summary>
    /// <param name="recommendationId">The recommendation ID.</param>
    /// <param name="feedback">The feedback provided by the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the feedback operation.</returns>
    Task<Result> ProvideFeedbackAsync(
        Guid recommendationId,
        RecommendationFeedbackV2 feedback,
        CancellationToken ct = default);
}

/// <summary>
/// Context for generating recommendations.
/// </summary>
public sealed record RecommendationContext(
    Guid UserId,
    int Count = 10,
    RecommendationFilters? Filters = null,
    ContextualFactors? ContextualFactors = null);

/// <summary>
/// Filters for recommendation queries.
/// </summary>
public sealed record RecommendationFilters(
    IReadOnlyList<string>? Genres = null,
    IReadOnlyList<string>? Platforms = null,
    TimeSpan? MinPlayTime = null,
    TimeSpan? MaxPlayTime = null,
    DateTime? ReleasedAfter = null,
    DateTime? ReleasedBefore = null,
    float? MinRating = null,
    bool ExcludePlayedGames = true,
    bool ExcludeBacklog = false);

/// <summary>
/// Contextual factors affecting recommendations (time, mood, etc.).
/// </summary>
public sealed record ContextualFactors(
    TimeOfDay TimeOfDay,
    DayOfWeek DayOfWeek,
    TimeSpan AvailableTime,
    GamingMood? Mood = null,
    bool IsWeekend = false,
    string? Location = null,
    string? DeviceType = null);

/// <summary>
/// Time of day classification.
/// </summary>
public enum TimeOfDay
{
    Morning,
    Afternoon,
    Evening,
    Night,
    LateNight
}

/// <summary>
/// Gaming mood for contextual recommendations.
/// </summary>
public enum GamingMood
{
    Relaxed,
    Competitive,
    Adventurous,
    Strategic,
    Social,
    QuickSession,
    Immersive
}

/// <summary>
/// Enhanced game recommendation with detailed scoring.
/// </summary>
public sealed record GameRecommendationV2(
    Guid Id,
    Guid? GameId,
    string Title,
    string Description,
    string Reason,
    float ConfidenceScore,
    float CollaborativeScore,
    float ContentScore,
    float ContextualScore,
    string? CoverArtUrl,
    IReadOnlyList<string> MatchingTags,
    IReadOnlyList<RecommendationFactor> Factors,
    RecommendationSourceV2 Source,
    bool IsInLibrary,
    DateTime GeneratedAt);

/// <summary>
/// Individual factor contributing to a recommendation.
/// </summary>
public sealed record RecommendationFactor(
    string Name,
    string Description,
    float Weight,
    float Score);

/// <summary>
/// Recommendation source types V2.
/// </summary>
public enum RecommendationSourceV2
{
    CollaborativeFiltering,
    ContentBased,
    Contextual,
    Trending,
    SocialGraph,
    Hybrid,
    AiAnalysis
}

/// <summary>
/// Enhanced recommendation feedback.
/// </summary>
public sealed record RecommendationFeedbackV2(
    RecommendationFeedbackType Type,
    string? Comment = null,
    int? Rating = null);

/// <summary>
/// Types of recommendation feedback.
/// </summary>
public enum RecommendationFeedbackType
{
    Liked,
    Disliked,
    NotInterested,
    AlreadyPlayed,
    AddedToBacklog,
    PlayedNow,
    Ignored
}

/// <summary>
/// Context for "Play Next" predictions.
/// </summary>
public sealed record PlayNextContext(
    Guid UserId,
    TimeSpan? AvailableTime = null,
    GamingMood? PreferredMood = null,
    bool PreferShortSession = false,
    IReadOnlyList<string>? PreferredGenres = null,
    IReadOnlyList<string>? ExcludedGames = null);

/// <summary>
/// Play next recommendation with session fit analysis.
/// </summary>
public sealed record PlayNextRecommendation(
    Guid Id,
    Guid GameId,
    string Title,
    string Reason,
    float FitScore,
    TimeSpan EstimatedSessionLength,
    TimeSpan? TimeToComplete,
    SessionFitAnalysis FitAnalysis,
    string? CoverArtUrl);

/// <summary>
/// Analysis of how well a game fits the current session context.
/// </summary>
public sealed record SessionFitAnalysis(
    float TimeFitScore,
    float MoodFitScore,
    float ContextFitScore,
    float EnergyFitScore,
    string? SuggestedSessionLength);

/// <summary>
/// Social-based game recommendation.
/// </summary>
public sealed record SocialGameRecommendation(
    Guid Id,
    Guid GameId,
    string Title,
    string Reason,
    float SocialScore,
    int FriendCount,
    IReadOnlyList<FriendActivityInfo> FriendActivities,
    string? CoverArtUrl);

/// <summary>
/// Friend activity information for social recommendations.
/// </summary>
public sealed record FriendActivityInfo(
    Guid FriendId,
    string FriendName,
    string ActivityType,
    DateTime ActivityDate,
    string? Comment = null);

/// <summary>
/// External platform data for enhanced recommendations.
/// </summary>
public interface IExternalPlatformDataProvider
{
    /// <summary>
    /// Gets trending games from external platform.
    /// </summary>
    Task<Result<IReadOnlyList<ExternalGameData>>> GetTrendingGamesAsync(
        string platform,
        int count = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Gets similar games from external platform.
    /// </summary>
    Task<Result<IReadOnlyList<ExternalGameData>>> GetSimilarGamesAsync(
        string platform,
        string gameId,
        int count = 5,
        CancellationToken ct = default);

    /// <summary>
    /// Gets user library from external platform.
    /// </summary>
    Task<Result<IReadOnlyList<ExternalGameData>>> GetUserLibraryAsync(
        string platform,
        string userId,
        CancellationToken ct = default);
}

/// <summary>
/// External game data from platforms like Steam, GOG, Epic.
/// </summary>
public sealed record ExternalGameData(
    string Platform,
    string ExternalId,
    string Title,
    string? Description,
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Tags,
    float? Rating,
    int? ReviewCount,
    DateTime? ReleaseDate,
    string? CoverImageUrl);
