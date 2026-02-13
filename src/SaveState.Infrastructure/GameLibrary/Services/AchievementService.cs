using Microsoft.Extensions.Logging;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;
using System.Text.Json;

namespace SaveState.Infrastructure.GameLibrary.Services;

public class AchievementService : IAchievementService
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IGameSessionRepository _sessionRepository;
    private readonly ILogger<AchievementService> _logger;

    public AchievementService(
        IAchievementRepository achievementRepository,
        IGameRepository gameRepository,
        IGameSessionRepository sessionRepository,
        ILogger<AchievementService> logger)
    {
        _achievementRepository = achievementRepository;
        _gameRepository = gameRepository;
        _sessionRepository = sessionRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Achievement>> CheckForUnlockedAchievementsAsync(Guid userId, CancellationToken ct = default)
    {
        var achievements = await _achievementRepository.GetActiveAchievementsAsync(ct);
        var userAchievements = await _achievementRepository.GetUserAchievementsAsync(userId, ct);
        var newlyUnlocked = new List<Achievement>();
        var userAchievementLookup = userAchievements.ToDictionary(ua => ua.AchievementId, ua => ua);

        foreach (var achievement in achievements)
        {
            userAchievementLookup.TryGetValue(achievement.Id, out var userAchievement);

            var criteria = ParseCriteria(achievement.Criteria, achievement.GameId);
            var targetProgress = userAchievement?.TargetProgress ?? DetermineTargetProgress(achievement, criteria);
            var scopeGameId = criteria?.GameId ?? achievement.GameId;

            var metrics = await GetSessionMetricsAsync(scopeGameId, criteria?.MinSessionsInLastDays, ct);
            var completedGamesCount = await GetCompletedGamesCountAsync(scopeGameId, criteria, achievement, ct);

            var progress = DetermineProgress(achievement, criteria, metrics, completedGamesCount);
            var criteriaSatisfied = EvaluateCriteria(criteria, metrics, completedGamesCount);

            if (userAchievement == null)
            {
                userAchievement = new UserAchievement(userId, achievement.Id, targetProgress);
            }

            var wasUnlocked = userAchievement.IsUnlocked;

            if (criteria != null && !criteriaSatisfied && progress >= targetProgress)
            {
                progress = Math.Max(0, targetProgress - 1);
            }

            if (progress != userAchievement.CurrentProgress)
            {
                userAchievement.UpdateProgress(progress);
            }

            if (criteria != null && criteriaSatisfied && progress >= targetProgress && !userAchievement.IsUnlocked)
            {
                userAchievement.Unlock();
            }

            await _achievementRepository.AddOrUpdateUserAchievementAsync(userAchievement, ct);

            if (!wasUnlocked && userAchievement.IsUnlocked)
            {
                newlyUnlocked.Add(userAchievement.Achievement ?? achievement);
            }
        }

        return newlyUnlocked;
    }

    public async Task<IReadOnlyList<UserAchievement>> UpdateProgressAsync(Guid userId, AchievementType achievementType, int progressIncrement, string? metadata = null, CancellationToken ct = default)
    {
        var achievements = await _achievementRepository.GetAchievementsByTypeAsync(achievementType, ct);
        var updatedAchievements = new List<UserAchievement>();

        foreach (var achievement in achievements)
        {
            var userAchievement = await _achievementRepository.GetUserAchievementAsync(userId, achievement.Id, ct);

            if (userAchievement != null && userAchievement.IsUnlocked) continue;

            if (userAchievement == null)
            {
                var criteria = ParseCriteria(achievement.Criteria, achievement.GameId);
                var targetProgress = DetermineTargetProgress(achievement, criteria);
                userAchievement = new UserAchievement(userId, achievement.Id, targetProgress);
            }

            userAchievement.AddProgress(progressIncrement, metadata);
            await _achievementRepository.AddOrUpdateUserAchievementAsync(userAchievement, ct);
            updatedAchievements.Add(userAchievement);
        }

        return updatedAchievements;
    }

    public async Task<bool> AwardAchievementAsync(Guid userId, Guid achievementId, CancellationToken ct = default)
    {
        var achievement = await _achievementRepository.GetAchievementByIdAsync(achievementId, ct);
        if (achievement == null) return false;

        var userAchievement = await _achievementRepository.GetUserAchievementAsync(userId, achievementId, ct);
        if (userAchievement != null && userAchievement.IsUnlocked) return false;

        if (userAchievement == null)
        {
            userAchievement = new UserAchievement(userId, achievementId, achievement.TargetValue);
        }

        userAchievement.Unlock();
        await _achievementRepository.AddOrUpdateUserAchievementAsync(userAchievement, ct);
        return true;
    }

    public async Task<IReadOnlyList<Achievement>> GetUnlockedAchievementsAsync(Guid userId, CancellationToken ct = default)
    {
        var userAchievements = await _achievementRepository.GetUserAchievementsAsync(userId, ct);
        return userAchievements.Where(ua => ua.IsUnlocked && ua.Achievement != null).Select(ua => ua.Achievement!).ToList();
    }

    public async Task<IReadOnlyList<UserAchievement>> GetUserProgressAsync(Guid userId, CancellationToken ct = default)
    {
        return await _achievementRepository.GetUserAchievementsAsync(userId, ct);
    }

    public async Task ResetUserProgressAsync(Guid userId, CancellationToken ct = default)
    {
        // Not implemented in repository yet (Reset/Delete), so skipping for now or would loop delete
        _logger.LogWarning("ResetUserProgressAsync not implemented");
        await Task.CompletedTask;
    }

    private sealed record AchievementCriteria(
        Guid? GameId,
        int? MinSessionCount,
        int? MinSessionsInLastDays,
        int? MinTotalPlaytimeMinutes,
        int? MinTotalPlaytimeHours,
        int? MinDistinctGamesPlayed,
        int? MinCompletedGames,
        bool? RequireCompletion);

    private sealed record SessionMetrics(
        int SessionCount,
        TimeSpan TotalPlaytime,
        int SessionsInLastDays,
        int DistinctGamesPlayed);

    private AchievementCriteria? ParseCriteria(string? criteriaJson, Guid? fallbackGameId)
    {
        if (string.IsNullOrWhiteSpace(criteriaJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(criteriaJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var root = document.RootElement;

            var gameId = TryGetGuid(root, "gameId") ?? TryGetGuid(root, "GameId") ?? fallbackGameId;
            var minSessionCount = TryGetInt(root, "minSessions") ?? TryGetInt(root, "minSessionCount");
            var minSessionsInLastDays = TryGetInt(root, "minSessionsInLastDays") ?? TryGetInt(root, "minSessionsLastDays");
            var minTotalPlaytimeMinutes = TryGetInt(root, "minTotalPlaytimeMinutes") ?? TryGetInt(root, "minPlaytimeMinutes");
            var minTotalPlaytimeHours = TryGetInt(root, "minTotalPlaytimeHours") ?? TryGetInt(root, "minPlaytimeHours");
            var minDistinctGamesPlayed = TryGetInt(root, "minDistinctGamesPlayed");
            var minCompletedGames = TryGetInt(root, "minCompletedGames");
            var requireCompletion = TryGetBool(root, "requireCompletion") ?? TryGetBool(root, "requiresCompletion");

            return new AchievementCriteria(
                gameId,
                minSessionCount,
                minSessionsInLastDays,
                minTotalPlaytimeMinutes,
                minTotalPlaytimeHours,
                minDistinctGamesPlayed,
                minCompletedGames,
                requireCompletion);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse achievement criteria");
            return null;
        }
    }

    private static int DetermineTargetProgress(Achievement achievement, AchievementCriteria? criteria)
    {
        if (criteria != null)
        {
            if (criteria.MinTotalPlaytimeMinutes.HasValue)
            {
                return criteria.MinTotalPlaytimeMinutes.Value;
            }

            if (criteria.MinTotalPlaytimeHours.HasValue)
            {
                return criteria.MinTotalPlaytimeHours.Value;
            }

            if (criteria.MinSessionCount.HasValue)
            {
                return criteria.MinSessionCount.Value;
            }

            if (criteria.MinSessionsInLastDays.HasValue)
            {
                return criteria.MinSessionsInLastDays.Value;
            }

            if (criteria.MinDistinctGamesPlayed.HasValue)
            {
                return criteria.MinDistinctGamesPlayed.Value;
            }

            if (criteria.MinCompletedGames.HasValue)
            {
                return criteria.MinCompletedGames.Value;
            }

            if (criteria.RequireCompletion.HasValue)
            {
                return 1;
            }
        }

        return achievement.TargetValue;
    }

    private static int DetermineProgress(
        Achievement achievement,
        AchievementCriteria? criteria,
        SessionMetrics metrics,
        int completedGamesCount)
    {
        if (criteria != null)
        {
            if (criteria.MinTotalPlaytimeMinutes.HasValue)
            {
                return (int)Math.Floor(metrics.TotalPlaytime.TotalMinutes);
            }

            if (criteria.MinTotalPlaytimeHours.HasValue)
            {
                return (int)Math.Floor(metrics.TotalPlaytime.TotalHours);
            }

            if (criteria.MinSessionCount.HasValue)
            {
                return metrics.SessionCount;
            }

            if (criteria.MinSessionsInLastDays.HasValue)
            {
                return metrics.SessionsInLastDays;
            }

            if (criteria.MinDistinctGamesPlayed.HasValue)
            {
                return metrics.DistinctGamesPlayed;
            }

            if (criteria.MinCompletedGames.HasValue || criteria.RequireCompletion.HasValue)
            {
                return completedGamesCount;
            }
        }

        return achievement.Type switch
        {
            AchievementType.GameCompletion => completedGamesCount,
            AchievementType.PlayTime => (int)Math.Floor(metrics.TotalPlaytime.TotalHours),
            AchievementType.Collection => metrics.DistinctGamesPlayed,
            AchievementType.Social => metrics.SessionCount,
            AchievementType.Special => 0,
            _ => 0
        };
    }

    private static bool EvaluateCriteria(
        AchievementCriteria? criteria,
        SessionMetrics metrics,
        int completedGamesCount)
    {
        if (criteria == null)
        {
            return true;
        }

        if (criteria.MinSessionCount.HasValue && metrics.SessionCount < criteria.MinSessionCount.Value)
        {
            return false;
        }

        if (criteria.MinSessionsInLastDays.HasValue && metrics.SessionsInLastDays < criteria.MinSessionsInLastDays.Value)
        {
            return false;
        }

        if (criteria.MinTotalPlaytimeMinutes.HasValue &&
            metrics.TotalPlaytime.TotalMinutes < criteria.MinTotalPlaytimeMinutes.Value)
        {
            return false;
        }

        if (criteria.MinTotalPlaytimeHours.HasValue &&
            metrics.TotalPlaytime.TotalHours < criteria.MinTotalPlaytimeHours.Value)
        {
            return false;
        }

        if (criteria.MinDistinctGamesPlayed.HasValue && metrics.DistinctGamesPlayed < criteria.MinDistinctGamesPlayed.Value)
        {
            return false;
        }

        if (criteria.MinCompletedGames.HasValue && completedGamesCount < criteria.MinCompletedGames.Value)
        {
            return false;
        }

        if (criteria.RequireCompletion.HasValue && criteria.RequireCompletion.Value && completedGamesCount < 1)
        {
            return false;
        }

        return true;
    }

    private async Task<SessionMetrics> GetSessionMetricsAsync(Guid? gameId, int? recentDays, CancellationToken ct)
    {
        IReadOnlyList<GameSession> sessions;
        if (gameId.HasValue)
        {
            sessions = await _sessionRepository.GetByGameIdAsync(gameId.Value, int.MaxValue, ct);
        }
        else
        {
            sessions = await _sessionRepository.GetAllAsync(ct);
        }

        var totalPlaytime = TimeSpan.Zero;
        foreach (var session in sessions)
        {
            totalPlaytime += session.Duration;
        }

        var sessionCount = sessions.Count;
        var distinctGames = sessions.Select(s => s.GameId).Distinct().Count();

        var sessionsInLastDays = 0;
        if (recentDays.HasValue && recentDays.Value > 0)
        {
            var cutoff = DateTime.UtcNow.AddDays(-recentDays.Value);
            sessionsInLastDays = sessions.Count(s => s.StartedAt >= cutoff);
        }

        return new SessionMetrics(sessionCount, totalPlaytime, sessionsInLastDays, distinctGames);
    }

    private async Task<int> GetCompletedGamesCountAsync(
        Guid? gameId,
        AchievementCriteria? criteria,
        Achievement achievement,
        CancellationToken ct)
    {
        if ((criteria?.MinCompletedGames.HasValue != true) &&
            (criteria?.RequireCompletion.HasValue != true) &&
            achievement.Type != AchievementType.GameCompletion)
        {
            return 0;
        }

        if (gameId.HasValue)
        {
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId.Value), ct);
            return game?.IsCompleted == true ? 1 : 0;
        }

        var games = await _gameRepository.GetAllAsync(ct);
        return games.Count(game => game.IsCompleted);
    }

    private static int? TryGetInt(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var element))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value))
        {
            return value;
        }

        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out value))
        {
            return value;
        }

        return null;
    }

    private static bool? TryGetBool(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var element))
        {
            return null;
        }

        return element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False
            ? element.GetBoolean()
            : null;
    }

    private static Guid? TryGetGuid(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var element))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.String && Guid.TryParse(element.GetString(), out var value))
        {
            return value;
        }

        return null;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
