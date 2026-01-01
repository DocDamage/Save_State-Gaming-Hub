using SaveState.Core.Common;
using SaveState.Core.Social.Entities;

namespace SaveState.Core.Social.Services;

/// <summary>
/// Service for managing game reviews.
/// </summary>
public interface IGameReviewService
{
    /// <summary>
    /// Creates a new review for a game.
    /// </summary>
    Task<Result<GameReview>> CreateReviewAsync(
        Guid gameId,
        int rating,
        bool isRecommended,
        TimeSpan? playtimeAtReview = null,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing review.
    /// </summary>
    Task<Result<GameReview>> UpdateReviewAsync(
        Guid reviewId,
        int? rating = null,
        string? title = null,
        string? content = null,
        bool? containsSpoilers = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a review by ID.
    /// </summary>
    Task<Result<GameReview>> GetReviewAsync(Guid reviewId, CancellationToken ct = default);

    /// <summary>
    /// Gets the review for a specific game.
    /// </summary>
    Task<Result<GameReview?>> GetGameReviewAsync(Guid gameId, CancellationToken ct = default);

    /// <summary>
    /// Gets reviews with optional filtering.
    /// </summary>
    Task<Result<PagedResult<GameReview>>> GetReviewsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        Guid? gameId = null,
        int? minRating = null,
        int? maxRating = null,
        bool? isRecommended = null,
        bool? containsSpoilers = null,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a review.
    /// </summary>
    Task<Result> DeleteReviewAsync(Guid reviewId, CancellationToken ct = default);

    /// <summary>
    /// Gets review statistics.
    /// </summary>
    Task<Result<GameReviewStatistics>> GetStatisticsAsync(
        Guid? gameId = null,
        CancellationToken ct = default);
}