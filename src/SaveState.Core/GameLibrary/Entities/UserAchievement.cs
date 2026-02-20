using SaveState.Core.Common.Base;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.Common.Services;

namespace SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Represents a user's progress towards unlocking an achievement.
/// </summary>
public class UserAchievement : EntityBase
{
    private static DateTime UtcNow => SystemTimeProvider.Instance.UtcNow;

    /// <summary>
    /// The ID of the user who is working towards this achievement.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The achievement being worked towards.
    /// </summary>
    public Guid AchievementId { get; private set; }

    /// <summary>
    /// The achievement definition.
    /// </summary>
    public Achievement? Achievement { get; private set; }

    /// <summary>
    /// Current progress value (e.g., games completed, hours played).
    /// </summary>
    public int CurrentProgress { get; private set; }

    /// <summary>
    /// Target progress value required to unlock the achievement.
    /// </summary>
    public int TargetProgress { get; private set; }

    /// <summary>
    /// Whether the achievement has been unlocked.
    /// </summary>
    public bool IsUnlocked { get; private set; }

    /// <summary>
    /// When the achievement was unlocked (if applicable).
    /// </summary>
    public DateTime? UnlockedAt { get; private set; }

    /// <summary>
    /// When this progress was last updated.
    /// </summary>
    public DateTime LastUpdatedAt { get; private set; }

    /// <summary>
    /// Additional metadata as JSON (for flexible progress tracking).
    /// </summary>
    public string? Metadata { get; private set; }

    protected UserAchievement() { } // EF Core

    /// <summary>
    /// Creates a new UserAchievement instance.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="achievementId">The achievement ID.</param>
    /// <param name="targetProgress">The target progress value.</param>
    public UserAchievement(Guid userId, Guid achievementId, int targetProgress)
    {
        UserId = userId;
        AchievementId = achievementId;
        CurrentProgress = 0;
        TargetProgress = targetProgress;
        IsUnlocked = false;
        LastUpdatedAt = UtcNow;
    }

    /// <summary>
    /// Updates the current progress towards the achievement.
    /// </summary>
    /// <param name="newProgress">The new progress value.</param>
    /// <param name="metadata">Optional metadata for the progress update.</param>
    public void UpdateProgress(int newProgress, string? metadata = null)
    {
        CurrentProgress = Math.Max(0, newProgress);
        LastUpdatedAt = UtcNow;

        if (metadata != null)
        {
            Metadata = metadata;
        }

        // Check if achievement should be unlocked
        if (CurrentProgress >= TargetProgress && !IsUnlocked)
        {
            Unlock();
        }
    }

    /// <summary>
    /// Adds incremental progress to the achievement.
    /// </summary>
    /// <param name="increment">The amount to add to current progress.</param>
    /// <param name="metadata">Optional metadata for the progress update.</param>
    public void AddProgress(int increment, string? metadata = null)
    {
        UpdateProgress(CurrentProgress + increment, metadata);
    }

    /// <summary>
    /// Marks the achievement as unlocked.
    /// </summary>
    public void Unlock()
    {
        if (!IsUnlocked)
        {
            IsUnlocked = true;
            UnlockedAt = UtcNow;
            LastUpdatedAt = UtcNow;
        }
    }

    /// <summary>
    /// Resets the achievement progress.
    /// </summary>
    public void ResetProgress()
    {
        CurrentProgress = 0;
        IsUnlocked = false;
        UnlockedAt = null;
        LastUpdatedAt = UtcNow;
        Metadata = null;
    }
}
