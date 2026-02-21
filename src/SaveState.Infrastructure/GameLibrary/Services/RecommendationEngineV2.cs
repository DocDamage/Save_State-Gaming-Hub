using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Models.Recommendations;
using SaveState.Core.GameLibrary.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Implementation of the advanced hybrid recommendation engine V2.
/// Provides recommendations based on time, mood, social factors, and user preferences.
/// </summary>
public class RecommendationEngineV2 : IRecommendationEngineV2
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameSessionRepository _sessionRepository;
    private readonly ILogger<RecommendationEngineV2> _logger;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationEngineV2"/> class.
    /// </summary>
    public RecommendationEngineV2(
        IGameRepository gameRepository,
        IGameSessionRepository sessionRepository,
        ITimeProvider timeProvider,
        ILogger<RecommendationEngineV2> logger)
    {
        _gameRepository = gameRepository;
        _sessionRepository = sessionRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<GameRecommendation>>> GetRecommendationsAsync(
        RecommendationContext context,
        int count = 10,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Generating recommendations for timeOfDay={TimeOfDay}, mood={Mood}, availableTime={AvailableTime}",
                context.TimeOfDay,
                context.CurrentMood?.ToString() ?? "not specified",
                context.AvailableTime);

            var allGames = await _gameRepository.GetAllAsync(ct);
            var recentlyPlayed = context.RecentlyPlayed.ToHashSet();

            // Filter out recently played games
            var candidates = allGames.Where(g => !recentlyPlayed.Contains(g.Id)).ToList();

            _logger.LogDebug("Found {CandidateCount} candidate games after filtering recently played", candidates.Count);

            // Score each candidate
            var scoredGames = new List<ScoredGame>();
            foreach (var game in candidates)
            {
                var score = await ScoreGameAsync(game, context, ct);
                if (score.Score > 0.3f) // Minimum threshold
                {
                    scoredGames.Add(score);
                }
            }

            // Sort by score descending
            var topRecommendations = scoredGames
                .OrderByDescending(s => s.Score)
                .Take(count)
                .Select(s => ToRecommendation(s))
                .ToList();

            _logger.LogInformation(
                "Generated {Count} recommendations from {ScoredCount} scored games",
                topRecommendations.Count,
                scoredGames.Count);

            return Result<IReadOnlyList<GameRecommendation>>.Success(topRecommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate recommendations");
            return Result<IReadOnlyList<GameRecommendation>>.Failure(
                "Failed to generate recommendations", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<GameRecommendation>>> GetPlayNextAsync(
        PlayNextContext context,
        int count = 5,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Generating Play Next recommendations for {FinishedCount} just-finished games",
                context.JustFinished.Count);

            // Convert PlayNextContext to RecommendationContext
            var hour = context.CurrentTime.Hour;
            var timeOfDay = hour switch
            {
                >= 6 and < 12 => TimeOfDay.Morning,
                >= 12 and < 17 => TimeOfDay.Afternoon,
                >= 17 and < 22 => TimeOfDay.Evening,
                _ => TimeOfDay.Night
            };

            // Get games similar to just finished, but not the same ones
            var justFinishedSet = context.JustFinished.ToHashSet();
            var allGames = await _gameRepository.GetAllAsync(ct);

            // Find games similar to just finished
            var justFinishedGames = allGames.Where(g => justFinishedSet.Contains(g.Id)).ToList();
            var justFinishedGenres = justFinishedGames
                .SelectMany(g => g.Genres)
                .Select(g => g.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Score candidates based on similarity to just finished
            var candidates = allGames.Where(g => !justFinishedSet.Contains(g.Id)).ToList();
            var scoredGames = new List<ScoredGame>();

            foreach (var game in candidates)
            {
                var scores = new List<(float weight, float score, string factor)>();

                // Genre similarity (high weight for Play Next)
                var gameGenres = game.Genres.Select(g => g.Name).ToList();
                var genreOverlap = gameGenres.Count(g => justFinishedGenres.Contains(g));
                var genreScore = gameGenres.Count > 0 ? (float)genreOverlap / gameGenres.Count : 0.5f;
                scores.Add((0.40f, genreScore, $"Similar genre: {genreScore:P0}"));

                // Time appropriateness
                var timeScore = CalculateTimeScore(game, timeOfDay, context.AvailableTime);
                scores.Add((0.25f, timeScore, $"Time appropriate: {timeScore:P0}"));

                // Mood matching
                if (context.CurrentMood.HasValue)
                {
                    var moodScore = CalculateMoodScore(game, context.CurrentMood.Value);
                    scores.Add((0.25f, moodScore, $"Mood match: {moodScore:P0}"));
                }

                // Session length fit
                var estimatedTime = game.EstimatedTimeToComplete ?? TimeSpan.FromHours(1);
                var sessionFit = estimatedTime <= context.AvailableTime ? 1.0f : 0.5f;
                scores.Add((0.10f, sessionFit, $"Session fit: {sessionFit:P0}"));

                var totalWeight = scores.Sum(s => s.weight);
                var finalScore = scores.Sum(s => s.weight * s.score) / totalWeight;

                if (finalScore > 0.3f)
                {
                    scoredGames.Add(new ScoredGame
                    {
                        Game = game,
                        Score = finalScore,
                        Reason = genreScore > 0.5f ? RecommendationReason.SimilarToRecent : RecommendationReason.TimeAppropriate,
                        Factors = scores.Select(s => s.factor).ToList()
                    });
                }
            }

            var topRecommendations = scoredGames
                .OrderByDescending(s => s.Score)
                .Take(count)
                .Select(s => ToRecommendation(s))
                .ToList();

            return Result<IReadOnlyList<GameRecommendation>>.Success(topRecommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Play Next recommendations");
            return Result<IReadOnlyList<GameRecommendation>>.Failure(
                "Failed to generate Play Next recommendations", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<GameRecommendation>>> GetSocialRecommendationsAsync(
        SocialRecommendationContext context,
        int count = 5,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Generating social recommendations for {FriendCount} friends",
                context.FriendUsernames.Count);

            var allGames = await _gameRepository.GetAllAsync(ct);
            var friendsPlayingSet = context.FriendsCurrentlyPlaying.ToHashSet();
            var friendsRecommendationsSet = context.FriendsRecommendations.ToHashSet();

            var scoredGames = new List<ScoredGame>();

            foreach (var game in allGames)
            {
                var scores = new List<(float weight, float score, string factor)>();
                var reasons = new List<RecommendationReason>();

                // Friends currently playing (highest weight)
                if (friendsPlayingSet.Contains(game.Id))
                {
                    scores.Add((0.50f, 1.0f, "Friends are playing now"));
                    reasons.Add(RecommendationReason.FriendPlaying);
                }

                // Friends recommendations
                if (friendsRecommendationsSet.Contains(game.Id))
                {
                    scores.Add((0.40f, 0.9f, "Recommended by friends"));
                    reasons.Add(RecommendationReason.SimilarToRecent);
                }

                // Multiplayer suitability bonus
                var hasMultiplayer = game.Tags.Any(t => t.Contains("multiplayer", StringComparison.OrdinalIgnoreCase));
                if (hasMultiplayer)
                {
                    scores.Add((0.10f, 1.0f, "Multiplayer available"));
                }

                if (scores.Count > 0)
                {
                    var totalWeight = scores.Sum(s => s.weight);
                    var finalScore = scores.Sum(s => s.weight * s.score) / totalWeight;

                    scoredGames.Add(new ScoredGame
                    {
                        Game = game,
                        Score = finalScore,
                        Reason = reasons.FirstOrDefault(RecommendationReason.FriendPlaying),
                        Factors = scores.Select(s => s.factor).ToList()
                    });
                }
            }

            var topRecommendations = scoredGames
                .OrderByDescending(s => s.Score)
                .Take(count)
                .Select(s => ToRecommendation(s))
                .ToList();

            return Result<IReadOnlyList<GameRecommendation>>.Success(topRecommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate social recommendations");
            return Result<IReadOnlyList<GameRecommendation>>.Failure(
                "Failed to generate social recommendations", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<GameRecommendation>>> GetTrendingAsync(
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Fetching trending games");

            // Get recent sessions to determine trending games
            var recentSessions = await _sessionRepository.GetRecentSessionsAsync(TimeSpan.FromDays(7), 100, ct);

            // Count sessions per game
            var gameSessionCounts = recentSessions
                .GroupBy(s => s.GameId)
                .Select(g => new { GameId = g.Key, SessionCount = g.Count(), LastPlayed = g.Max(s => s.StartTime) })
                .OrderByDescending(g => g.SessionCount)
                .ThenByDescending(g => g.LastPlayed)
                .Take(10)
                .ToList();

            var allGames = await _gameRepository.GetAllAsync(ct);
            var gameDict = allGames.ToDictionary(g => g.Id);

            var recommendations = new List<GameRecommendation>();
            foreach (var sessionInfo in gameSessionCounts)
            {
                if (gameDict.TryGetValue(sessionInfo.GameId, out var game))
                {
                    var score = Math.Min(1.0f, sessionInfo.SessionCount / 10.0f); // Normalize
                    recommendations.Add(new GameRecommendation
                    {
                        GameId = game.Id,
                        GameTitle = game.Title,
                        Score = score,
                        Reason = RecommendationReason.Trending,
                        Factors = new[] { $"{sessionInfo.SessionCount} recent plays", "Trending in community" },
                        CoverImageUrl = game.CoverImagePath,
                        EstimatedPlaytime = game.EstimatedTimeToComplete,
                        Confidence = score
                    });
                }
            }

            return Result<IReadOnlyList<GameRecommendation>>.Success(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch trending games");
            return Result<IReadOnlyList<GameRecommendation>>.Failure(
                "Failed to fetch trending games", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<GameRecommendation>>> GetHiddenGemsAsync(
        int count = 10,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Finding hidden gems");

            var allGames = await _gameRepository.GetAllAsync(ct);

            // Hidden gems: high user rating but low total playtime (lesser known)
            var hiddenGems = allGames
                .Where(g => g.UserRating.HasValue &&
                           g.UserRating.Value >= 4.0 &&
                           g.TotalPlayTime < TimeSpan.FromHours(10))
                .OrderByDescending(g => g.UserRating.Value)
                .ThenBy(g => g.TotalPlayTime)
                .Take(count)
                .Select(game => new GameRecommendation
                {
                    GameId = game.Id,
                    GameTitle = game.Title,
                    Score = (float)(game.UserRating.Value / 5.0),
                    Reason = RecommendationReason.HiddenGem,
                    Factors = new[]
                    {
                        $"Rated {game.UserRating.Value:F1}/5",
                        "Undiscovered gem",
                        "High quality, low visibility"
                    },
                    CoverImageUrl = game.CoverImagePath,
                    EstimatedPlaytime = game.EstimatedTimeToComplete,
                    Confidence = (float)(game.UserRating.Value / 5.0)
                })
                .ToList();

            return Result<IReadOnlyList<GameRecommendation>>.Success(hiddenGems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find hidden gems");
            return Result<IReadOnlyList<GameRecommendation>>.Failure(
                "Failed to find hidden gems", ErrorType.Internal);
        }
    }

    private async Task<ScoredGame> ScoreGameAsync(
        Game game,
        RecommendationContext context,
        CancellationToken ct)
    {
        var scores = new List<(float weight, float score, string factor)>();

        // Genre preference scoring (weight: 0.25)
        var genreScore = CalculateGenreScore(game, context.PreferredGenres);
        scores.Add((0.25f, genreScore, $"Genre match: {genreScore:P0}"));

        // Time-appropriate scoring (weight: 0.20)
        var timeScore = CalculateTimeScore(game, context.TimeOfDay, context.AvailableTime);
        scores.Add((0.20f, timeScore, $"Time appropriate: {timeScore:P0}"));

        // Mood matching (weight: 0.20)
        if (context.CurrentMood.HasValue)
        {
            var moodScore = CalculateMoodScore(game, context.CurrentMood.Value);
            scores.Add((0.20f, moodScore, $"Mood match: {moodScore:P0}"));
        }

        // Player count (weight: 0.15)
        var playerScore = CalculatePlayerCountScore(game, context.PlayerCount);
        scores.Add((0.15f, playerScore, $"Player count: {playerScore:P0}"));

        // Similar to recent (weight: 0.20)
        var similarityScore = await CalculateSimilarityScoreAsync(game, context.RecentlyPlayed, ct);
        scores.Add((0.20f, similarityScore, $"Similar to recent: {similarityScore:P0}"));

        // Calculate weighted average
        var totalWeight = scores.Sum(s => s.weight);
        var finalScore = scores.Sum(s => s.weight * s.score) / totalWeight;

        // Determine primary reason
        var primaryReason = DeterminePrimaryReason(scores);

        return new ScoredGame
        {
            Game = game,
            Score = finalScore,
            Reason = primaryReason,
            Factors = scores.Select(s => s.factor).ToList()
        };
    }

    private float CalculateGenreScore(Game game, IReadOnlyList<string> preferredGenres)
    {
        if (preferredGenres.Count == 0) return 0.5f;

        var gameGenres = game.Genres.Select(g => g.Name).ToList();
        var matches = gameGenres.Count(g => preferredGenres.Contains(g, StringComparer.OrdinalIgnoreCase));
        return (float)matches / Math.Max(gameGenres.Count, 1);
    }

    private float CalculateTimeScore(Game game, TimeOfDay timeOfDay, TimeSpan availableTime)
    {
        // Score based on session length fit
        var avgSession = game.EstimatedTimeToComplete ?? TimeSpan.FromHours(1);
        var sessionFit = avgSession <= availableTime ? 1.0f : 0.3f;

        // Time of day preferences (some genres better at certain times)
        var gameGenres = game.Genres.Select(g => g.Name).ToList();
        var timeMultiplier = timeOfDay switch
        {
            TimeOfDay.Morning => gameGenres.Contains("Puzzle", StringComparer.OrdinalIgnoreCase) ||
                                gameGenres.Contains("Strategy", StringComparer.OrdinalIgnoreCase) ? 1.2f : 0.9f,
            TimeOfDay.Evening => gameGenres.Contains("RPG", StringComparer.OrdinalIgnoreCase) ||
                                gameGenres.Contains("Adventure", StringComparer.OrdinalIgnoreCase) ? 1.2f : 1.0f,
            TimeOfDay.Night => gameGenres.Contains("Horror", StringComparer.OrdinalIgnoreCase) ||
                              game.Tags.Contains("Atmospheric") ? 1.2f : 0.9f,
            _ => 1.0f
        };

        return Math.Min(sessionFit * timeMultiplier, 1.0f);
    }

    private float CalculateMoodScore(Game game, Mood mood)
    {
        var gameGenres = game.Genres.Select(g => g.Name).ToList();

        return mood switch
        {
            Mood.Relaxed => gameGenres.Contains("Casual", StringComparer.OrdinalIgnoreCase) ||
                           gameGenres.Contains("Puzzle", StringComparer.OrdinalIgnoreCase) ? 1.0f : 0.4f,
            Mood.Competitive => gameGenres.Contains("FPS", StringComparer.OrdinalIgnoreCase) ||
                               gameGenres.Contains("Fighting", StringComparer.OrdinalIgnoreCase) ||
                               gameGenres.Contains("Racing", StringComparer.OrdinalIgnoreCase) ? 1.0f : 0.3f,
            Mood.Adventurous => gameGenres.Contains("RPG", StringComparer.OrdinalIgnoreCase) ||
                               gameGenres.Contains("Adventure", StringComparer.OrdinalIgnoreCase) ||
                               gameGenres.Contains("Open World", StringComparer.OrdinalIgnoreCase) ? 1.0f : 0.4f,
            Mood.Nostalgic => game.ReleaseDate.HasValue &&
                             game.ReleaseDate.Value < DateOnly.FromDateTime(DateTime.Now.AddYears(-10)) ? 1.0f :
                             gameGenres.Contains("Retro", StringComparer.OrdinalIgnoreCase) ? 1.0f : 0.3f,
            Mood.Social => game.Tags.Any(t => t.Contains("multiplayer", StringComparison.OrdinalIgnoreCase)) ? 1.0f : 0.2f,
            _ => 0.5f
        };
    }

    private float CalculatePlayerCountScore(Game game, int playerCount)
    {
        var hasMultiplayer = game.Tags.Any(t => t.Contains("multiplayer", StringComparison.OrdinalIgnoreCase));

        if (playerCount == 1)
            return !hasMultiplayer ? 1.0f : 0.7f; // Solo play

        return hasMultiplayer ? 1.0f : 0.2f; // Multiplayer
    }

    private async Task<float> CalculateSimilarityScoreAsync(
        Game game,
        IReadOnlyList<Guid> recentlyPlayed,
        CancellationToken ct)
    {
        if (recentlyPlayed.Count == 0) return 0.5f;

        // Get genre overlap with recently played games
        var recentGames = await _gameRepository.GetByIdsAsync(recentlyPlayed, ct);
        var recentGenres = recentGames
            .SelectMany(g => g.Genres)
            .Select(g => g.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var gameGenres = game.Genres.Select(g => g.Name).ToList();
        var overlap = gameGenres.Count(g => recentGenres.Contains(g));

        var maxCount = Math.Max(gameGenres.Count, recentGenres.Count);
        return maxCount > 0 ? (float)overlap / maxCount : 0.5f;
    }

    private RecommendationReason DeterminePrimaryReason(List<(float weight, float score, string factor)> scores)
    {
        var maxScore = scores.OrderByDescending(s => s.weight * s.score).First();

        return maxScore.factor switch
        {
            var f when f.Contains("Genre", StringComparison.OrdinalIgnoreCase) => RecommendationReason.GenrePreference,
            var f when f.Contains("Time", StringComparison.OrdinalIgnoreCase) => RecommendationReason.TimeAppropriate,
            var f when f.Contains("Mood", StringComparison.OrdinalIgnoreCase) => RecommendationReason.MoodMatch,
            var f when f.Contains("Similar", StringComparison.OrdinalIgnoreCase) => RecommendationReason.SimilarToRecent,
            var f when f.Contains("Player", StringComparison.OrdinalIgnoreCase) => RecommendationReason.Backlog,
            _ => RecommendationReason.Backlog
        };
    }

    private GameRecommendation ToRecommendation(ScoredGame scored)
    {
        return new GameRecommendation
        {
            GameId = scored.Game.Id,
            GameTitle = scored.Game.Title,
            Score = scored.Score,
            Reason = scored.Reason,
            Factors = scored.Factors,
            CoverImageUrl = scored.Game.CoverImagePath,
            EstimatedPlaytime = scored.Game.EstimatedTimeToComplete,
            Confidence = scored.Score
        };
    }

    private class ScoredGame
    {
        public required Game Game { get; init; }
        public required float Score { get; init; }
        public required RecommendationReason Reason { get; init; }
        public required IReadOnlyList<string> Factors { get; init; }
    }
}
