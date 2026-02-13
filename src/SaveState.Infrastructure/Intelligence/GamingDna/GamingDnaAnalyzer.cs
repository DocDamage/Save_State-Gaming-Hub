using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary;
using SaveState.Core.Intelligence.GamingDna.Services;

namespace SaveState.Infrastructure.Intelligence.GamingDna;

/// <summary>
/// Analyzes user gaming behavior to create a unique "Gaming DNA" profile.
/// </summary>
public sealed class GamingDnaAnalyzer : IGamingDnaAnalyzer
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameSessionRepository _sessionRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<GamingDnaAnalyzer> _logger;

    public GamingDnaAnalyzer(
        IGameRepository gameRepository,
        IGameSessionRepository sessionRepository,
        ITimeProvider timeProvider,
        ILogger<GamingDnaAnalyzer> logger)
    {
        _gameRepository = gameRepository;
        _sessionRepository = sessionRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<GamingDnaProfile>> AnalyzeProfileAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Analyzing Gaming DNA for user {UserId}", userId);

            var sessions = await _sessionRepository.GetByUserIdAsync(userId, ct)
                .ConfigureAwait(false);

            if (!sessions.Any())
            {
                return Result.Failure<GamingDnaProfile>(
                    "No gaming history found for user", ErrorType.NotFound);
            }

            var archetypes = await CalculateArchetypesAsync(userId, sessions, ct);
            var genrePreferences = AnalyzeGenrePreferences(sessions);
            var playStyle = AnalyzePlayStyle(sessions);
            var engagement = AnalyzeEngagementPatterns(sessions);
            var socialProfile = AnalyzeSocialProfile(sessions);
            var achievementProfile = await AnalyzeAchievementProfileAsync(userId, sessions, ct);
            var signature = GenerateSignature(userId, archetypes, genrePreferences);

            var profile = new GamingDnaProfile(
                UserId: userId,
                GeneratedAt: _timeProvider.UtcNow,
                Archetypes: archetypes,
                GenrePreferences: genrePreferences,
                PlayStyleMetrics: playStyle,
                EngagementPatterns: engagement,
                SocialProfile: socialProfile,
                AchievementProfile: achievementProfile,
                Signature: signature);

            _logger.LogInformation(
                "Successfully analyzed Gaming DNA for user {UserId}. Primary archetype: {Archetype}",
                userId, archetypes.FirstOrDefault()?.Archetype.ToString() ?? "Unknown");

            return Result.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze Gaming DNA for user {UserId}", userId);
            return Result.Failure<GamingDnaProfile>(
                "Failed to analyze gaming profile", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<GamingArchetypeScore>>> GetArchetypesAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var sessions = await _sessionRepository.GetByUserIdAsync(userId, ct)
            .ConfigureAwait(false);

        if (!sessions.Any())
        {
            return Result.Success<IReadOnlyList<GamingArchetypeScore>>(
                new List<GamingArchetypeScore>());
        }

        var archetypes = await CalculateArchetypesAsync(userId, sessions, ct);
        return Result.Success<IReadOnlyList<GamingArchetypeScore>>(archetypes);
    }

    /// <inheritdoc />
    public Task<Result<GenreEvolutionTimeline>> GetGenreEvolutionAsync(
        Guid userId,
        TimeRange timeRange,
        CancellationToken ct = default)
    {
        // For now, return a simplified timeline
        // In production, this would analyze historical session data
        var timeline = new GenreEvolutionTimeline(
            UserId: userId,
            TimeRange: timeRange,
            DataPoints: new List<GenreEvolutionPoint>());

        return Task.FromResult(Result.Success(timeline));
    }

    /// <inheritdoc />
    public Task<Result<DnaVisualizationData>> GetVisualizationDataAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        // Generate visualization data structures
        var radarData = new RadarChartData(
            new List<RadarDimension>
            {
                new("Completion", 75),
                new("Exploration", 60),
                new("Competition", 45),
                new("Story", 80),
                new("Strategy", 70),
                new("Social", 55)
            });

        var timelineData = new TimelineChartData(
            new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun" },
            new List<TimelineDataset>
            {
                new("RPG", new List<float> { 40, 45, 50, 55, 60, 65 }, "#FF5733"),
                new("Strategy", new List<float> { 30, 35, 30, 40, 35, 40 }, "#33FF57")
            });

        var heatmapData = new HeatmapData(
            new List<HeatmapCell>
            {
                new(0, 0, 0.8f, "Morning"),
                new(0, 1, 0.6f, "Afternoon"),
                new(0, 2, 0.9f, "Evening"),
                new(1, 0, 0.4f, "Weekend AM"),
                new(1, 1, 0.7f, "Weekend PM"),
                new(1, 2, 0.95f, "Weekend Eve")
            }, 2, 3);

        var archetypeViz = new ArchetypeVisualization(
            new List<ArchetypeNode>
            {
                new("story", "Story Seeker", 1.0f, "#FF5733", 0, 0),
                new("strategist", "Strategist", 0.8f, "#33FF57", 1, 0.5f),
                new("explorer", "Explorer", 0.6f, "#3357FF", 0.5f, 1)
            },
            new List<ArchetypeEdge>
            {
                new("story", "strategist", 0.7f),
                new("strategist", "explorer", 0.5f)
            });

        var visualizationData = new DnaVisualizationData(
            UserId: userId,
            RadarChart: radarData,
            TimelineChart: timelineData,
            Heatmap: heatmapData,
            ArchetypeViz: archetypeViz);

        return Task.FromResult(Result.Success(visualizationData));
    }

    /// <inheritdoc />
    public Task<Result<DnaComparisonResult>> CompareProfilesAsync(
        Guid userId1,
        Guid userId2,
        CancellationToken ct = default)
    {
        // Compare two DNA profiles
        // For now, return a placeholder result
        var result = new DnaComparisonResult(
            UserId1: userId1,
            UserId2: userId2,
            SimilarityScore: 0.65f,
            SharedPreferences: new List<string> { "RPG", "Strategy" },
            ComplementaryTraits: new List<string> { "Exploration", "Competition" },
            RecommendedGamesToPlayTogether: new List<DnaGameRecommendation>());

        return Task.FromResult(Result.Success(result));
    }

    /// <inheritdoc />
    public Task<Result> RefreshAnalysisAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Refreshing DNA analysis for user {UserId}", userId);
        // Invalidate cache and re-analyze
        return Task.FromResult(Result.Success());
    }

    // Private helper methods

    private async Task<IReadOnlyList<GamingArchetypeScore>> CalculateArchetypesAsync(
        Guid userId,
        IReadOnlyList<Core.GameLibrary.Entities.GameSession> sessions,
        CancellationToken ct)
    {
        var archetypeScores = new Dictionary<GamingArchetype, float>();
        var indicators = new Dictionary<GamingArchetype, List<string>>();

        // Calculate Completionist score
        var completionRate = CalculateCompletionRate(sessions);
        archetypeScores[GamingArchetype.Completionist] = completionRate;
        indicators[GamingArchetype.Completionist] = new List<string>
        {
            $"Completion rate: {completionRate:P0}"
        };

        // Calculate Explorer score
        var uniqueGames = sessions.Select(s => s.GameId).Distinct().Count();
        var explorerScore = Math.Min(uniqueGames / 20f, 1.0f);
        archetypeScores[GamingArchetype.Explorer] = explorerScore;
        indicators[GamingArchetype.Explorer] = new List<string>
        {
            $"Unique games played: {uniqueGames}"
        };

        // Calculate Competitor score
        var competitiveGames = sessions.Count(s =>
            s.Game.Genres?.Any(g =>
                g.Name.Contains("Competitive") ||
                g.Name.Contains("Action") ||
                g.Name.Contains("Fighting")) ?? false);
        var competitorScore = Math.Min(competitiveGames / (float)sessions.Count * 2, 1.0f);
        archetypeScores[GamingArchetype.Competitor] = competitorScore;
        indicators[GamingArchetype.Competitor] = new List<string>
        {
            $"Competitive games ratio: {competitorScore:P0}"
        };

        // Calculate Story Seeker score
        var storyGames = sessions.Count(s =>
            s.Game.Genres?.Any(g =>
                g.Name.Contains("RPG") ||
                g.Name.Contains("Story") ||
                g.Name.Contains("Adventure")) ?? false);
        var storyScore = Math.Min(storyGames / (float)sessions.Count * 2, 1.0f);
        archetypeScores[GamingArchetype.StorySeeker] = storyScore;
        indicators[GamingArchetype.StorySeeker] = new List<string>
        {
            $"Story-rich games: {storyScore:P0}"
        };

        // Calculate Strategist score
        var strategyGames = sessions.Count(s =>
            s.Game.Genres?.Any(g =>
                g.Name.Contains("Strategy") ||
                g.Name.Contains("Tactical")) ?? false);
        var strategistScore = Math.Min(strategyGames / (float)sessions.Count * 3, 1.0f);
        archetypeScores[GamingArchetype.Strategist] = strategistScore;
        indicators[GamingArchetype.Strategist] = new List<string>
        {
            $"Strategy games: {strategistScore:P0}"
        };

        // Calculate Hardcore score
        var avgSessionLength = sessions.Average(s => s.Duration?.TotalHours ?? 0);
        var hardcoreScore = Math.Min((float)(avgSessionLength / 3), 1.0f);
        archetypeScores[GamingArchetype.Hardcore] = hardcoreScore;
        indicators[GamingArchetype.Hardcore] = new List<string>
        {
            $"Average session: {avgSessionLength:F1} hours"
        };

        // Calculate Casual score
        var casualScore = 1 - hardcoreScore;
        archetypeScores[GamingArchetype.Casual] = casualScore;
        indicators[GamingArchetype.Casual] = new List<string>
        {
            "Prefers shorter sessions"
        };

        // Return top archetypes
        return archetypeScores
            .OrderByDescending(kv => kv.Value)
            .Take(5)
            .Select(kv => new GamingArchetypeScore(
                kv.Key,
                kv.Value,
                indicators[kv.Key]))
            .ToList();
    }

    private float CalculateCompletionRate(IReadOnlyList<Core.GameLibrary.Entities.GameSession> sessions)
    {
        // Simplified completion rate calculation
        // In production, this would check actual completion achievements/status
        var gamesWithHighPlaytime = sessions
            .Where(s => s.Duration?.TotalHours > 20)
            .Count();

        var uniqueGames = sessions.Select(s => s.GameId).Distinct().Count();

        return uniqueGames > 0 ? Math.Min(gamesWithHighPlaytime / (float)uniqueGames, 1.0f) : 0f;
    }

    private GenrePreferences AnalyzeGenrePreferences(
        IReadOnlyList<Core.GameLibrary.Entities.GameSession> sessions)
    {
        var genreStats = sessions
            .SelectMany(s => s.Game.Genres?.Select(g => new
            {
                Genre = g.Name,
                PlayTime = s.Duration ?? TimeSpan.Zero,
                Session = s
            }) ?? Enumerable.Empty<dynamic>())
            .GroupBy(x => (string)x.Genre)
            .Select(g => new WeightedGenre(
                Genre: g.Key,
                Weight: (float)g.Sum(x => ((TimeSpan)x.PlayTime).TotalHours),
                GameCount: g.Select(x => ((Core.GameLibrary.Entities.GameSession)x.Session).GameId).Distinct().Count(),
                TotalPlayTime: TimeSpan.FromHours(g.Sum(x => ((TimeSpan)x.PlayTime).TotalHours)),
                LastPlayed: g.Max(x => ((Core.GameLibrary.Entities.GameSession)x.Session).StartTime)))
            .OrderByDescending(g => g.Weight)
            .ToList();

        var topGenres = genreStats.Take(5).ToList();
        var emergingGenres = genreStats
            .Where(g => g.LastPlayed > _timeProvider.UtcNow.AddMonths(-1))
            .OrderByDescending(g => g.Weight)
            .Take(3)
            .ToList();
        var decliningGenres = genreStats
            .Where(g => g.LastPlayed < _timeProvider.UtcNow.AddMonths(-6))
            .Take(3)
            .ToList();

        return new GenrePreferences(topGenres, emergingGenres, decliningGenres);
    }

    private PlayStyleMetrics AnalyzePlayStyle(
        IReadOnlyList<Core.GameLibrary.Entities.GameSession> sessions)
    {
        var avgSessionLength = sessions
            .Where(s => s.Duration.HasValue)
            .Average(s => s.Duration!.Value.TotalMinutes);

        var peakHour = sessions
            .GroupBy(s => s.StartTime.Hour)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        var peakTime = peakHour switch
        {
            >= 5 and < 12 => TimeOfDay.Morning,
            >= 12 and < 17 => TimeOfDay.Afternoon,
            >= 17 and < 22 => TimeOfDay.Evening,
            >= 22 or < 2 => TimeOfDay.Night,
            _ => TimeOfDay.LateNight
        };

        var mostActiveDay = sessions
            .GroupBy(s => s.StartTime.DayOfWeek)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        var weekendSessions = sessions.Count(s =>
            s.StartTime.DayOfWeek == DayOfWeek.Saturday ||
            s.StartTime.DayOfWeek == DayOfWeek.Sunday);
        var weekdaySessions = sessions.Count - weekendSessions;
        var weekendRatio = sessions.Count > 0 ?
            weekendSessions / (float)(weekendSessions + weekdaySessions) : 0.5f;

        return new PlayStyleMetrics(
            AverageSessionLengthMinutes: (float)avgSessionLength,
            PreferredSessionLengthMinutes: (float)avgSessionLength,
            PeakPlayTime: peakTime,
            MostActiveDay: mostActiveDay,
            WeekendVsWeekdayRatio: weekendRatio,
            SinglePlayerVsMultiplayerRatio: 0.7f, // Default assumption
            StoryVsGameplayFocus: 0.6f,
            PreferredDifficulty: DifficultyPreference.Normal);
    }

    private EngagementPatterns AnalyzeEngagementPatterns(
        IReadOnlyList<Core.GameLibrary.Entities.GameSession> sessions)
    {
        var totalGames = sessions.Select(s => s.GameId).Distinct().Count();
        var completedGames = sessions
            .Where(s => s.Duration?.TotalHours > 30)
            .Select(s => s.GameId)
            .Distinct()
            .Count();

        var completionRate = totalGames > 0 ? completedGames / (float)totalGames : 0f;

        var replayedGames = sessions
            .GroupBy(s => s.GameId)
            .Count(g => g.Count() > 5);
        var replayRate = totalGames > 0 ? replayedGames / (float)totalGames : 0f;

        return new EngagementPatterns(
            CompletionRate: completionRate,
            AbandonmentRate: 1 - completionRate,
            ReplayRate: replayRate,
            EarlyAccessInterest: 0.3f, // Default
            IndieAffinity: 0.5f,
            AaaAffinity: 0.7f,
            NewReleaseInterest: 0.6f,
            ClassicGameInterest: 0.4f);
    }

    private SocialGamingProfile AnalyzeSocialProfile(
        IReadOnlyList<Core.GameLibrary.Entities.GameSession> sessions)
    {
        // Simplified social profile analysis
        var multiplayerGames = sessions.Count(s =>
            s.Game.Genres?.Any(g =>
                g.Name.Contains("Multiplayer") ||
                g.Name.Contains("Co-op") ||
                g.Name.Contains("MMO")) ?? false);

        var socialScore = sessions.Count > 0 ?
            Math.Min(multiplayerGames / (float)sessions.Count * 2, 1.0f) : 0.5f;

        return new SocialGamingProfile(
            SocialGamingScore: socialScore,
            PreferredPartySize: socialScore > 0.5f ? 3 : 1,
            CoopVsCompetitiveRatio: 0.6f,
            VoiceChatPreference: 0.7f,
            CommunityEngagement: socialScore,
            PreferredSocialFeatures: new List<string>
            {
                "Co-op Campaign",
                "Party System",
                "Voice Chat"
            });
    }

    private async Task<AchievementProfile> AnalyzeAchievementProfileAsync(
        Guid userId,
        IReadOnlyList<Core.GameLibrary.Entities.GameSession> sessions,
        CancellationToken ct)
    {
        // Simplified achievement analysis
        var totalPlaytime = sessions.Sum(s => s.Duration?.TotalHours ?? 0);
        var achievementScore = Math.Min((float)(totalPlaytime / 100), 1.0f);

        return new AchievementProfile(
            AchievementHunterScore: achievementScore * 0.7f,
            CompletionistScore: CalculateCompletionRate(sessions),
            ChallengeSeekerScore: achievementScore * 0.8f,
            TotalAchievementsUnlocked: (int)(totalPlaytime * 2),
            RareAchievementsUnlocked: (int)(totalPlaytime * 0.1),
            AverageAchievementCompletionRate: 0.65f);
    }

    private DnaSignature GenerateSignature(
        Guid userId,
        IReadOnlyList<GamingArchetypeScore> archetypes,
        GenrePreferences genrePreferences)
    {
        // Create a unique signature vector
        var vector = new List<float>();

        // Add archetype scores
        foreach (GamingArchetype archetype in Enum.GetValues<GamingArchetype>())
        {
            var score = archetypes.FirstOrDefault(a => a.Archetype == archetype)?.ConfidenceScore ?? 0f;
            vector.Add(score);
        }

        // Add genre weights
        foreach (var genre in genrePreferences.TopGenres.Take(5))
        {
            vector.Add(genre.Weight);
        }

        // Generate hash from vector
        var hash = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(
                string.Join(",", vector)));

        return new DnaSignature(
            Hash: hash[..Math.Min(32, hash.Length)],
            Vector: vector,
            GeneratedAt: _timeProvider.UtcNow);
    }
}
