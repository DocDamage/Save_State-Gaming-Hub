using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.Recommendations.DTOs;
using SaveState.Core.Recommendations.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Recommendations;

/// <summary>
/// Simplified implementation of game recommendation service.
/// TODO: Enhance with actual play history and user preferences when schema supports it.
/// </summary>
public class GameRecommendationService : IGameRecommendationService
{
    private readonly SaveStateDbContext _context;
    private readonly ILogger<GameRecommendationService> _logger;

    public GameRecommendationService(
        SaveStateDbContext context,
        ILogger<GameRecommendationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<SmartGameRecommendation>>> GetRecommendationsAsync(
        Guid userId,
        int count = 10,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating recommendations for user {UserId}", userId);

            // Get all games with genres and tags
            var games = await _context.Games
                .Include(g => g.Genres)
                .Where(g => !g.IsDeleted)
                .OrderByDescending(g => g.CreatedAt)
                .Take(count * 2) // Get more to filter
                .ToListAsync(ct);

            if (!games.Any())
            {
                return Result.Success<IReadOnlyList<SmartGameRecommendation>>(
                    Array.Empty<SmartGameRecommendation>());
            }

            // Score games based on available data
            var recommendations = games
                .Select(game =>
                {
                    var genreNames = game.Genres.Select(g => g.Name).ToList();
                    var tagList = game.Tags.ToList();
                    var score = CalculateBasicScore(game);

                    return new SmartGameRecommendation(
                        game.Id,
                        game.Title,
                        score,
                        GenerateReason(game, genreNames),
                        genreNames,
                        tagList,
                        null); // EstimatedPlayTime not available in current schema
                })
                .OrderByDescending(r => r.ConfidenceScore)
                .Take(count)
                .ToList();

            _logger.LogInformation("Generated {Count} recommendations", recommendations.Count);
            return Result.Success<IReadOnlyList<SmartGameRecommendation>>(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate recommendations");
            return Result.Failure<IReadOnlyList<SmartGameRecommendation>>(
                "Failed to generate recommendations", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<SmartSimilarGame>>> GetSimilarGamesAsync(
        Guid gameId,
        int count = 5,
        CancellationToken ct = default)
    {
        try
        {
            var sourceGame = await _context.Games
                .Include(g => g.Genres)
                .FirstOrDefaultAsync(g => g.Id == gameId, ct);

            if (sourceGame == null)
            {
                return Result.Failure<IReadOnlyList<SmartSimilarGame>>(
                    "Game not found", ErrorType.NotFound);
            }

            var sourceGenres = sourceGame.Genres.Select(g => g.Name).ToHashSet();
            var sourceTags = sourceGame.Tags.ToHashSet();

            var otherGames = await _context.Games
                .Include(g => g.Genres)
                .Where(g => g.Id != gameId && !g.IsDeleted)
                .ToListAsync(ct);

            var similarGames = otherGames
                .Select(game =>
                {
                    var gameGenres = game.Genres.Select(g => g.Name).ToHashSet();
                    var gameTags = game.Tags.ToHashSet();

                    var sharedGenres = sourceGenres.Intersect(gameGenres).ToList();
                    var sharedTags = sourceTags.Intersect(gameTags).ToList();

                    var similarityScore = CalculateSimilarity(
                        sourceGenres, gameGenres, sourceTags, gameTags);

                    return new
                    {
                        Game = game,
                        Score = similarityScore,
                        SharedGenres = sharedGenres,
                        SharedTags = sharedTags
                    };
                })
                .Where(x => x.Score > 0.1f)
                .OrderByDescending(x => x.Score)
                .Take(count)
                .Select(x => new SmartSimilarGame(
                    x.Game.Id,
                    x.Game.Title,
                    x.Score * 100,
                    x.SharedGenres,
                    x.SharedTags))
                .ToList();

            return Result.Success<IReadOnlyList<SmartSimilarGame>>(similarGames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get similar games");
            return Result.Failure<IReadOnlyList<SmartSimilarGame>>(
                "Failed to get similar games", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<SmartTrendingGame>>> GetTrendingGamesAsync(
        int count = 10,
        CancellationToken ct = default)
    {
        try
        {
            // Simplified: Return recently added games as "trending"
            var recentGames = await _context.Games
                .Where(g => !g.IsDeleted)
                .OrderByDescending(g => g.CreatedAt)
                .Take(count)
                .ToListAsync(ct);

            var trending = recentGames.Select((game, index) => new SmartTrendingGame(
                game.Id,
                game.Title,
                100 - (index * 5), // Decreasing trend score
                0, // Active players not tracked yet
                game.UserRating.HasValue ? (float)game.UserRating.Value : null
            )).ToList();

            return Result.Success<IReadOnlyList<SmartTrendingGame>>(trending);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get trending games");
            return Result.Failure<IReadOnlyList<SmartTrendingGame>>(
                "Failed to get trending games", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<SmartBacklogRecommendation>>> GetBacklogRecommendationsAsync(
        Guid userId,
        int count = 5,
        CancellationToken ct = default)
    {
        try
        {
            var backlogGames = await _context.BacklogEntries
                .Include(b => b.Game)
                    .ThenInclude(g => g.Genres)
                .Where(b => b.Status == BacklogStatus.NotStarted)
                .OrderBy(b => b.AddedAt)
                .Take(count)
                .ToListAsync(ct);

            var recommendations = backlogGames.Select(entry =>
            {
                var priority = CalculateBacklogPriority(entry);
                var reason = GenerateBacklogReason(entry);

                return new SmartBacklogRecommendation(
                    entry.GameId,
                    entry.Game.Title,
                    priority,
                    reason,
                    entry.EstimatedPlaytime,
                    entry.AddedAt
                );
            }).ToList();

            return Result.Success<IReadOnlyList<SmartBacklogRecommendation>>(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get backlog recommendations");
            return Result.Failure<IReadOnlyList<SmartBacklogRecommendation>>(
                "Failed to get backlog recommendations", ErrorType.Internal);
        }
    }

    #region Private Helper Methods

    private float CalculateBasicScore(Game game)
    {
        float score = 50; // Base score

        // Boost for having genres
        if (game.Genres.Any())
            score += 20;

        // Boost for having tags
        if (game.Tags.Any())
            score += 15;

        // Boost for user rating
        if (game.UserRating.HasValue)
            score += (float)(game.UserRating.Value / 5.0 * 15);

        return Math.Min(score, 100);
    }

    private float CalculateSimilarity(
        HashSet<string> sourceGenres,
        HashSet<string> targetGenres,
        HashSet<string> sourceTags,
        HashSet<string> targetTags)
    {
        // Jaccard similarity
        var genreUnion = sourceGenres.Union(targetGenres).Count();
        var genreIntersection = sourceGenres.Intersect(targetGenres).Count();
        var genreSimilarity = genreUnion > 0 ? genreIntersection / (float)genreUnion : 0;

        var tagUnion = sourceTags.Union(targetTags).Count();
        var tagIntersection = sourceTags.Intersect(targetTags).Count();
        var tagSimilarity = tagUnion > 0 ? tagIntersection / (float)tagUnion : 0;

        return (genreSimilarity * 0.6f) + (tagSimilarity * 0.4f);
    }

    private float CalculateBacklogPriority(BacklogEntry entry)
    {
        float priority = 50;

        // Boost for explicit priority
        priority += entry.Priority * 0.3f;

        // Boost for older items
        var daysInBacklog = (DateTime.UtcNow - entry.AddedAt).Days;
        priority += Math.Min(daysInBacklog / 30.0f, 20);

        // Boost for shorter games
        if (entry.EstimatedPlaytime.HasValue)
        {
            var hours = entry.EstimatedPlaytime.Value.TotalHours;
            if (hours < 10)
                priority += 15;
            else if (hours < 20)
                priority += 10;
        }

        return Math.Min(priority, 100);
    }

    private string GenerateReason(Game game, List<string> genres)
    {
        if (genres.Any())
        {
            return $"Features {string.Join(", ", genres.Take(2))}";
        }

        if (game.UserRating.HasValue && game.UserRating.Value >= 4.0)
        {
            return "Highly rated game";
        }

        return "Popular in your library";
    }

    private string GenerateBacklogReason(BacklogEntry entry)
    {
        var daysInBacklog = (DateTime.UtcNow - entry.AddedAt).Days;

        if (daysInBacklog > 90)
        {
            return "Been in your backlog for a while";
        }

        if (entry.EstimatedPlaytime.HasValue && entry.EstimatedPlaytime.Value.TotalHours < 10)
        {
            return "Quick game you can finish soon";
        }

        if (entry.Priority > 70)
        {
            return "High priority game";
        }

        return "Ready to play from your backlog";
    }

    #endregion
}

