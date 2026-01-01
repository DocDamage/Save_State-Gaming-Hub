using SaveState.Core.Common;

namespace SaveState.Core.Recommendations.Services;

public interface IRecommendationService
{
    Task<Result<IReadOnlyList<GameRecommendation>>> GetRecommendationsAsync(
        int count = 10,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<GameRecommendation>>> GetSimilarGamesAsync(
        Guid gameId,
        int count = 5,
        CancellationToken ct = default);

    Task<Result> ProvideRecommendationFeedbackAsync(
        Guid recommendationId,
        RecommendationFeedback feedback,
        CancellationToken ct = default);
}

public sealed record GameRecommendation(
    Guid Id,
    Guid? GameId,
    string Title,
    string Reason,
    float ConfidenceScore,
    string? CoverArtUrl,
    IReadOnlyList<string> MatchingTags,
    RecommendationSource Source,
    bool IsInLibrary);

public enum RecommendationSource
{
    PlayHistory,
    SimilarUsers,
    GenreMatch,
    TrendingNow,
    BacklogPriority,
    AiAnalysis
}

public enum RecommendationFeedback
{
    Liked,
    Disliked,
    NotInterested,
    AlreadyPlayed,
    AddedToBacklog
}