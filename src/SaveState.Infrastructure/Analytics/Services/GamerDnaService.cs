using SaveState.Core.Analytics.Models.GamerProfile;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using Microsoft.Extensions.Logging;

namespace SaveState.Infrastructure.Analytics.Services;

/// <summary>
/// Service implementation for analyzing and managing gamer DNA profiles.
/// </summary>
public class GamerDnaService : IGamerDnaService
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameSessionRepository _sessionRepository;
    private readonly IAchievementRepository _achievementRepository;
    private readonly ILogger<GamerDnaService> _logger;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GamerDnaService"/> class.
    /// </summary>
    public GamerDnaService(
        IGameRepository gameRepository,
        IGameSessionRepository sessionRepository,
        IAchievementRepository achievementRepository,
        ITimeProvider timeProvider,
        ILogger<GamerDnaService> logger)
    {
        _gameRepository = gameRepository;
        _sessionRepository = sessionRepository;
        _achievementRepository = achievementRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<GamerDnaProfile>> AnalyzeProfileAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Analyzing gamer DNA profile for user {UserId}", userId);

            // Get all games for the user
            var allGames = await _gameRepository.GetAllAsync(ct);
            // Filter games that belong to this user (for now, we'll use all games)
            var games = allGames.ToList();

            // Get all sessions
            var allSessions = await _sessionRepository.GetAllAsync(ct);
            var sessions = allSessions.ToList();

            // Get user achievements
            var userAchievements = await _achievementRepository.GetUserAchievementsAsync(userId, ct);
            var achievements = userAchievements.ToList();

            // Calculate archetype scores
            var archetypeScores = CalculateArchetypeScores(games, sessions, achievements);
            var primaryArchetype = archetypeScores.OrderByDescending(s => s.Value).First().Key;

            // Calculate genre preferences
            var genrePreferences = CalculateGenrePreferences(games, sessions);

            // Calculate platform preferences
            var platformPreferences = CalculatePlatformPreferences(games, sessions);

            // Calculate playstyle metrics
            var playstyle = CalculatePlaystyleMetrics(games, sessions, achievements);

            var profile = new GamerDnaProfile
            {
                UserId = userId,
                PrimaryArchetype = primaryArchetype,
                ArchetypeScores = archetypeScores,
                GenrePreferences = genrePreferences,
                PlatformPreferences = platformPreferences,
                Playstyle = playstyle,
                GeneratedAt = _timeProvider.UtcNow,
                LastUpdated = _timeProvider.UtcNow
            };

            _logger.LogInformation(
                "Successfully analyzed gamer DNA for user {UserId}. Primary archetype: {Archetype}",
                userId, primaryArchetype);

            return Result<GamerDnaProfile>.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze gamer DNA for user {UserId}", userId);
            return Result<GamerDnaProfile>.Failure("Failed to analyze profile", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<GamerDnaProfile>> AnalyzeProfileWithHistoryAsync(
        Guid userId,
        DateTime since,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Analyzing gamer DNA profile for user {UserId} since {Since}",
                userId, since);

            // Get games
            var allGames = await _gameRepository.GetAllAsync(ct);
            var games = allGames.ToList();

            // Get sessions since the specified date
            var sessions = await _sessionRepository.GetByDateRangeAsync(since, _timeProvider.UtcNow, ct);
            var sessionList = sessions.ToList();

            // Get user achievements
            var userAchievements = await _achievementRepository.GetUserAchievementsAsync(userId, ct);
            var achievements = userAchievements.ToList();

            // Calculate metrics
            var archetypeScores = CalculateArchetypeScores(games, sessionList, achievements);
            var primaryArchetype = archetypeScores.OrderByDescending(s => s.Value).First().Key;

            var profile = new GamerDnaProfile
            {
                UserId = userId,
                PrimaryArchetype = primaryArchetype,
                ArchetypeScores = archetypeScores,
                GenrePreferences = CalculateGenrePreferences(games, sessionList),
                PlatformPreferences = CalculatePlatformPreferences(games, sessionList),
                Playstyle = CalculatePlaystyleMetrics(games, sessionList, achievements),
                GeneratedAt = _timeProvider.UtcNow,
                LastUpdated = _timeProvider.UtcNow
            };

            return Result<GamerDnaProfile>.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze gamer DNA with history for user {UserId}", userId);
            return Result<GamerDnaProfile>.Failure("Failed to analyze profile with history", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<DnaEvolutionSnapshot>>> GetEvolutionHistoryAsync(
        Guid userId,
        int months = 12,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving evolution history for user {UserId} for {Months} months",
                userId, months);

            // This would typically query a historical snapshots table
            // For now, return simulated data based on current analysis
            var snapshots = GenerateMockEvolutionHistory(months);

            return Task.FromResult(Result<IReadOnlyList<DnaEvolutionSnapshot>>.Success(snapshots));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get evolution history for user {UserId}", userId);
            return Task.FromResult(Result<IReadOnlyList<DnaEvolutionSnapshot>>.Failure(
                "Failed to retrieve evolution history", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public async Task<Result<GamerArchetype>> DetermineArchetypeAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var profileResult = await AnalyzeProfileAsync(userId, ct);

        if (profileResult.IsFailure)
        {
            return Result<GamerArchetype>.Failure(profileResult.Error!, profileResult.ErrorType);
        }

        return Result<GamerArchetype>.Success(profileResult.Value!.PrimaryArchetype);
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<GamerArchetype>>> GetAllArchetypesAsync(
        CancellationToken ct = default)
    {
        var archetypes = Enum.GetValues<GamerArchetype>().ToList();
        return Task.FromResult(Result<IReadOnlyList<GamerArchetype>>.Success(archetypes));
    }

    /// <inheritdoc />
    public Task<Result> UpdateProfileAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        // This would trigger a background job to update the profile
        _logger.LogInformation("Profile update triggered for user {UserId}", userId);
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public async Task<Result<ShareableProfileCard>> GenerateShareableCardAsync(
        Guid userId,
        ProfileCardTheme theme = ProfileCardTheme.Cyberpunk,
        CancellationToken ct = default)
    {
        var profileResult = await AnalyzeProfileAsync(userId, ct);

        if (profileResult.IsFailure)
        {
            return Result<ShareableProfileCard>.Failure(profileResult.Error!, profileResult.ErrorType);
        }

        var profile = profileResult.Value!;
        var shareCode = GenerateShareCode(userId);

        var card = new ShareableProfileCard
        {
            DisplayName = $"Gamer_{userId.ToString()[..8]}",
            PrimaryArchetype = profile.PrimaryArchetype,
            TopGenres = profile.GenrePreferences.Take(3).Select(g => g.Genre).ToList(),
            KeyStats = new Dictionary<string, string>
            {
                ["Games Owned"] = profile.Playstyle.TotalGamesOwned.ToString(),
                ["Total Playtime"] = $"{profile.Playstyle.TotalPlaytimeHours:F0}h",
                ["Completion Rate"] = $"{profile.Playstyle.CompletionRate:P0}",
                ["Achievements"] = profile.Playstyle.TotalAchievementsUnlocked.ToString()
            },
            Theme = theme.ToString().ToLowerInvariant(),
            ShareCode = shareCode
        };

        return Result<ShareableProfileCard>.Success(card);
    }

    /// <inheritdoc />
    public async Task<Result<float>> CompareProfilesAsync(
        Guid userId1,
        Guid userId2,
        CancellationToken ct = default)
    {
        var profile1Result = await AnalyzeProfileAsync(userId1, ct);
        var profile2Result = await AnalyzeProfileAsync(userId2, ct);

        if (profile1Result.IsFailure)
        {
            return Result<float>.Failure(profile1Result.Error!, profile1Result.ErrorType);
        }

        if (profile2Result.IsFailure)
        {
            return Result<float>.Failure(profile2Result.Error!, profile2Result.ErrorType);
        }

        var profile1 = profile1Result.Value!;
        var profile2 = profile2Result.Value!;

        // Calculate similarity score based on archetype scores
        float similarity = 0;
        foreach (var archetype in Enum.GetValues<GamerArchetype>())
        {
            var score1 = profile1.ArchetypeScores.GetValueOrDefault(archetype, 0);
            var score2 = profile2.ArchetypeScores.GetValueOrDefault(archetype, 0);
            similarity += 1 - Math.Abs(score1 - score2);
        }

        similarity /= Enum.GetValues<GamerArchetype>().Length;

        return Result<float>.Success(similarity);
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<GamerTypeQuizQuestion>>> GetQuizQuestionsAsync(
        CancellationToken ct = default)
    {
        var questions = new List<GamerTypeQuizQuestion>
        {
            new()
            {
                Question = "What excites you most about a new game?",
                Answers = new List<GamerTypeQuizAnswer>
                {
                    new() { Text = "Discovering every secret and hidden area", Archetype = GamerArchetype.Explorer, Weight = 3 },
                    new() { Text = "Unlocking all achievements and 100% completion", Archetype = GamerArchetype.Completionist, Weight = 3 },
                    new() { Text = "The competitive multiplayer experience", Archetype = GamerArchetype.Competitor, Weight = 3 },
                    new() { Text = "An engaging story and memorable characters", Archetype = GamerArchetype.StorySeeker, Weight = 3 }
                }
            },
            new()
            {
                Question = "How do you prefer to spend your gaming time?",
                Answers = new List<GamerTypeQuizAnswer>
                {
                    new() { Text = "Planning strategies and tactical approaches", Archetype = GamerArchetype.Strategist, Weight = 3 },
                    new() { Text = "Trying to beat the game as fast as possible", Archetype = GamerArchetype.Speedrunner, Weight = 3 },
                    new() { Text = "Playing with friends and making new ones", Archetype = GamerArchetype.Socialite, Weight = 3 },
                    new() { Text = "Relaxing with easy, casual experiences", Archetype = GamerArchetype.Casual, Weight = 3 }
                }
            },
            new()
            {
                Question = "What do you value most in your game library?",
                Answers = new List<GamerTypeQuizAnswer>
                {
                    new() { Text = "Having a large, diverse collection", Archetype = GamerArchetype.Collector, Weight = 3 },
                    new() { Text = "Only the hardest, most challenging games", Archetype = GamerArchetype.Hardcore, Weight = 3 },
                    new() { Text = "Games with rich stories to experience", Archetype = GamerArchetype.StorySeeker, Weight = 2 },
                    new() { Text = "Games that test my skills competitively", Archetype = GamerArchetype.Competitor, Weight = 2 }
                }
            },
            new()
            {
                Question = "When playing a new RPG, you typically:",
                Answers = new List<GamerTypeQuizAnswer>
                {
                    new() { Text = "Follow the main story closely", Archetype = GamerArchetype.StorySeeker, Weight = 2 },
                    new() { Text = "Explore every corner of the world first", Archetype = GamerArchetype.Explorer, Weight = 3 },
                    new() { Text = "Optimize your build for maximum efficiency", Archetype = GamerArchetype.Strategist, Weight = 2 },
                    new() { Text = "Complete every side quest and collectible", Archetype = GamerArchetype.Completionist, Weight = 3 }
                }
            },
            new()
            {
                Question = "Your ideal gaming session length is:",
                Answers = new List<GamerTypeQuizAnswer>
                {
                    new() { Text = "Quick 15-30 minute sessions", Archetype = GamerArchetype.Casual, Weight = 3 },
                    new() { Text = "1-2 hours, a good balance", Archetype = GamerArchetype.Socialite, Weight = 2 },
                    new() { Text = "3-4 hours for serious progress", Archetype = GamerArchetype.Strategist, Weight = 2 },
                    new() { Text = "All day if I could!", Archetype = GamerArchetype.Hardcore, Weight = 3 }
                }
            }
        };

        return Task.FromResult(Result<IReadOnlyList<GamerTypeQuizQuestion>>.Success(questions));
    }

    /// <inheritdoc />
    public Task<Result<GamerTypeQuizResult>> ProcessQuizAnswersAsync(
        IReadOnlyList<(GamerArchetype Archetype, int Weight)> answers,
        CancellationToken ct = default)
    {
        // Calculate scores based on answers
        var scores = new Dictionary<GamerArchetype, int>();
        foreach (var archetype in Enum.GetValues<GamerArchetype>())
        {
            scores[archetype] = 0;
        }

        foreach (var (archetype, weight) in answers)
        {
            scores[archetype] += weight;
        }

        var primaryArchetype = scores.OrderByDescending(s => s.Value).First().Key;

        var result = new GamerTypeQuizResult
        {
            PrimaryArchetype = primaryArchetype,
            ArchetypeScores = scores,
            Description = primaryArchetype.GetDescription(),
            RecommendedGenres = primaryArchetype.GetRecommendedGenres()
        };

        return Task.FromResult(Result<GamerTypeQuizResult>.Success(result));
    }

    #region Private Helper Methods

    private Dictionary<GamerArchetype, float> CalculateArchetypeScores(
        IReadOnlyList<Game> games,
        IReadOnlyList<GameSession> sessions,
        IReadOnlyList<UserAchievement> achievements)
    {
        var scores = new Dictionary<GamerArchetype, float>
        {
            [GamerArchetype.Completionist] = CalculateCompletionistScore(games, achievements),
            [GamerArchetype.Explorer] = CalculateExplorerScore(games, sessions),
            [GamerArchetype.Competitor] = CalculateCompetitorScore(games, sessions),
            [GamerArchetype.StorySeeker] = CalculateStorySeekerScore(games, sessions),
            [GamerArchetype.Strategist] = CalculateStrategistScore(games, sessions),
            [GamerArchetype.Speedrunner] = CalculateSpeedrunnerScore(games, sessions),
            [GamerArchetype.Socialite] = CalculateSocialiteScore(games, sessions),
            [GamerArchetype.Collector] = CalculateCollectorScore(games),
            [GamerArchetype.Casual] = CalculateCasualScore(sessions),
            [GamerArchetype.Hardcore] = CalculateHardcoreScore(games, sessions)
        };

        // Normalize scores to 0-1 range
        var maxScore = scores.Values.Max();
        if (maxScore > 0)
        {
            foreach (var key in scores.Keys.ToList())
            {
                scores[key] /= maxScore;
            }
        }

        return scores;
    }

    private float CalculateCompletionistScore(IReadOnlyList<Game> games, IReadOnlyList<UserAchievement> achievements)
    {
        var unlockedCount = achievements.Count(a => a.IsUnlocked);
        var completedGames = games.Count(g => g.IsCompleted);
        var totalGames = games.Count;

        return (unlockedCount * 0.01f) + (completedGames / (float)Math.Max(totalGames, 1) * 0.5f);
    }

    private float CalculateExplorerScore(IReadOnlyList<Game> games, IReadOnlyList<GameSession> sessions)
    {
        var openWorldGames = games.Count(g =>
            g.Genres.Any(genre =>
                genre.Name.Contains("Open World", StringComparison.OrdinalIgnoreCase) ||
                genre.Name.Contains("Exploration", StringComparison.OrdinalIgnoreCase)));

        var explorationTime = sessions
            .Where(s => s.Game != null && s.Game.Genres.Any(g =>
                g.Name.Contains("Open World", StringComparison.OrdinalIgnoreCase) ||
                g.Name.Contains("Exploration", StringComparison.OrdinalIgnoreCase)))
            .Sum(s => s.Duration.TotalHours);

        return openWorldGames * 0.1f + (float)explorationTime * 0.01f;
    }

    private float CalculateCompetitorScore(IReadOnlyList<Game> games, IReadOnlyList<GameSession> sessions)
    {
        var competitiveGames = games.Count(g =>
            g.Genres.Any(genre =>
                genre.Name.Contains("Multiplayer", StringComparison.OrdinalIgnoreCase) ||
                genre.Name.Contains("Competitive", StringComparison.OrdinalIgnoreCase) ||
                genre.Name.Contains("PvP", StringComparison.OrdinalIgnoreCase)));

        return competitiveGames * 0.2f;
    }

    private float CalculateStorySeekerScore(IReadOnlyList<Game> games, IReadOnlyList<GameSession> sessions)
    {
        var storyGames = games.Count(g =>
            g.Genres.Any(genre =>
                genre.Name.Contains("Story Rich", StringComparison.OrdinalIgnoreCase) ||
                genre.Name.Contains("Visual Novel", StringComparison.OrdinalIgnoreCase) ||
                genre.Name.Contains("Narrative", StringComparison.OrdinalIgnoreCase)));

        return storyGames * 0.15f;
    }

    private float CalculateStrategistScore(IReadOnlyList<Game> games, IReadOnlyList<GameSession> sessions)
    {
        var strategyGames = games.Count(g =>
            g.Genres.Any(genre =>
                genre.Name.Contains("Strategy", StringComparison.OrdinalIgnoreCase) ||
                genre.Name.Contains("Tactical", StringComparison.OrdinalIgnoreCase) ||
                genre.Name.Contains("Turn-Based", StringComparison.OrdinalIgnoreCase)));

        return strategyGames * 0.15f;
    }

    private float CalculateSpeedrunnerScore(IReadOnlyList<Game> games, IReadOnlyList<GameSession> sessions)
    {
        var avgSessionTime = sessions.Any() ? sessions.Average(s => s.Duration.TotalMinutes) : 0;
        var speedrunGames = games.Count(g =>
            g.Tags.Any(t => t.Contains("Speedrun", StringComparison.OrdinalIgnoreCase)));

        return speedrunGames * 0.3f + (avgSessionTime < 30 ? 0.2f : 0f);
    }

    private float CalculateSocialiteScore(IReadOnlyList<Game> games, IReadOnlyList<GameSession> sessions)
    {
        // Since Game doesn't have MaxPlayers, we rely on genre tags
        var multiplayerGames = games.Count(g =>
            g.Genres.Any(genre => genre.Name.Contains("Multiplayer", StringComparison.OrdinalIgnoreCase)));

        return multiplayerGames * 0.15f;
    }

    private float CalculateCollectorScore(IReadOnlyList<Game> games)
    {
        var totalGames = games.Count;
        return totalGames * 0.02f;
    }

    private float CalculateCasualScore(IReadOnlyList<GameSession> sessions)
    {
        if (!sessions.Any()) return 0;

        var avgSessionLength = sessions.Average(s => s.Duration.TotalMinutes);
        var shortSessionRatio = sessions.Count(s => s.Duration < TimeSpan.FromMinutes(30)) / (float)sessions.Count;

        return shortSessionRatio * 0.5f + (avgSessionLength < 60 ? 0.3f : 0f);
    }

    private float CalculateHardcoreScore(IReadOnlyList<Game> games, IReadOnlyList<GameSession> sessions)
    {
        var hardGames = games.Count(g =>
            g.Tags.Any(t =>
                t.Contains("Hard", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("Souls-like", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("Permadeath", StringComparison.OrdinalIgnoreCase)));

        var totalHours = sessions.Sum(s => s.Duration.TotalHours);

        return hardGames * 0.2f + (float)(totalHours * 0.001);
    }

    private List<GenrePreference> CalculateGenrePreferences(
        IReadOnlyList<Game> games,
        IReadOnlyList<GameSession> sessions)
    {
        var genreStats = new Dictionary<string, (int hours, int games, SaveState.Core.Analytics.Models.GamerProfile.TrendDirection trend)>();

        foreach (var genre in games.SelectMany(g => g.Genres).Select(g => g.Name).Distinct())
        {
            var genreGames = games.Where(g => g.Genres.Any(gr => gr.Name == genre)).ToList();
            var genreSessions = sessions.Where(s =>
                s.Game != null && s.Game.Genres.Any(gr => gr.Name == genre)).ToList();
            var hours = (int)genreSessions.Sum(s => s.Duration.TotalHours);

            // Determine trend by comparing recent vs older sessions
            var recentThreshold = _timeProvider.UtcNow.AddMonths(-3);
            var recentHours = genreSessions
                .Where(s => s.StartTime > recentThreshold)
                .Sum(s => s.Duration.TotalHours);
            var olderHours = genreSessions
                .Where(s => s.StartTime <= recentThreshold)
                .Sum(s => s.Duration.TotalHours);

            var trend = recentHours > olderHours * 1.2 ? SaveState.Core.Analytics.Models.GamerProfile.TrendDirection.Rising :
                       recentHours < olderHours * 0.8 ? SaveState.Core.Analytics.Models.GamerProfile.TrendDirection.Declining :
                       SaveState.Core.Analytics.Models.GamerProfile.TrendDirection.Stable;

            genreStats[genre] = (hours, genreGames.Count, trend);
        }

        var maxHours = genreStats.Values.Any() ? genreStats.Values.Max(s => s.hours) : 0;

        return genreStats.Select(kvp => new GenrePreference
        {
            Genre = kvp.Key,
            HoursPlayed = kvp.Value.hours,
            GamesPlayed = kvp.Value.games,
            PreferenceScore = maxHours > 0 ? kvp.Value.hours / (float)maxHours : 0,
            Trend = kvp.Value.trend
        }).OrderByDescending(g => g.PreferenceScore).ToList();
    }

    private List<PlatformPreference> CalculatePlatformPreferences(
        IReadOnlyList<Game> games,
        IReadOnlyList<GameSession> sessions)
    {
        var platformGroups = games
            .Where(g => g.Platform != null)
            .GroupBy(g => g.Platform!.Name)
            .ToList();

        var platformStats = platformGroups
            .Select(g => new PlatformPreference
            {
                Platform = g.Key,
                GamesOwned = g.Count(),
                HoursPlayed = (int)sessions
                    .Where(s => s.Game?.Platform?.Name == g.Key)
                    .Sum(s => s.Duration.TotalHours),
                PreferenceScore = 0 // Will be calculated
            }).ToList();

        var maxHours = platformStats.Any() ? platformStats.Max(p => p.HoursPlayed) : 0;
        return platformStats.Select(p => p with
        {
            PreferenceScore = maxHours > 0 ? p.HoursPlayed / (float)maxHours : 0
        }).ToList();
    }

    private PlaystyleMetrics CalculatePlaystyleMetrics(
        IReadOnlyList<Game> games,
        IReadOnlyList<GameSession> sessions,
        IReadOnlyList<UserAchievement> achievements)
    {
        var totalHours = sessions.Sum(s => s.Duration.TotalHours);
        var avgSession = sessions.Any()
            ? TimeSpan.FromMinutes(sessions.Average(s => s.Duration.TotalMinutes))
            : TimeSpan.Zero;

        var completedGames = games.Count(g => g.IsCompleted);
        var achievementRate = games.Any()
            ? achievements.Count(a => a.IsUnlocked) / (double)games.Count
            : 0;

        var mostActiveDay = sessions.GroupBy(s => s.StartTime.DayOfWeek)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? DayOfWeek.Saturday;

        var mostActiveHour = sessions.GroupBy(s => s.StartTime.Hour)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? 20;

        TimeOfDay mostActiveTime = mostActiveHour switch
        {
            >= 6 and < 12 => TimeOfDay.Morning,
            >= 12 and < 17 => TimeOfDay.Afternoon,
            >= 17 and < 22 => TimeOfDay.Evening,
            _ => TimeOfDay.Night
        };

        return new PlaystyleMetrics
        {
            AverageSessionLength = avgSession,
            AverageTimeToComplete = TimeSpan.FromHours(25), // Placeholder
            CompletionRate = completedGames / (float)Math.Max(games.Count, 1),
            AchievementHunterScore = (float)achievementRate,
            ReplayabilityScore = CalculateReplayabilityScore(sessions),
            MostActiveDay = mostActiveDay,
            MostActiveTime = mostActiveTime,
            TotalGamesOwned = games.Count,
            TotalGamesCompleted = completedGames,
            TotalAchievementsUnlocked = achievements.Count(a => a.IsUnlocked),
            TotalPlaytimeHours = (float)totalHours
        };
    }

    private float CalculateReplayabilityScore(IReadOnlyList<GameSession> sessions)
    {
        var gameSessionCounts = sessions
            .GroupBy(s => s.GameId)
            .Select(g => g.Count())
            .ToList();

        if (!gameSessionCounts.Any()) return 0;

        var replayedGames = gameSessionCounts.Count(c => c > 1);
        return replayedGames / (float)gameSessionCounts.Count;
    }

    private List<DnaEvolutionSnapshot> GenerateMockEvolutionHistory(int months)
    {
        var snapshots = new List<DnaEvolutionSnapshot>();
        var random = new Random();
        var archetypes = Enum.GetValues<GamerArchetype>();

        for (int i = months; i >= 0; i--)
        {
            var timestamp = _timeProvider.UtcNow.AddMonths(-i);
            var randomArchetype = archetypes[random.Next(archetypes.Length)];

            snapshots.Add(new DnaEvolutionSnapshot
            {
                Timestamp = timestamp,
                DominantArchetype = randomArchetype,
                TopGenres = new Dictionary<string, float>
                {
                    ["RPG"] = random.NextSingle(),
                    ["Action"] = random.NextSingle(),
                    ["Strategy"] = random.NextSingle()
                }
            });
        }

        return snapshots;
    }

    private static string GenerateShareCode(Guid userId)
    {
        // Generate a short, shareable code
        var bytes = userId.ToByteArray();
        var base64 = Convert.ToBase64String(bytes);
        return base64.Replace("+", "-").Replace("/", "_")[..8];
    }

    #endregion
}
