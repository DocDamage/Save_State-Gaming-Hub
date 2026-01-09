using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.Social;
using SaveState.Core.Social.Entities;
using SaveState.Core.Social.Services;

namespace SaveState.Infrastructure.Social;

/// <summary>
/// Service implementation for managing game reviews.
/// </summary>
public class GameReviewService : IGameReviewService
{
    private readonly IGameReviewRepository _reviewRepository;
    private readonly IGameRepository _gameRepository;
    private readonly ISessionTrackingService _sessionTrackingService;
    private readonly ILogger<GameReviewService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameReviewService"/> class.
    /// </summary>
    /// <param name="reviewRepository">Repository for accessing game reviews.</param>
    /// <param name="gameRepository">Repository for accessing games.</param>
    /// <param name="sessionTrackingService">Service for tracking game sessions.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public GameReviewService(
        IGameReviewRepository reviewRepository,
        IGameRepository gameRepository,
        ISessionTrackingService sessionTrackingService,
        ILogger<GameReviewService> logger)
    {
        _reviewRepository = reviewRepository;
        _gameRepository = gameRepository;
        _sessionTrackingService = sessionTrackingService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new game review.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game to review.</param>
    /// <param name="rating">The rating for the game (typically 1-10).</param>
    /// <param name="isRecommended">Whether the reviewer recommends this game.</param>
    /// <param name="playtimeAtReview">The playtime when the review was written.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the created review or an error.</returns>
    public async Task<Result<GameReview>> CreateReviewAsync(
        Guid gameId,
        int rating,
        bool isRecommended,
        TimeSpan? playtimeAtReview = null,
        CancellationToken ct = default)
    {
        try
        {
            // Validate game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct);
            if (game is null)
            {
                return Result.Failure<GameReview>("Game not found", ErrorType.NotFound);
            }

            // Check if review already exists
            var existingReview = await _reviewRepository.GetByGameIdAsync(gameId, ct);
            if (existingReview is not null)
            {
                return Result.Failure<GameReview>("Review already exists for this game", ErrorType.Conflict);
            }

            // Get playtime if not provided
            if (playtimeAtReview is null)
            {
                var playtimeResult = await _sessionTrackingService.GetStatisticsAsync(gameId, ct);
                playtimeAtReview = playtimeResult.IsSuccess ? playtimeResult.Value.TotalPlaytime : TimeSpan.Zero;
            }

            var review = GameReview.Create(gameId, rating, playtimeAtReview.Value, isRecommended);

            await _reviewRepository.AddAsync(review, ct);

            _logger.LogInformation("Created review for game {GameId} with rating {Rating}", gameId, rating);

            return Result.Success<GameReview>(review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create review for game {GameId}", gameId);
            return Result.Failure<GameReview>("Failed to create review", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Updates an existing game review.
    /// </summary>
    /// <param name="reviewId">The unique identifier of the review to update.</param>
    /// <param name="rating">Optional new rating value.</param>
    /// <param name="title">Optional new title for the review.</param>
    /// <param name="content">Optional new content for the review.</param>
    /// <param name="containsSpoilers">Optional flag indicating if the review contains spoilers.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the updated review or an error.</returns>
    public async Task<Result<GameReview>> UpdateReviewAsync(
        Guid reviewId,
        int? rating = null,
        string? title = null,
        string? content = null,
        bool? containsSpoilers = null,
        CancellationToken ct = default)
    {
        try
        {
            var review = await _reviewRepository.GetByIdAsync(reviewId, ct);
            if (review is null)
            {
                return Result.Failure<GameReview>("Review not found", ErrorType.NotFound);
            }

            review.Update(rating, title, content, containsSpoilers);

            await _reviewRepository.UpdateAsync(review, ct);

            _logger.LogInformation("Updated review {ReviewId}", reviewId);

            return Result.Success<GameReview>(review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update review {ReviewId}", reviewId);
            return Result.Failure<GameReview>("Failed to update review", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets a review by its unique identifier.
    /// </summary>
    /// <param name="reviewId">The unique identifier of the review.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the review or an error.</returns>
    public async Task<Result<GameReview>> GetReviewAsync(Guid reviewId, CancellationToken ct = default)
    {
        try
        {
            var review = await _reviewRepository.GetByIdAsync(reviewId, ct);
            if (review is null)
            {
                return Result.Failure<GameReview>("Review not found", ErrorType.NotFound);
            }

            return Result.Success<GameReview>(review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get review {ReviewId}", reviewId);
            return Result.Failure<GameReview>("Failed to get review", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets the review for a specific game.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the review or null if none exists.</returns>
    public async Task<Result<GameReview?>> GetGameReviewAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var review = await _reviewRepository.GetByGameIdAsync(gameId, ct);
            return Result.Success<GameReview?>(review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get review for game {GameId}", gameId);
            return Result.Failure<GameReview?>("Failed to get game review", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets a paginated list of reviews with optional filtering.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of reviews per page.</param>
    /// <param name="gameId">Optional filter by game ID.</param>
    /// <param name="minRating">Optional minimum rating filter.</param>
    /// <param name="maxRating">Optional maximum rating filter.</param>
    /// <param name="isRecommended">Optional filter by recommendation status.</param>
    /// <param name="containsSpoilers">Optional filter by spoiler content.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing a paged list of reviews.</returns>
    public async Task<Result<PagedResult<GameReview>>> GetReviewsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        Guid? gameId = null,
        int? minRating = null,
        int? maxRating = null,
        bool? isRecommended = null,
        bool? containsSpoilers = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _reviewRepository.GetReviewsAsync(
                pageNumber, pageSize, gameId, minRating, maxRating,
                isRecommended, containsSpoilers, ct);

            return Result.Success<PagedResult<GameReview>>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get reviews");
            return Result.Failure<PagedResult<GameReview>>("Failed to get reviews", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Deletes a review by its unique identifier.
    /// </summary>
    /// <param name="reviewId">The unique identifier of the review to delete.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> DeleteReviewAsync(Guid reviewId, CancellationToken ct = default)
    {
        try
        {
            var review = await _reviewRepository.GetByIdAsync(reviewId, ct);
            if (review is null)
            {
                return Result.Failure("Review not found", ErrorType.NotFound);
            }

            await _reviewRepository.DeleteAsync(reviewId, ct);

            _logger.LogInformation("Deleted review {ReviewId}", reviewId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete review {ReviewId}", reviewId);
            return Result.Failure("Failed to delete review", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets review statistics, optionally filtered by game.
    /// </summary>
    /// <param name="gameId">Optional game ID to filter statistics.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing review statistics.</returns>
    public async Task<Result<GameReviewStatistics>> GetStatisticsAsync(
        Guid? gameId = null,
        CancellationToken ct = default)
    {
        try
        {
            var statistics = await _reviewRepository.GetStatisticsAsync(gameId, ct);
            return Result.Success<GameReviewStatistics>(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get review statistics");
            return Result.Failure<GameReviewStatistics>("Failed to get statistics", ErrorType.Internal);
        }
    }
}


