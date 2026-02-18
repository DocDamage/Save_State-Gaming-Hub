// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.Achievements;

/// <summary>
/// Service for tracking and managing user achievements across platforms.
/// </summary>
public interface IAchievementTrackingService
{
    /// <summary>
    /// Gets all achievements for a user.
    /// </summary>
    Task<IReadOnlyList<UserAchievementProgress>> GetUserAchievementsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets achievements for a specific game.
    /// </summary>
    Task<IReadOnlyList<UserAchievementProgress>> GetGameAchievementsAsync(Guid userId, Guid gameId, CancellationToken ct = default);

    /// <summary>
    /// Gets recent achievements earned by the user.
    /// </summary>
    Task<IReadOnlyList<UserAchievementProgress>> GetRecentAchievementsAsync(Guid userId, int count = 10, CancellationToken ct = default);

    /// <summary>
    /// Gets achievement statistics for the user.
    /// </summary>
    Task<AchievementStatistics> GetStatisticsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Tracks achievement progress.
    /// </summary>
    Task TrackProgressAsync(Guid userId, string achievementKey, int progress, CancellationToken ct = default);

    /// <summary>
    /// Unlocks an achievement for the user.
    /// </summary>
    Task UnlockAchievementAsync(Guid userId, string achievementKey, CancellationToken ct = default);

    /// <summary>
    /// Syncs achievements from external platforms (Steam, RetroAchievements, etc.).
    /// </summary>
    Task SyncExternalAchievementsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets rare achievements (less than X% of users have unlocked).
    /// </summary>
    Task<IReadOnlyList<UserAchievementProgress>> GetRareAchievementsAsync(Guid userId, double maxUnlockRate = 10.0, CancellationToken ct = default);

    /// <summary>
    /// Gets the next recommended achievements to pursue.
    /// </summary>
    Task<IReadOnlyList<AchievementRecommendation>> GetRecommendationsAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// User's progress on an achievement.
/// </summary>
public class UserAchievementProgress
{
    /// <summary>
    /// Achievement ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Achievement name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Achievement description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Icon URL or path.
    /// </summary>
    public string IconUrl { get; set; } = string.Empty;

    /// <summary>
    /// Points awarded.
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// Achievement type.
    /// </summary>
    public AchievementType Type { get; set; }

    /// <summary>
    /// Associated game name.
    /// </summary>
    public string? GameName { get; set; }

    /// <summary>
    /// Game ID.
    /// </summary>
    public Guid? GameId { get; set; }

    /// <summary>
    /// Current progress value.
    /// </summary>
    public int CurrentProgress { get; set; }

    /// <summary>
    /// Target value to unlock.
    /// </summary>
    public int TargetValue { get; set; }

    /// <summary>
    /// Whether the achievement is unlocked.
    /// </summary>
    public bool IsUnlocked { get; set; }

    /// <summary>
    /// When the achievement was unlocked.
    /// </summary>
    public DateTime? UnlockedAt { get; set; }

    /// <summary>
    /// Platform (Steam, RetroAchievements, Internal, etc.).
    /// </summary>
    public string Platform { get; set; } = "Internal";

    /// <summary>
    /// Rarity percentage (how many users have unlocked this).
    /// </summary>
    public double? RarityPercent { get; set; }

    /// <summary>
    /// Progress percentage.
    /// </summary>
    public double ProgressPercent => TargetValue > 0 
        ? Math.Min(100, (double)CurrentProgress / TargetValue * 100) 
        : (IsUnlocked ? 100 : 0);

    /// <summary>
    /// Formatted progress text.
    /// </summary>
    public string ProgressText => TargetValue > 1 
        ? $"{CurrentProgress} / {TargetValue}" 
        : (IsUnlocked ? "Unlocked" : "Locked");

    /// <summary>
    /// Whether this is a rare achievement.
    /// </summary>
    public bool IsRare => RarityPercent.HasValue && RarityPercent.Value < 10.0;
}

/// <summary>
/// Achievement statistics for a user.
/// </summary>
public class AchievementStatistics
{
    /// <summary>
    /// Total achievements available.
    /// </summary>
    public int TotalAchievements { get; set; }

    /// <summary>
    /// Number of achievements unlocked.
    /// </summary>
    public int UnlockedCount { get; set; }

    /// <summary>
    /// Total points earned.
    /// </summary>
    public int TotalPoints { get; set; }

    /// <summary>
    /// Maximum possible points.
    /// </summary>
    public int MaxPoints { get; set; }

    /// <summary>
    /// Completion percentage.
    /// </summary>
    public double CompletionPercent => TotalAchievements > 0 
        ? (double)UnlockedCount / TotalAchievements * 100 
        : 0;

    /// <summary>
    /// Number of rare achievements unlocked.
    /// </summary>
    public int RareAchievementsCount { get; set; }

    /// <summary>
    /// Current streak (days with at least one achievement).
    /// </summary>
    public int CurrentStreak { get; set; }

    /// <summary>
    /// Longest streak achieved.
    /// </summary>
    public int LongestStreak { get; set; }

    /// <summary>
    /// Achievements unlocked this month.
    /// </summary>
    public int AchievementsThisMonth { get; set; }

    /// <summary>
    /// Achievements unlocked today.
    /// </summary>
    public int AchievementsToday { get; set; }

    /// <summary>
    /// Platform breakdown.
    /// </summary>
    public Dictionary<string, PlatformStats> ByPlatform { get; set; } = new();

    /// <summary>
    /// Type breakdown.
    /// </summary>
    public Dictionary<AchievementType, int> ByType { get; set; } = new();
}

/// <summary>
/// Platform-specific achievement stats.
/// </summary>
public class PlatformStats
{
    public int Unlocked { get; set; }
    public int Total { get; set; }
    public int Points { get; set; }
}

/// <summary>
/// Achievement recommendation for the user.
/// </summary>
public class AchievementRecommendation
{
    /// <summary>
    /// The achievement.
    /// </summary>
    public UserAchievementProgress Achievement { get; set; } = null!;

    /// <summary>
    /// Why this is recommended.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Estimated difficulty (1-10).
    /// </summary>
    public int Difficulty { get; set; }

    /// <summary>
    /// How close to completion (0-100).
    /// </summary>
    public double CompletionPercent { get; set; }

    /// <summary>
    /// Points that would be earned.
    /// </summary>
    public int PointsReward { get; set; }
}

/// <summary>
/// Achievement unlocked event.
/// </summary>
public class AchievementUnlockedEvent : Common.Events.IEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; set; }
    public Guid UserId { get; set; }
    public string AchievementName { get; set; } = string.Empty;
    public string? GameName { get; set; }
    public int Points { get; set; }
    public bool IsRare { get; set; }
    public DateTime UnlockedAt { get; set; }
}
