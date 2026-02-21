using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.Intelligence.Recommendations.Services;

namespace SaveState.Infrastructure.Intelligence.Recommendations;

/// <summary>
/// Hybrid recommendation engine V2 implementation combining collaborative filtering,
/// content-based recommendations, and contextual factors.
/// </summary>
public sealed class HybridRecommendationEngineV2 : IRecommendationEngineV2
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameSessionRepository _sessionRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<HybridRecommendationEngineV2> _logger;
    private readonly Dictionary<Guid, List<RecommendationFeedbackEntry>> _feedbackStore = new();

    // ML model weights (would be trained in production)
    private const float CollaborativeWeight = 0.4f;
    private const float ContentWeight = 0.35f;
    private const float ContextualWeight = 0.25f;

    public HybridRecommendationEngineV2(
        IGameRepository gameRepository,
        IGameSessionRepository sessionRepository,
        ITimeProvider timeProvider,
        ILogger<HybridRecommendationEngineV2> logger)
    {
        _gameRepository = gameRepository;
        _sessionRepository = sessionRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<GameRecommendationV2>>> GetRecommendationsAsync(
        RecommendationContext context,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Generating recommendations for user {UserId} with count {Count}",
                context.UserId, context.Count);

            // Get user's game history
            var userSessions = await _sessionRepository.GetByUserIdAsync(context.UserId, ct)
                .ConfigureAwait(false);
            var playedGameIds = userSessions.Select(s => s.GameId).Distinct().ToHashSet();
            var excludePlayedGames = context.Filters?.ExcludePlayedGames == true;

            // Get candidate games
            var allGames = await _gameRepository.GetAllAsync(ct).ConfigureAwait(false);
            var candidates = allGames
                .Where(g => !excludePlayedGames || !playedGameIds.Contains(g.Id))
                .ToList();

            // Apply filters
            if (context.Filters != null)
            {
                candidates = ApplyFilters(candidates, context.Filters);
            }

            // Calculate scores using different approaches
            var recommendations = new List<GameRecommendationV2>();
            var now = _timeProvider.UtcNow;

            foreach (var game in candidates.Take(100)) // Limit for performance
            {
                var collaborativeScore = CalculateCollaborativeScore(game, userSessions, playedGameIds);
                var contentScore = CalculateContentScore(game, userSessions);
                var contextualScore = CalculateContextualScore(game, context.ContextualFactors);

                var combinedScore = (collaborativeScore * CollaborativeWeight) +
                                   (contentScore * ContentWeight) +
                                   (contextualScore * ContextualWeight);

                if (combinedScore > 0.3f) // Threshold for recommendation
                {
                    var factors = BuildRecommendationFactors(
                        collaborativeScore, contentScore, contextualScore);

                    recommendations.Add(new GameRecommendationV2(
                        Id: Guid.NewGuid(),
                        GameId: game.Id,
                        Title: game.Title,
                        Description: game.Description ?? "",
                        Reason: GenerateReason(factors, combinedScore),
                        ConfidenceScore: combinedScore,
                        CollaborativeScore: collaborativeScore,
                        ContentScore: contentScore,
                        ContextualScore: contextualScore,
                        CoverArtUrl: game.CoverImagePath,
                        MatchingTags: game.Tags?.ToList() ?? new List<string>(),
                        Factors: factors,
                        Source: DetermineSource(factors),
                        IsInLibrary: playedGameIds.Contains(game.Id),
                        GeneratedAt: now));
                }
            }

            // Sort by confidence and take requested count
            var result = recommendations
                .OrderByDescending(r => r.ConfidenceScore)
                .Take(context.Count)
                .ToList();

            _logger.LogInformation(
                "Generated {Count} recommendations for user {UserId}",
                result.Count, context.UserId);

            return Result.Success<IReadOnlyList<GameRecommendationV2>>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to generate recommendations for user {UserId}",
                context.UserId);
            return Result.Failure<IReadOnlyList<GameRecommendationV2>>(
                "Failed to generate recommendations", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PlayNextRecommendation>>> GetPlayNextAsync(
        PlayNextContext context,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Generating Play Next recommendations for user {UserId}",
                context.UserId);

            // Get user's backlog and recent games
            var userSessions = await _sessionRepository.GetByUserIdAsync(context.UserId, ct)
                .ConfigureAwait(false);
            var recentGames = userSessions
                .OrderByDescending(s => s.StartTime)
                .Take(10)
                .Select(s => s.GameId)
                .ToHashSet();

            var allGames = await _gameRepository.GetAllAsync(ct).ConfigureAwait(false);
            var candidates = allGames
                .Where(g => !context.ExcludedGames?.Contains(g.Id.ToString()) ?? true)
                .ToList();

            var recommendations = new List<PlayNextRecommendation>();
            var now = _timeProvider.UtcNow;

            foreach (var game in candidates)
            {
                var fitAnalysis = AnalyzeSessionFit(game, context);
                var fitScore = (fitAnalysis.TimeFitScore +
                              fitAnalysis.MoodFitScore +
                              fitAnalysis.ContextFitScore +
                              fitAnalysis.EnergyFitScore) / 4;

                if (fitScore > 0.4f)
                {
                    var estimatedSession = EstimateSessionLength(game, context);

                    recommendations.Add(new PlayNextRecommendation(
                        Id: Guid.NewGuid(),
                        GameId: game.Id,
                        Title: game.Title,
                        Reason: GeneratePlayNextReason(fitAnalysis, game),
                        FitScore: fitScore,
                        EstimatedSessionLength: estimatedSession,
                        TimeToComplete: game.EstimatedTimeToComplete,
                        FitAnalysis: fitAnalysis,
                        CoverArtUrl: game.CoverImagePath));
                }
            }

            var result = recommendations
                .OrderByDescending(r => r.FitScore)
                .Take(5)
                .ToList();

            return Result.Success<IReadOnlyList<PlayNextRecommendation>>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to generate Play Next recommendations for user {UserId}",
                context.UserId);
            return Result.Failure<IReadOnlyList<PlayNextRecommendation>>(
                "Failed to generate Play Next recommendations", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<SocialGameRecommendation>>> GetSocialRecommendationsAsync(
        Guid userId,
        int count = 5,
        CancellationToken ct = default)
    {
        // Social graph implementation would integrate with friend system
        // For now, return empty list as this requires social features
        _logger.LogInformation(
            "Social recommendations requested for user {UserId} (not implemented)",
            userId);

        return Task.FromResult(
            Result.Success<IReadOnlyList<SocialGameRecommendation>>(
                new List<SocialGameRecommendation>()));
    }

    /// <inheritdoc />
    public Task<Result> RefreshModelAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Refreshing recommendation model");
        // In production, this would retrain ML models
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> ProvideFeedbackAsync(
        Guid recommendationId,
        RecommendationFeedbackV2 feedback,
        CancellationToken ct = default)
    {
        if (!_feedbackStore.ContainsKey(recommendationId))
        {
            _feedbackStore[recommendationId] = new List<RecommendationFeedbackEntry>();
        }

        _feedbackStore[recommendationId].Add(new RecommendationFeedbackEntry(
            feedback.Type,
            _timeProvider.UtcNow,
            feedback.Comment));

        _logger.LogInformation(
            "Received feedback {FeedbackType} for recommendation {RecommendationId}",
            feedback.Type, recommendationId);

        return Task.FromResult(Result.Success());
    }

    // Private helper methods

    private List<Game> ApplyFilters(List<Game> games, RecommendationFilters filters)
    {
        var query = games.AsEnumerable();

        if (filters.Genres?.Any() == true)
        {
            query = query.Where(g =>
                g.Genres?.Any(gg => filters.Genres.Contains(gg.Name)) ?? false);
        }

        if (filters.Platforms?.Any() == true)
        {
            query = query.Where(g =>
                g.Platforms?.Any(p => filters.Platforms.Contains(p.Name)) ?? false);
        }

        if (filters.MinRating.HasValue)
        {
            query = query.Where(g => (g.Rating ?? 0) >= filters.MinRating.Value);
        }

        if (filters.ReleasedAfter.HasValue)
        {
            var releasedAfter = DateOnly.FromDateTime(filters.ReleasedAfter.Value);
            query = query.Where(g => g.ReleaseDate.HasValue && g.ReleaseDate.Value >= releasedAfter);
        }

        if (filters.ReleasedBefore.HasValue)
        {
            var releasedBefore = DateOnly.FromDateTime(filters.ReleasedBefore.Value);
            query = query.Where(g => g.ReleaseDate.HasValue && g.ReleaseDate.Value <= releasedBefore);
        }

        return query.ToList();
    }

    private float CalculateCollaborativeScore(Game game,
        IReadOnlyList<GameSession> userSessions, HashSet<Guid> playedGameIds)
    {
        // Simplified collaborative filtering
        // In production, this would use matrix factorization or similar
        var score = 0f;

        // Boost score for games similar to highly played games
        foreach (var session in userSessions.OrderByDescending(s => s.GetDuration(_timeProvider)).Take(5))
        {
            var similarity = CalculateGameSimilarity(game, session.Game);
            score += similarity * 0.2f;
        }

        return Math.Min(score, 1.0f);
    }

    private float CalculateContentScore(Game game, IReadOnlyList<GameSession> userSessions)
    {
        // Content-based filtering based on genre/tag overlap
        if (!userSessions.Any()) return 0.5f;

        var userGenres = userSessions
            .SelectMany(s => s.Game.Genres?.Select(g => g.Name) ?? Enumerable.Empty<string>())
            .GroupBy(g => g)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();

        var gameGenres = game.Genres?.Select(g => g.Name).ToList() ?? new List<string>();

        if (!userGenres.Any() || !gameGenres.Any()) return 0.3f;

        var matches = gameGenres.Count(g => userGenres.Contains(g));
        return Math.Min(matches / (float)Math.Max(userGenres.Count, 3), 1.0f);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Maintainability",
        "CA1502:Avoid excessive complexity",
        Justification = "Context scoring intentionally combines multiple weighted factors in one method.")]
    private float CalculateContextualScore(Game game, ContextualFactors? factors)
    {
        if (factors == null) return 0.5f;

        var score = 0f;

        // Time of day factor
        score += factors.TimeOfDay switch
        {
            TimeOfDay.Morning => game.Genres?.Any(g =>
                g.Name.Contains("Puzzle") || g.Name.Contains("Strategy")) ?? false ? 0.2f : 0.1f,
            TimeOfDay.Evening => game.Genres?.Any(g =>
                g.Name.Contains("RPG") || g.Name.Contains("Adventure")) ?? false ? 0.2f : 0.1f,
            TimeOfDay.Night => game.Genres?.Any(g =>
                g.Name.Contains("Horror") || g.Name.Contains("Atmospheric")) ?? false ? 0.2f : 0.1f,
            _ => 0.1f
        };

        // Mood factor
        if (factors.Mood.HasValue)
        {
            score += factors.Mood.Value switch
            {
                GamingMood.Competitive => game.Genres?.Any(g =>
                    g.Name.Contains("Action") || g.Name.Contains("Competitive")) ?? false ? 0.2f : 0f,
                GamingMood.Relaxed => game.Genres?.Any(g =>
                    g.Name.Contains("Casual") || g.Name.Contains("Puzzle")) ?? false ? 0.2f : 0f,
                GamingMood.Immersive => game.Genres?.Any(g =>
                    g.Name.Contains("RPG") || g.Name.Contains("Story")) ?? false ? 0.2f : 0f,
                _ => 0.1f
            };
        }

        // Available time factor
        if (game.EstimatedTimeToComplete.HasValue)
        {
            if (game.EstimatedTimeToComplete.Value <= factors.AvailableTime)
            {
                score += 0.2f;
            }
        }

        return Math.Min(score, 1.0f);
    }

    private float CalculateGameSimilarity(Game game1, Game game2)
    {
        if (game1.Id == game2.Id) return 1.0f;

        var genres1 = game1.Genres?.Select(g => g.Name).ToList() ?? new List<string>();
        var genres2 = game2.Genres?.Select(g => g.Name).ToList() ?? new List<string>();

        if (!genres1.Any() || !genres2.Any()) return 0f;

        var intersection = genres1.Intersect(genres2).Count();
        var union = genres1.Union(genres2).Count();

        return union > 0 ? intersection / (float)union : 0f;
    }

    private List<RecommendationFactor> BuildRecommendationFactors(
        float collaborativeScore, float contentScore, float contextualScore)
    {
        var factors = new List<RecommendationFactor>();

        if (collaborativeScore > 0.3f)
        {
            factors.Add(new RecommendationFactor(
                "Similar Players",
                "Players with similar tastes enjoyed this",
                CollaborativeWeight,
                collaborativeScore));
        }

        if (contentScore > 0.3f)
        {
            factors.Add(new RecommendationFactor(
                "Genre Match",
                "Matches your preferred genres",
                ContentWeight,
                contentScore));
        }

        if (contextualScore > 0.3f)
        {
            factors.Add(new RecommendationFactor(
                "Right Now",
                "Fits your current context",
                ContextualWeight,
                contextualScore));
        }

        return factors;
    }

    private string GenerateReason(List<RecommendationFactor> factors, float score)
    {
        var topFactor = factors.OrderByDescending(f => f.Score * f.Weight).FirstOrDefault();
        return topFactor?.Description ?? "Recommended based on your preferences";
    }

    private RecommendationSourceV2 DetermineSource(List<RecommendationFactor> factors)
    {
        if (factors.Count >= 3) return RecommendationSourceV2.Hybrid;
        if (factors.Any(f => f.Name == "Similar Players")) return RecommendationSourceV2.CollaborativeFiltering;
        if (factors.Any(f => f.Name == "Genre Match")) return RecommendationSourceV2.ContentBased;
        if (factors.Any(f => f.Name == "Right Now")) return RecommendationSourceV2.Contextual;
        return RecommendationSourceV2.AiAnalysis;
    }

    private SessionFitAnalysis AnalyzeSessionFit(Game game, PlayNextContext context)
    {
        var timeFit = 0.5f;
        if (game.EstimatedTimeToComplete.HasValue && context.AvailableTime.HasValue)
        {
            var sessionRatio = context.AvailableTime.Value.TotalMinutes /
                              Math.Max(game.EstimatedTimeToComplete.Value.TotalMinutes, 60);
            timeFit = Math.Min((float)sessionRatio, 1.0f);
        }

        var moodFit = 0.5f;
        if (context.PreferredMood.HasValue && game.Genres != null)
        {
            moodFit = context.PreferredMood.Value switch
            {
                GamingMood.QuickSession => game.Genres.Any(g =>
                    g.Name.Contains("Arcade") || g.Name.Contains("Platformer")) ? 0.9f : 0.3f,
                GamingMood.Immersive => game.Genres.Any(g =>
                    g.Name.Contains("RPG") || g.Name.Contains("Open World")) ? 0.9f : 0.3f,
                _ => 0.5f
            };
        }

        return new SessionFitAnalysis(
            TimeFitScore: timeFit,
            MoodFitScore: moodFit,
            ContextFitScore: 0.7f, // Default for now
            EnergyFitScore: context.PreferShortSession ?
                (game.EstimatedTimeToComplete?.TotalHours < 20 ? 0.8f : 0.4f) : 0.6f,
            SuggestedSessionLength: context.AvailableTime.HasValue ?
                $"{context.AvailableTime.Value.TotalMinutes} minutes" : null);
    }

    private TimeSpan EstimateSessionLength(Game game, PlayNextContext context)
    {
        // Default session lengths by genre
        if (game.Genres?.Any(g => g.Name.Contains("RPG") || g.Name.Contains("Strategy")) ?? false)
        {
            return TimeSpan.FromHours(1.5);
        }

        if (game.Genres?.Any(g => g.Name.Contains("Casual") || g.Name.Contains("Puzzle")) ?? false)
        {
            return TimeSpan.FromMinutes(30);
        }

        return TimeSpan.FromHours(1);
    }

    private string GeneratePlayNextReason(SessionFitAnalysis analysis, Game game)
    {
        if (analysis.TimeFitScore > 0.8f)
            return "Perfect fit for your available time";
        if (analysis.MoodFitScore > 0.8f)
            return "Matches your current mood perfectly";
        if (game.Genres?.Any(g => g.Name.Contains("Favorite")) ?? false)
            return "From one of your favorite genres";
        return "Recommended based on your gaming patterns";
    }

    private record RecommendationFeedbackEntry(
        RecommendationFeedbackType Type,
        DateTime Timestamp,
        string? Comment);
}
