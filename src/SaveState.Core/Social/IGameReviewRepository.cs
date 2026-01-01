using SaveState.Core.Common;
using SaveState.Core.Social.Entities;

namespace SaveState.Core.Social;

/// <summary>
/// Repository interface for game reviews.
/// </summary>
public interface IGameReviewRepository
{
    /// <summary>
    /// Gets a review by its ID.
    /// </summary>
    Task<GameReview?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets the review for a specific game.
    /// </summary>
    Task<GameReview?> GetByGameIdAsync(Guid gameId, CancellationToken ct = default);

    /// <summary>
    /// Gets all reviews with optional filtering.
    /// </summary>
    Task<PagedResult<GameReview>> GetReviewsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        Guid? gameId = null,
        int? minRating = null,
        int? maxRating = null,
        bool? isRecommended = null,
        bool? containsSpoilers = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets reviews for multiple games.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, GameReview>> GetReviewsForGamesAsync(
        IReadOnlyList<Guid> gameIds,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a new review.
    /// </summary>
    Task AddAsync(GameReview review, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing review.
    /// </summary>
    Task UpdateAsync(GameReview review, CancellationToken ct = default);

    /// <summary>
    /// Deletes a review.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets review statistics.
    /// </summary>
    Task<GameReviewStatistics> GetStatisticsAsync(
        Guid? gameId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a game has been reviewed.
    /// </summary>
    Task<bool> HasReviewAsync(Guid gameId, CancellationToken ct = default);

    /// <summary>
    /// Gets the average rating for a game or all games.
    /// </summary>
    Task<double?> GetAverageRatingAsync(Guid? gameId = null, CancellationToken ct = default);
}

/// <summary>
/// Statistics for game reviews.
/// </summary>
public sealed record GameReviewStatistics(
    int TotalReviews,
    int AverageRating,
    int RecommendedCount,
    int FiveStarReviews,
    int OneStarReviews,
    TimeSpan AveragePlaytimeAtReview);