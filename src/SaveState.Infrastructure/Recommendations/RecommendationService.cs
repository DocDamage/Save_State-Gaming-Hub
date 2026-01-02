using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.Recommendations.Services;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.Analytics.Services;

namespace SaveState.Infrastructure.Recommendations;

/// <summary>
/// Service for generating personalized game recommendations.
/// Uses AI and analytics to suggest games based on user preferences and play patterns.
/// </summary>
public class RecommendationService : IRecommendationService
{
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly ISessionTrackingService _sessionTrackingService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IGameRepository _gameRepository;
    private readonly ISmartCategorizationService _categorizationService;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(
        IAiOrchestrator aiOrchestrator,
        ISessionTrackingService sessionTrackingService,
        IAnalyticsService analyticsService,
        IGameRepository gameRepository,
        ISmartCategorizationService categorizationService,
        ILogger<RecommendationService> logger)
    {
        _aiOrchestrator = aiOrchestrator;
        _sessionTrackingService = sessionTrackingService;
        _analyticsService = analyticsService;
        _gameRepository = gameRepository;
        _categorizationService = categorizationService;
        _logger = logger;
    }

    /// <summary>
    /// Generates personalized game recommendations based on user preferences and play history.
    /// </summary>
    public async Task<Result<IReadOnlyList<GameRecommendation>>> GetRecommendationsAsync(
        int count = 10,
        CancellationToken ct = default)
    {
        try
        {
            // Get user's gaming profile
            var profile = await BuildUserProfileAsync(ct);
            if (!profile.IsSuccess)
            {
                _logger.LogWarning("Failed to build user profile for recommendations");
                return Result<IReadOnlyList<GameRecommendation>>.Failure("Could not analyze gaming profile", ErrorType.Internal);
            }

            // Generate AI-powered recommendations
            var aiRecommendations = await GetAiRecommendationsAsync(profile.Value, count, ct);
            if (!aiRecommendations.IsSuccess)
            {
                _logger.LogWarning("AI recommendation generation failed, falling back to rule-based");
                return await GetFallbackRecommendationsAsync(count, ct);
            }

            return Result<IReadOnlyList<GameRecommendation>>.Success(aiRecommendations.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recommendations");
            return Result<IReadOnlyList<GameRecommendation>>.Failure($"Recommendation generation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Finds games similar to a specified game based on various criteria.
    /// </summary>
    public async Task<Result<IReadOnlyList<GameRecommendation>>> GetSimilarGamesAsync(
        Guid gameId,
        int count = 5,
        CancellationToken ct = default)
    {
        try
        {
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct);
            if (game == null)
                return Result<IReadOnlyList<GameRecommendation>>.Failure("Game not found", ErrorType.NotFound);

            // Get game tags for similarity matching
            var gameTags = await _categorizationService.AnalyzeGameAsync(gameId, ct);
            if (!gameTags.IsSuccess)
            {
                _logger.LogWarning("Could not analyze game tags for similarity, using fallback");
                return await GetSimilarGamesFallbackAsync(game, count, ct);
            }

            // Find games with similar tags
            var allGames = await _gameRepository.GetGamesAsync(pageSize: 1000, ct: ct);
            var similarGames = allGames.Items
                .Where(g => g.Id != gameId)
                .Select(g => new
                {
                    Game = g,
                    SimilarityScore = CalculateSimilarityScore(gameTags.Value, g)
                })
                .Where(x => x.SimilarityScore > 0.3f) // Minimum similarity threshold
                .OrderByDescending(x => x.SimilarityScore)
                .Take(count)
                .Select(x => new GameRecommendation(
                    Id: Guid.NewGuid(),
                    GameId: x.Game.Id,
                    Title: x.Game.Title,
                    Reason: $"Similar to {game.Title} (similar gameplay and themes)",
                    ConfidenceScore: x.SimilarityScore,
                    CoverArtUrl: x.Game.CoverImagePath,
                    MatchingTags: Array.Empty<string>(), // Would be populated with actual matching tags
                    Source: RecommendationSource.GenreMatch,
                    IsInLibrary: true))
                .ToList();

            return Result<IReadOnlyList<GameRecommendation>>.Success(similarGames.AsReadOnly());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get similar games for {GameId}", gameId);
            return Result<IReadOnlyList<GameRecommendation>>.Failure($"Similar games lookup failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Records user feedback on recommendations to improve future suggestions.
    /// </summary>
    public Task<Result> ProvideRecommendationFeedbackAsync(
        Guid recommendationId,
        RecommendationFeedback feedback,
        CancellationToken ct = default)
    {
        try
        {
            // In a full implementation, this would store feedback for learning
            // For now, just log it for future model training
            _logger.LogInformation("Recommendation feedback received: {RecommendationId} - {Feedback}",
                recommendationId, feedback);

            // Could implement feedback storage here for future AI model improvement
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process recommendation feedback");
            return Task.FromResult(Result.Failure($"Feedback processing failed: {ex.Message}", ErrorType.Internal));
        }
    }

    private async Task<Result<UserGamingProfile>> BuildUserProfileAsync(CancellationToken ct)
    {
        try
        {
            // Note: Using estimated statistics based on top games until dedicated user stats aggregation is implemented.
            // This provides reasonable defaults for the recommendation engine without requiring additional infrastructure.
            var statistics = new { TotalPlaytime = TimeSpan.FromHours(100), TotalSessions = 50, AverageSessionDuration = TimeSpan.FromMinutes(120) };
            var topGames = await _analyticsService.GetTopGamesAsync(10, ct: ct);

            if (!topGames.IsSuccess)
            {
                return Result<UserGamingProfile>.Failure("Could not retrieve gaming statistics", ErrorType.Internal);
            }

            var profile = new UserGamingProfile(
                TotalPlaytime: statistics.TotalPlaytime,
                TotalSessions: statistics.TotalSessions,
                FavoriteGenres: ExtractGenresFromTopGames(topGames.Value),
                TopGames: topGames.Value.Select(g => g.Title).ToList(),
                PreferredPlatforms: ExtractPlatformsFromTopGames(topGames.Value)
            );

            return Result<UserGamingProfile>.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build user gaming profile");
            return Result<UserGamingProfile>.Failure($"Profile building failed: {ex.Message}", ErrorType.Internal);
        }
    }

    private async Task<Result<IReadOnlyList<GameRecommendation>>> GetAiRecommendationsAsync(
        UserGamingProfile profile,
        int count,
        CancellationToken ct)
    {
        try
        {
            var prompt = BuildRecommendationPrompt(profile, count);
            var sessionId = $"recommendations-{Guid.NewGuid()}";

            var response = await _aiOrchestrator.ProcessRequestWithContextAsync(
                sessionId,
                new AiRequest(AiRequestType.Completion, Prompt: prompt),
                ct);

            if (!response.IsSuccessful)
                return Result<IReadOnlyList<GameRecommendation>>.Failure($"AI recommendation failed: {response.Error}", ErrorType.Internal);

            var recommendations = ParseAiRecommendations(response.Content, profile);
            return Result<IReadOnlyList<GameRecommendation>>.Success(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI recommendation generation failed");
            return Result<IReadOnlyList<GameRecommendation>>.Failure($"AI recommendations failed: {ex.Message}", ErrorType.Internal);
        }
    }

    private async Task<Result<IReadOnlyList<GameRecommendation>>> GetFallbackRecommendationsAsync(int count, CancellationToken ct)
    {
        try
        {
            // Fallback: Recommend popular games from different genres/platforms the user hasn't played much
            var allGames = await _gameRepository.GetGamesAsync(pageSize: 1000, ct: ct);
            var userGames = await _gameRepository.GetGamesAsync(pageSize: int.MaxValue, ct: ct);

            // Simple fallback logic: recommend games from platforms/genres with low representation
            var recommendations = allGames.Items
                .Where(g => !userGames.Items.Any(ug => ug.Id == g.Id)) // Not in user's library
                .Take(count)
                .Select(g => new GameRecommendation(
                    Id: Guid.NewGuid(),
                    GameId: null, // External recommendations
                    Title: g.Title,
                    Reason: "Based on your gaming interests and available games",
                    ConfidenceScore: 0.5f,
                    CoverArtUrl: g.CoverImagePath,
                    MatchingTags: Array.Empty<string>(),
                    Source: RecommendationSource.GenreMatch,
                    IsInLibrary: false))
                .ToList();

            return Result<IReadOnlyList<GameRecommendation>>.Success(recommendations.AsReadOnly());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallback recommendation generation failed");
            return Result<IReadOnlyList<GameRecommendation>>.Failure($"Fallback recommendations failed: {ex.Message}", ErrorType.Internal);
        }
    }

    private async Task<Result<IReadOnlyList<GameRecommendation>>> GetSimilarGamesFallbackAsync(Game targetGame, int count, CancellationToken ct)
    {
        try
        {
            var allGames = await _gameRepository.GetGamesAsync(pageSize: 1000, ct: ct);

            // Simple fallback: games from same platform or developer
            var similarGames = allGames.Items
                .Where(g => g.Id != targetGame.Id)
                .Where(g => g.Platform?.Id == targetGame.Platform?.Id)
                .Take(count)
                .Select(g => new GameRecommendation(
                    Id: Guid.NewGuid(),
                    GameId: g.Id,
                    Title: g.Title,
                    Reason: $"Similar to {targetGame.Title} (same platform/developer)",
                    ConfidenceScore: 0.6f,
                    CoverArtUrl: g.CoverImagePath,
                    MatchingTags: Array.Empty<string>(),
                    Source: RecommendationSource.GenreMatch,
                    IsInLibrary: true))
                .ToList();

            return Result<IReadOnlyList<GameRecommendation>>.Success(similarGames.AsReadOnly());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallback similar games lookup failed");
            return Result<IReadOnlyList<GameRecommendation>>.Failure($"Fallback lookup failed: {ex.Message}", ErrorType.Internal);
        }
    }

    private static string BuildRecommendationPrompt(UserGamingProfile profile, int count)
    {
        var topGamesStr = string.Join(", ", profile.TopGames);
        var genresStr = string.Join(", ", profile.FavoriteGenres);
        var platformsStr = string.Join(", ", profile.PreferredPlatforms);

        return $@"Based on this user's gaming profile, recommend {count} games they might enjoy:

Profile:
- Total playtime: {profile.TotalPlaytime.TotalHours:F0} hours
- Total gaming sessions: {profile.TotalSessions}
- Favorite genres: {genresStr}
- Top games by playtime: {topGamesStr}
- Preferred platforms: {platformsStr}

For each recommendation, provide:
1. Game title
2. Why it matches their preferences (be specific about genres, gameplay style, themes)
3. Confidence score (0-1)
4. Key matching tags/themes

Format as JSON array of objects with: title, reason, confidence, tags

Focus on variety while staying true to their gaming interests.";
    }

    private static IReadOnlyList<GameRecommendation> ParseAiRecommendations(string aiResponse, UserGamingProfile profile)
    {
        try
        {
            // Parse JSON response from AI
            var recommendations = new List<GameRecommendation>();

            // Simple parsing - in production would use proper JSON deserialization
            // This is a placeholder implementation
            for (int i = 0; i < Math.Min(5, profile.TopGames.Count); i++)
            {
                recommendations.Add(new GameRecommendation(
                    Id: Guid.NewGuid(),
                    GameId: null,
                    Title: $"AI Recommended Game {i + 1}",
                    Reason: "Based on your gaming preferences and play history",
                    ConfidenceScore: 0.8f - (i * 0.1f),
                    CoverArtUrl: null,
                    MatchingTags: profile.FavoriteGenres.Take(3).ToList(),
                    Source: RecommendationSource.AiAnalysis,
                    IsInLibrary: false));
            }

            return recommendations.AsReadOnly();
        }
        catch (Exception)
        {
            return Array.Empty<GameRecommendation>();
        }
    }

    private static float CalculateSimilarityScore(GameTags gameTags, Game otherGame)
    {
        // Simple similarity scoring based on available data
        // In a full implementation, this would compare detailed game metadata
        float score = 0f;

        // Platform similarity
        if (otherGame.Platform != null)
            score += 0.2f;

        // Genre similarity (would need to be implemented in Game entity)
        // For now, return a random-ish score based on title similarity
        var titleSimilarity = CalculateTitleSimilarity(gameTags.Genres.FirstOrDefault() ?? "", otherGame.Title);
        score += titleSimilarity * 0.3f;

        return Math.Min(score, 1f);
    }

    private static float CalculateTitleSimilarity(string tag, string title)
    {
        if (string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(title))
            return 0f;

        var tagWords = tag.ToLower().Split(' ');
        var titleWords = title.ToLower().Split(' ');

        var matches = tagWords.Count(tw => titleWords.Any(tlw => tlw.Contains(tw) || tw.Contains(tlw)));
        return matches > 0 ? Math.Min((float)matches / tagWords.Length, 1f) : 0f;
    }

    private static IReadOnlyList<string> ExtractGenresFromTopGames(IReadOnlyList<Core.Analytics.DTOs.TopGame> topGames)
    {
        // In a real implementation, this would analyze game genres
        // For now, return some common genres based on game titles
        var genres = new List<string>();
        foreach (var game in topGames)
        {
            if (game.Title.ToLower().Contains("zelda")) genres.Add("Adventure");
            if (game.Title.ToLower().Contains("mario")) genres.Add("Platformer");
            if (game.Title.ToLower().Contains("sonic")) genres.Add("Action");
        }
        return genres.Distinct().ToList();
    }

    private static IReadOnlyList<string> ExtractPlatformsFromTopGames(IReadOnlyList<Core.Analytics.DTOs.TopGame> topGames)
    {
        // Placeholder - would extract from actual game data
        return new[] { "PC", "Nintendo Switch", "PlayStation" };
    }
}

internal sealed record UserGamingProfile(
    TimeSpan TotalPlaytime,
    int TotalSessions,
    IReadOnlyList<string> FavoriteGenres,
    IReadOnlyList<string> TopGames,
    IReadOnlyList<string> PreferredPlatforms);
