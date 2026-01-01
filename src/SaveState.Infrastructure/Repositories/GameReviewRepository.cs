using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common;
using SaveState.Core.Social;
using SaveState.Core.Social.Entities;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for game reviews.
/// </summary>
public class GameReviewRepository : IGameReviewRepository
{
    private readonly SaveStateDbContext _context;

    public GameReviewRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    public async Task<GameReview?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.GameReviews
            .Include(r => r.Game)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<GameReview?> GetByGameIdAsync(Guid gameId, CancellationToken ct = default)
    {
        return await _context.GameReviews
            .Include(r => r.Game)
            .FirstOrDefaultAsync(r => r.GameId == gameId, ct)
            .ConfigureAwait(false);
    }

    public async Task<PagedResult<GameReview>> GetReviewsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        Guid? gameId = null,
        int? minRating = null,
        int? maxRating = null,
        bool? isRecommended = null,
        bool? containsSpoilers = null,
        CancellationToken ct = default)
    {
        var query = _context.GameReviews
            .Include(r => r.Game)
            .AsQueryable();

        if (gameId.HasValue)
        {
            query = query.Where(r => r.GameId == gameId.Value);
        }

        if (minRating.HasValue)
        {
            query = query.Where(r => r.Rating >= minRating.Value);
        }

        if (maxRating.HasValue)
        {
            query = query.Where(r => r.Rating <= maxRating.Value);
        }

        if (isRecommended.HasValue)
        {
            query = query.Where(r => r.IsRecommended == isRecommended.Value);
        }

        if (containsSpoilers.HasValue)
        {
            query = query.Where(r => r.ContainsSpoilers == containsSpoilers.Value);
        }

        // Order by creation date (newest first)
        query = query.OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<GameReview>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<IReadOnlyDictionary<Guid, GameReview>> GetReviewsForGamesAsync(
        IReadOnlyList<Guid> gameIds,
        CancellationToken ct = default)
    {
        var reviews = await _context.GameReviews
            .Include(r => r.Game)
            .Where(r => gameIds.Contains(r.GameId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return reviews.ToDictionary(r => r.GameId);
    }

    public async Task AddAsync(GameReview review, CancellationToken ct = default)
    {
        await _context.GameReviews.AddAsync(review, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(GameReview review, CancellationToken ct = default)
    {
        _context.GameReviews.Update(review);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var review = await _context.GameReviews.FindAsync(new object[] { id }, ct).ConfigureAwait(false);
        if (review is not null)
        {
            _context.GameReviews.Remove(review);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task<GameReviewStatistics> GetStatisticsAsync(
        Guid? gameId = null,
        CancellationToken ct = default)
    {
        var query = _context.GameReviews.AsQueryable();

        if (gameId.HasValue)
        {
            query = query.Where(r => r.GameId == gameId.Value);
        }

        var reviews = await query.ToListAsync(ct).ConfigureAwait(false);

        if (!reviews.Any())
        {
            return new GameReviewStatistics(0, 0, 0, 0, 0, TimeSpan.Zero);
        }

        var totalReviews = reviews.Count;
        var averageRating = (int)Math.Round(reviews.Average(r => r.Rating));
        var recommendedCount = reviews.Count(r => r.IsRecommended);
        var fiveStarReviews = reviews.Count(r => r.Rating == 5);
        var oneStarReviews = reviews.Count(r => r.Rating == 1);
        var averagePlaytime = TimeSpan.FromTicks(
            (long)reviews.Average(r => r.PlaytimeAtReview.Ticks));

        return new GameReviewStatistics(
            totalReviews,
            averageRating,
            recommendedCount,
            fiveStarReviews,
            oneStarReviews,
            averagePlaytime);
    }

    public async Task<bool> HasReviewAsync(Guid gameId, CancellationToken ct = default)
    {
        return await _context.GameReviews
            .AnyAsync(r => r.GameId == gameId, ct)
            .ConfigureAwait(false);
    }

    public async Task<double?> GetAverageRatingAsync(Guid? gameId = null, CancellationToken ct = default)
    {
        var query = _context.GameReviews.AsQueryable();

        if (gameId.HasValue)
        {
            query = query.Where(r => r.GameId == gameId.Value);
        }

        var average = await query.AverageAsync(r => (double?)r.Rating, ct).ConfigureAwait(false);
        return average;
    }
}