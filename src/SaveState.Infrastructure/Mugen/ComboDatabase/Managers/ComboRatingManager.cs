using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Mugen.ComboDatabase.Managers;

/// <summary>
/// Manages combo ratings, voting, and usage tracking.
/// </summary>
public class ComboRatingManager
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<ComboRatingManager> _logger;

    public ComboRatingManager(
        SaveStateDbContext dbContext,
        ILogger<ComboRatingManager> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Records when a combo is used in a match.
    /// </summary>
    public async Task<Result> RecordComboUsageAsync(
        Guid comboId,
        bool successful,
        CancellationToken ct = default)
    {
        try
        {
            var combo = await _dbContext.ComboEntries
                .FirstOrDefaultAsync(c => c.Id == comboId, ct);

            if (combo == null)
                return Result.Failure($"Combo {comboId} not found", ErrorType.NotFound);

            combo.UsageStats.MatchUsageCount++;
            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record combo usage");
            return Result.Failure($"Failed to record usage: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Rates a combo with 1-5 stars.
    /// </summary>
    public async Task<Result> RateComboAsync(
        Guid comboId,
        int rating,
        string? userId = null,
        CancellationToken ct = default)
    {
        try
        {
            if (rating < 1 || rating > 5)
                return Result.Failure("Rating must be between 1 and 5", ErrorType.Validation);

            var combo = await _dbContext.ComboEntries
                .FirstOrDefaultAsync(c => c.Id == comboId, ct);

            if (combo == null)
                return Result.Failure($"Combo {comboId} not found", ErrorType.NotFound);

            var ratings = combo.Ratings;
            ratings.RatingCount++;

            if (!ratings.RatingDistribution.ContainsKey(rating))
                ratings.RatingDistribution[rating] = 0;
            ratings.RatingDistribution[rating]++;

            ratings.AverageRating = ratings.RatingDistribution.Sum(r => r.Key * r.Value) / ratings.RatingCount;

            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rate combo");
            return Result.Failure($"Failed to rate combo: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Upvotes a combo.
    /// </summary>
    public async Task<Result> UpvoteComboAsync(Guid comboId, CancellationToken ct = default)
    {
        try
        {
            var combo = await _dbContext.ComboEntries
                .FirstOrDefaultAsync(c => c.Id == comboId, ct);

            if (combo == null)
                return Result.Failure($"Combo {comboId} not found", ErrorType.NotFound);

            combo.Ratings.Upvotes++;
            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upvote combo");
            return Result.Failure($"Failed to upvote: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Downvotes a combo.
    /// </summary>
    public async Task<Result> DownvoteComboAsync(Guid comboId, CancellationToken ct = default)
    {
        try
        {
            var combo = await _dbContext.ComboEntries
                .FirstOrDefaultAsync(c => c.Id == comboId, ct);

            if (combo == null)
                return Result.Failure($"Combo {comboId} not found", ErrorType.NotFound);

            combo.Ratings.Downvotes++;
            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to downvote combo");
            return Result.Failure($"Failed to downvote: {ex.Message}", ErrorType.Internal);
        }
    }
}
