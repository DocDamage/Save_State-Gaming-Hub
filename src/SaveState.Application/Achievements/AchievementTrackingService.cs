// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.Logging;
using SaveState.Application.Common.Events;
using SaveState.Core.Achievements;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Application.Achievements;

/// <summary>
/// Service for tracking and managing user achievements.
/// </summary>
public sealed class AchievementTrackingService : IAchievementTrackingService
{
    private readonly ILogger<AchievementTrackingService> _logger;
    private readonly IAchievementRepository _achievementRepository;
    private readonly IRetroAchievementsClient _retroAchievementsClient;
    private readonly ICacheService _cache;
    private readonly ITimeProvider _timeProvider;
    private readonly IEventPublisher _eventPublisher;

    public AchievementTrackingService(
        ILogger<AchievementTrackingService> logger,
        IAchievementRepository achievementRepository,
        IRetroAchievementsClient retroAchievementsClient,
        ICacheService cache,
        ITimeProvider timeProvider,
        IEventPublisher eventPublisher)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _achievementRepository = achievementRepository ?? throw new ArgumentNullException(nameof(achievementRepository));
        _retroAchievementsClient = retroAchievementsClient ?? throw new ArgumentNullException(nameof(retroAchievementsClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UserAchievementProgress>>> GetUserAchievementsAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var cacheKey = $"achievements_user_{userId}";
            if (_cache.TryGetValue<IReadOnlyList<UserAchievementProgress>>(cacheKey, out var cached))
            {
                if (cached is not null)
                {
                    return Result.Success(cached);
                }
            }

            var achievements = await _achievementRepository.GetUserAchievementsAsync(userId, ct);

            _cache.Set(cacheKey, achievements, TimeSpan.FromMinutes(5));
            return Result.Success<IReadOnlyList<UserAchievementProgress>>(achievements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user achievements for {UserId}", userId);
            return Result.Failure<IReadOnlyList<UserAchievementProgress>>(
                $"Failed to get user achievements for {userId}: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UserAchievementProgress>>> GetGameAchievementsAsync(Guid userId, Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var achievements = await _achievementRepository.GetGameAchievementsAsync(userId, gameId, ct);
            return Result.Success<IReadOnlyList<UserAchievementProgress>>(achievements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get game achievements for user {UserId}, game {GameId}", userId, gameId);
            return Result.Failure<IReadOnlyList<UserAchievementProgress>>(
                $"Failed to get game achievements for user {userId}, game {gameId}: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UserAchievementProgress>>> GetRecentAchievementsAsync(Guid userId, int count = 10, CancellationToken ct = default)
    {
        try
        {
            var achievements = await _achievementRepository.GetRecentAchievementsAsync(userId, count, ct);
            return Result.Success<IReadOnlyList<UserAchievementProgress>>(achievements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent achievements for {UserId}", userId);
            return Result.Failure<IReadOnlyList<UserAchievementProgress>>(
                $"Failed to get recent achievements for {userId}: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<AchievementStatistics>> GetStatisticsAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var cacheKey = $"achievement_stats_{userId}";
            if (_cache.TryGetValue<AchievementStatistics>(cacheKey, out var cached))
            {
                if (cached is not null)
                {
                    return Result.Success(cached);
                }
            }

            var achievements = await _achievementRepository.GetUserAchievementsAsync(userId, ct);
            var unlocked = achievements.Where(a => a.IsUnlocked).ToList();

            var stats = new AchievementStatistics
            {
                TotalAchievements = achievements.Count,
                UnlockedCount = unlocked.Count,
                TotalPoints = unlocked.Sum(a => a.Points),
                MaxPoints = achievements.Sum(a => a.Points),
                RareAchievementsCount = unlocked.Count(a => a.IsRare),
                AchievementsToday = unlocked.Count(a => a.UnlockedAt?.Date == _timeProvider.UtcNow.Date),
                AchievementsThisMonth = unlocked.Count(a => a.UnlockedAt?.Month == _timeProvider.UtcNow.Month && 
                                                           a.UnlockedAt?.Year == _timeProvider.UtcNow.Year),
                ByPlatform = achievements.GroupBy(a => a.Platform)
                    .ToDictionary(g => g.Key, g => new PlatformStats
                    {
                        Unlocked = g.Count(a => a.IsUnlocked),
                        Total = g.Count(),
                        Points = g.Where(a => a.IsUnlocked).Sum(a => a.Points)
                    }),
                ByType = achievements.GroupBy(a => a.Type)
                    .ToDictionary(g => g.Key, g => g.Count(a => a.IsUnlocked))
            };

            // Calculate streak
            stats.CurrentStreak = CalculateStreak(unlocked);
            stats.LongestStreak = CalculateLongestStreak(unlocked);

            _cache.Set(cacheKey, stats, TimeSpan.FromMinutes(5));
            return Result.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get achievement statistics for {UserId}", userId);
            return Result.Failure<AchievementStatistics>(
                $"Failed to get achievement statistics for {userId}: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task TrackProgressAsync(Guid userId, string achievementKey, int progress, CancellationToken ct = default)
    {
        try
        {
            var achievement = await _achievementRepository.GetUserAchievementAsync(userId, achievementKey, ct);
            if (achievement == null || achievement.IsUnlocked)
                return;

            var newProgress = Math.Min(progress, achievement.TargetValue);
            await _achievementRepository.UpdateProgressAsync(userId, achievementKey, newProgress, ct);

            // Check if unlocked
            if (newProgress >= achievement.TargetValue)
            {
                await UnlockAchievementAsync(userId, achievementKey, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track achievement progress");
        }
    }

    /// <inheritdoc />
    public async Task UnlockAchievementAsync(Guid userId, string achievementKey, CancellationToken ct = default)
    {
        try
        {
            var achievement = await _achievementRepository.GetUserAchievementAsync(userId, achievementKey, ct);
            if (achievement == null || achievement.IsUnlocked)
                return;

            await _achievementRepository.UnlockAchievementAsync(userId, achievementKey, _timeProvider.UtcNow, ct);

            // Publish event
            var evt = new AchievementUnlockedEvent
            {
                OccurredOn = _timeProvider.UtcNow,
                UserId = userId,
                AchievementName = achievement.Name,
                GameName = achievement.GameName,
                Points = achievement.Points,
                IsRare = achievement.IsRare,
                UnlockedAt = _timeProvider.UtcNow
            };
            await _eventPublisher.PublishAsync(evt, ct);

            // Invalidate cache
            _cache.Remove($"achievements_user_{userId}");
            _cache.Remove($"achievement_stats_{userId}");

            _logger.LogInformation("Achievement unlocked: {Achievement} for user {UserId}", achievement.Name, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unlock achievement");
        }
    }

    /// <inheritdoc />
    public async Task SyncExternalAchievementsAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Syncing external achievements for user {UserId}", userId);

            // Sync RetroAchievements
            if (_retroAchievementsClient.IsAuthenticated)
            {
                var recentAchievements = await _retroAchievementsClient.GetRecentAchievementsAsync(50, ct);
                if (recentAchievements.IsSuccess)
                {
                    // Map and sync achievements
                    _logger.LogInformation("Synced {Count} RetroAchievements", recentAchievements.Value.Count);
                }
            }

            // Invalidate cache
            _cache.Remove($"achievements_user_{userId}");
            _cache.Remove($"achievement_stats_{userId}");

            _logger.LogInformation("External achievements synced for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync external achievements");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UserAchievementProgress>>> GetRareAchievementsAsync(Guid userId, double maxUnlockRate = 10.0, CancellationToken ct = default)
    {
        try
        {
            var achievements = await _achievementRepository.GetUserAchievementsAsync(userId, ct);
            var rareAchievements = achievements
                .Where(a => a.IsUnlocked && a.RarityPercent.HasValue && a.RarityPercent.Value < maxUnlockRate)
                .OrderBy(a => a.RarityPercent)
                .ToList();
            return Result.Success<IReadOnlyList<UserAchievementProgress>>(rareAchievements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get rare achievements");
            return Result.Failure<IReadOnlyList<UserAchievementProgress>>(
                $"Failed to get rare achievements for {userId}: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AchievementRecommendation>>> GetRecommendationsAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var achievements = await _achievementRepository.GetUserAchievementsAsync(userId, ct);
            var inProgress = achievements.Where(a => !a.IsUnlocked && a.CurrentProgress > 0).ToList();

            var recommendations = inProgress
                .Select(a => new AchievementRecommendation
                {
                    Achievement = a,
                    Reason = GetRecommendationReason(a),
                    Difficulty = EstimateDifficulty(a),
                    CompletionPercent = a.ProgressPercent,
                    PointsReward = a.Points
                })
                .OrderByDescending(r => r.CompletionPercent)
                .ThenByDescending(r => r.PointsReward)
                .Take(5)
                .ToList();

            return Result.Success<IReadOnlyList<AchievementRecommendation>>(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get achievement recommendations");
            return Result.Failure<IReadOnlyList<AchievementRecommendation>>(
                $"Failed to get achievement recommendations for {userId}: {ex.Message}",
                ErrorType.Internal);
        }
    }

    private int CalculateStreak(List<UserAchievementProgress> unlocked)
    {
        if (!unlocked.Any()) return 0;

        var today = _timeProvider.UtcNow.Date;
        var dates = unlocked
            .Where(a => a.UnlockedAt.HasValue)
            .Select(a => a.UnlockedAt!.Value.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        if (!dates.Any() || dates.First() < today.AddDays(-1))
            return 0;

        int streak = 1;
        for (int i = 1; i < dates.Count; i++)
        {
            if (dates[i] == dates[i - 1].AddDays(-1))
                streak++;
            else
                break;
        }

        return streak;
    }

    private int CalculateLongestStreak(List<UserAchievementProgress> unlocked)
    {
        if (!unlocked.Any()) return 0;

        var dates = unlocked
            .Where(a => a.UnlockedAt.HasValue)
            .Select(a => a.UnlockedAt!.Value.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        if (!dates.Any()) return 0;

        int maxStreak = 1;
        int currentStreak = 1;

        for (int i = 1; i < dates.Count; i++)
        {
            if (dates[i] == dates[i - 1].AddDays(1))
            {
                currentStreak++;
                maxStreak = Math.Max(maxStreak, currentStreak);
            }
            else
            {
                currentStreak = 1;
            }
        }

        return maxStreak;
    }

    private string GetRecommendationReason(UserAchievementProgress achievement)
    {
        if (achievement.ProgressPercent >= 75)
            return "Almost there! Just a bit more to unlock.";
        if (achievement.ProgressPercent >= 50)
            return "Good progress - keep it up!";
        if (achievement.IsRare)
            return "Rare achievement - worth the effort!";
        return "Started but not finished - give it another try!";
    }

    private int EstimateDifficulty(UserAchievementProgress achievement)
    {
        var difficulty = 5;
        
        if (achievement.RarityPercent.HasValue)
        {
            if (achievement.RarityPercent.Value < 1) difficulty += 3;
            else if (achievement.RarityPercent.Value < 5) difficulty += 2;
            else if (achievement.RarityPercent.Value < 10) difficulty += 1;
        }

        if (achievement.TargetValue > 100) difficulty += 2;
        else if (achievement.TargetValue > 50) difficulty += 1;

        return Math.Min(10, Math.Max(1, difficulty));
    }
}

/// <summary>
/// Repository interface for achievement data.
/// </summary>
public interface IAchievementRepository
{
    Task<IReadOnlyList<UserAchievementProgress>> GetUserAchievementsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserAchievementProgress>> GetGameAchievementsAsync(Guid userId, Guid gameId, CancellationToken ct = default);
    Task<IReadOnlyList<UserAchievementProgress>> GetRecentAchievementsAsync(Guid userId, int count, CancellationToken ct = default);
    Task<UserAchievementProgress?> GetUserAchievementAsync(Guid userId, string achievementKey, CancellationToken ct = default);
    Task UpdateProgressAsync(Guid userId, string achievementKey, int progress, CancellationToken ct = default);
    Task UnlockAchievementAsync(Guid userId, string achievementKey, DateTime unlockedAt, CancellationToken ct = default);
    Task<string?> GetRetroAchievementsUsernameAsync(Guid userId, CancellationToken ct = default);
    Task SyncRetroAchievementsAsync(Guid userId, IEnumerable<UserAchievementProgress> achievements, CancellationToken ct = default);
}
