namespace SaveState.Application.Mugen.Services.ContentMarketplace.Engines;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.ContentMarketplace;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for managing content reviews and ratings.
/// </summary>
public class ReviewEngine
{
    private readonly ILogger<ReviewEngine> _logger;
    private readonly ConcurrentDictionary<string, ContentReview> _reviews;
    private readonly ConcurrentDictionary<string, ContentRating> _ratings;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReviewEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ReviewEngine(ILogger<ReviewEngine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _reviews = new ConcurrentDictionary<string, ContentReview>();
        _ratings = new ConcurrentDictionary<string, ContentRating>();
        _timeProvider = new SystemTimeProvider();
    }

    /// <summary>
    /// Rates content with a star rating.
    /// </summary>
    /// <param name="contentId">The content ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="rating">The rating (1-5).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the rating information.</returns>
    public Task<Result<ContentRating>> RateContentAsync(string contentId, string userId, int rating, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(contentId))
                return Task.FromResult(Result.Failure<ContentRating>("Content ID is required", ErrorType.Validation));
            if (string.IsNullOrWhiteSpace(userId))
                return Task.FromResult(Result.Failure<ContentRating>("User ID is required", ErrorType.Validation));
            if (rating < 1 || rating > 5)
                return Task.FromResult(Result.Failure<ContentRating>("Rating must be between 1 and 5", ErrorType.Validation));

            var ratingId = $"{contentId}:{userId}";
            var now = _timeProvider.UtcNow;

            // Calculate new average
            var existingRatings = _ratings.Values.Where(r => r.ContentId == contentId).ToList();
            var totalRatings = existingRatings.Count + (existingRatings.Any(r => r.UserId == userId) ? 0 : 1);
            var sumRatings = existingRatings.Sum(r => r.Rating) + rating - existingRatings.Where(r => r.UserId == userId).Sum(r => r.Rating);
            var averageRating = totalRatings > 0 ? (double)sumRatings / totalRatings : rating;

            var contentRating = new ContentRating
            {
                ContentId = contentId,
                UserId = userId,
                Rating = rating,
                RatedAt = now,
                AverageRating = averageRating,
                TotalRatings = totalRatings
            };

            _ratings[ratingId] = contentRating;
            _logger.LogInformation("Content rated: {ContentId} by {UserId} with {Rating} stars", contentId, userId, rating);

            return Task.FromResult(Result.Success(contentRating));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rate content {ContentId}", contentId);
            return Task.FromResult(Result.Failure<ContentRating>($"Rating failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets reviews for specific content.
    /// </summary>
    /// <param name="contentId">The content ID.</param>
    /// <param name="limit">Maximum number of reviews to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of content reviews.</returns>
    public Task<IReadOnlyList<ContentReview>> GetContentReviewsAsync(string contentId, int limit = 20, CancellationToken cancellationToken = default)
    {
        var reviews = _reviews.Values
            .Where(r => r.ContentId == contentId)
            .OrderByDescending(r => r.IsVerifiedPurchase)
            .ThenByDescending(r => r.HelpfulVotes)
            .ThenByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<ContentReview>>(reviews);
    }

    /// <summary>
    /// Submits a review for content.
    /// </summary>
    /// <param name="contentId">The content ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="comment">The review comment.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the submitted review.</returns>
    public Task<Result<ContentReview>> SubmitReviewAsync(string contentId, string userId, string comment, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(contentId))
                return Task.FromResult(Result.Failure<ContentReview>("Content ID is required", ErrorType.Validation));
            if (string.IsNullOrWhiteSpace(userId))
                return Task.FromResult(Result.Failure<ContentReview>("User ID is required", ErrorType.Validation));
            if (string.IsNullOrWhiteSpace(comment))
                return Task.FromResult(Result.Failure<ContentReview>("Comment is required", ErrorType.Validation));

            var reviewId = Guid.NewGuid().ToString("N");
            var now = _timeProvider.UtcNow;

            // Check if user has rated this content
            var ratingId = $"{contentId}:{userId}";
            var hasRating = _ratings.TryGetValue(ratingId, out var userRating);

            var review = new ContentReview
            {
                ReviewId = reviewId,
                ContentId = contentId,
                UserId = userId,
                UserName = $"User_{userId[..Math.Min(8, userId.Length)]}",
                Rating = hasRating ? userRating!.Rating : 0,
                Comment = comment.Trim(),
                CreatedAt = now,
                UpdatedAt = null,
                IsVerifiedPurchase = false, // Would be determined by purchase history
                HelpfulVotes = 0
            };

            _reviews[reviewId] = review;
            _logger.LogInformation("Review submitted: {ReviewId} for content {ContentId}", reviewId, contentId);

            return Task.FromResult(Result.Success(review));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit review for content {ContentId}", contentId);
            return Task.FromResult(Result.Failure<ContentReview>($"Review submission failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets the average rating for content.
    /// </summary>
    /// <param name="contentId">The content ID.</param>
    /// <returns>The average rating and total count.</returns>
    internal (double Average, int Count) GetRatingStats(string contentId)
    {
        var ratings = _ratings.Values.Where(r => r.ContentId == contentId).ToList();
        if (!ratings.Any())
            return (0, 0);
        
        return (ratings.Average(r => r.Rating), ratings.Count);
    }

    /// <summary>
    /// Marks a review as helpful.
    /// </summary>
    /// <param name="reviewId">The review ID.</param>
    /// <returns>True if successful.</returns>
    internal bool MarkHelpful(string reviewId)
    {
        if (_reviews.TryGetValue(reviewId, out var review))
        {
            review.HelpfulVotes++;
            return true;
        }
        return false;
    }
}
