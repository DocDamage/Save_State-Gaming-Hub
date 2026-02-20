namespace SaveState.Application.GameLibrary.Services;

using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.Common.Services;
using SaveState.Core.Common.ValueObjects;
using System.Text.Json;

/// <summary>
/// Implementation of the achievement service for managing user achievements.
/// </summary>
public class AchievementService : IAchievementService
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IGameSessionRepository _sessionRepository;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the AchievementService.
    /// </summary>
    /// <param name="achievementRepository">The achievement repository.</param>
    /// <param name="gameRepository">The game repository.</param>
    /// <param name="sessionRepository">The session repository.</param>
    /// <param name="timeProvider">The time provider.</param>
    public AchievementService(
        IAchievementRepository achievementRepository,
        IGameRepository gameRepository,
        IGameSessionRepository sessionRepository,
        ITimeProvider timeProvider)
    {
        _achievementRepository = achievementRepository;
        _gameRepository = gameRepository;
        _sessionRepository = sessionRepository;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Checks if a user has unlocked any achievements based on current progress.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of newly unlocked achievements.</returns>
    public async Task<IReadOnlyList<Achievement>> CheckForUnlockedAchievementsAsync(Guid userId, CancellationToken ct = default)
    {
        var achievements = await _achievementRepository.GetActiveAchievementsAsync(ct);
        var userAchievements = await _achievementRepository.GetUserAchievementsAsync(userId, ct);
        var newlyUnlocked = new List<Achievement>();
        var userAchievementLookup = userAchievements.ToDictionary(ua => ua.AchievementId, ua => ua);

        foreach (var achievement in achievements)
        {
            userAchievementLookup.TryGetValue(achievement.Id, out var userAchievement);

            var criteriaResult = ParseCriteria(achievement.Criteria, achievement.GameId);
            var criteria = criteriaResult.IsSuccess ? criteriaResult.Value : null;
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

    /// <summary>
    /// Updates user progress for a specific achievement type.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="achievementType">The type of achievement to update.</param>
    /// <param name="progressIncrement">The amount to increment progress by.</param>
    /// <param name="metadata">Optional metadata for the progress update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated user achievements.</returns>
    public async Task<IReadOnlyList<UserAchievement>> UpdateProgressAsync(
        Guid userId,
        AchievementType achievementType,
        int progressIncrement,
        string? metadata = null,
        CancellationToken ct = default)
    {
        var achievements = await _achievementRepository.GetAchievementsByTypeAsync(achievementType, ct);
        var updatedAchievements = new List<UserAchievement>();

        foreach (var achievement in achievements)
        {
            var userAchievement = await _achievementRepository.GetUserAchievementAsync(userId, achievement.Id, ct);

            if (userAchievement == null)
            {
                var criteriaResult = ParseCriteria(achievement.Criteria, achievement.GameId);
                var criteria = criteriaResult.IsSuccess ? criteriaResult.Value : null;
                var targetProgress = DetermineTargetProgress(achievement, criteria);

                userAchievement = new UserAchievement(userId, achievement.Id, targetProgress);
                await _achievementRepository.AddOrUpdateUserAchievementAsync(userAchievement, ct);
            }

            if (!userAchievement.IsUnlocked)
            {
                userAchievement.AddProgress(progressIncrement, metadata);
                await _achievementRepository.UpdateUserAchievementAsync(userAchievement, ct);
                updatedAchievements.Add(userAchievement);
            }
        }

        return updatedAchievements;
    }

    /// <summary>
    /// Manually awards an achievement to a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="achievementId">The achievement ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the achievement was awarded, false if already unlocked.</returns>
    public async Task<bool> AwardAchievementAsync(Guid userId, Guid achievementId, CancellationToken ct = default)
    {
        var userAchievement = await _achievementRepository.GetUserAchievementAsync(userId, achievementId, ct);

        if (userAchievement == null)
        {
            var achievement = await _achievementRepository.GetAchievementByIdAsync(achievementId, ct);
            if (achievement == null)
            {
                return false;
            }

            userAchievement = new UserAchievement(userId, achievementId, 1);
            await _achievementRepository.AddOrUpdateUserAchievementAsync(userAchievement, ct);
        }

        if (!userAchievement.IsUnlocked)
        {
            userAchievement.Unlock();
            await _achievementRepository.UpdateUserAchievementAsync(userAchievement, ct);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets all achievements that a user has unlocked.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of unlocked achievements.</returns>
    public async Task<Result<IReadOnlyList<Achievement>>> GetUnlockedAchievementsAsync(Guid userId, CancellationToken ct = default)
    {
        var userAchievements = await _achievementRepository.GetUserAchievementsAsync(userId, ct);

        var unlocked = userAchievements
            .Where(ua => ua.IsUnlocked && ua.Achievement != null)
            .Select(ua => ua.Achievement!)
            .ToList();

        return Result.Success<IReadOnlyList<Achievement>>(unlocked);
    }

    /// <summary>
    /// Gets user achievement progress including locked achievements.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of user achievement progress.</returns>
    public async Task<Result<IReadOnlyList<UserAchievement>>> GetUserProgressAsync(Guid userId, CancellationToken ct = default)
    {
        var progress = await _achievementRepository.GetUserAchievementsAsync(userId, ct);
        return Result.Success<IReadOnlyList<UserAchievement>>(progress);
    }

    /// <summary>
    /// Resets all user achievement progress (for testing/debugging).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task ResetUserProgressAsync(Guid userId, CancellationToken ct = default)
    {
        var userAchievements = await _achievementRepository.GetUserAchievementsAsync(userId, ct);

        foreach (var userAchievement in userAchievements)
        {
            userAchievement.ResetProgress();
            await _achievementRepository.UpdateUserAchievementAsync(userAchievement, ct);
        }
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

    private static Result<AchievementCriteria> ParseCriteria(string? criteriaJson, Guid? fallbackGameId)
    {
        if (string.IsNullOrWhiteSpace(criteriaJson))
        {
            return Result.Failure<AchievementCriteria>("Criteria JSON is empty");
        }

        try
        {
            using var document = JsonDocument.Parse(criteriaJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Result.Failure<AchievementCriteria>("Criteria JSON is not an object");
            }

            var root = document.RootElement;

            var gameId = TryGetGuid(root, "gameId").Value
                ?? TryGetGuid(root, "GameId").Value
                ?? fallbackGameId;
            var minSessionCount = TryGetInt(root, "minSessions").Value
                ?? TryGetInt(root, "minSessionCount").Value;
            var minSessionsInLastDays = TryGetInt(root, "minSessionsInLastDays").Value
                ?? TryGetInt(root, "minSessionsLastDays").Value;
            var minTotalPlaytimeMinutes = TryGetInt(root, "minTotalPlaytimeMinutes").Value
                ?? TryGetInt(root, "minPlaytimeMinutes").Value;
            var minTotalPlaytimeHours = TryGetInt(root, "minTotalPlaytimeHours").Value
                ?? TryGetInt(root, "minPlaytimeHours").Value;
            var minDistinctGamesPlayed = TryGetInt(root, "minDistinctGamesPlayed").Value;
            var minCompletedGames = TryGetInt(root, "minCompletedGames").Value;
            var requireCompletion = TryGetBool(root, "requireCompletion").Value
                ?? TryGetBool(root, "requiresCompletion").Value;

            return Result.Success(new AchievementCriteria(
                gameId,
                minSessionCount,
                minSessionsInLastDays,
                minTotalPlaytimeMinutes,
                minTotalPlaytimeHours,
                minDistinctGamesPlayed,
                minCompletedGames,
                requireCompletion));
        }
        catch (JsonException ex)
        {
            return Result.Failure<AchievementCriteria>($"Failed to parse criteria: {ex.Message}");
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
            var cutoff = _timeProvider.UtcNow.AddDays(-recentDays.Value);
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

    private static Result<int?> TryGetInt(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var element))
        {
            return Result.Success<int?>(null);
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value))
        {
            return Result.Success<int?>(value);
        }

        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out value))
        {
            return Result.Success<int?>(value);
        }

        return Result.Success<int?>(null);
    }

    private static Result<bool?> TryGetBool(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var element))
        {
            return Result.Success<bool?>(null);
        }

        return element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False
            ? Result.Success<bool?>(element.GetBoolean())
            : Result.Success<bool?>(null);
    }

    private static Result<Guid?> TryGetGuid(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var element))
        {
            return Result.Success<Guid?>(null);
        }

        if (element.ValueKind == JsonValueKind.String && Guid.TryParse(element.GetString(), out var value))
        {
            return Result.Success<Guid?>(value);
        }

        return Result.Success<Guid?>(null);
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
