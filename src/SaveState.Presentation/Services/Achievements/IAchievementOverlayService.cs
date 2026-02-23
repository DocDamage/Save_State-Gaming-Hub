using SaveState.Presentation.Models.Achievements;

namespace SaveState.Presentation.Services.Achievements;

/// <summary>
/// Service for displaying achievement overlay notifications and managing achievement UI state.
/// </summary>
public interface IAchievementOverlayService
{
    /// <summary>
    /// Shows an achievement unlock notification.
    /// </summary>
    /// <param name="achievement">The achievement notification to display.</param>
    void ShowAchievementUnlocked(AchievementNotification achievement);

    /// <summary>
    /// Shows a progress update for the current game's achievements.
    /// </summary>
    /// <param name="progress">The achievement progress to display.</param>
    void ShowProgressUpdate(AchievementProgress progress);

    /// <summary>
    /// Shows a milestone reached notification.
    /// </summary>
    /// <param name="milestone">The milestone description.</param>
    /// <param name="value">The milestone value.</param>
    void ShowMilestoneReached(string milestone, int value);

    /// <summary>
    /// Shows a milestone reached notification with full details.
    /// </summary>
    /// <param name="milestone">The milestone details.</param>
    void ShowMilestone(AchievementMilestone milestone);

    /// <summary>
    /// Registers the achievement unlock handler with the RetroAchievements service.
    /// </summary>
    void RegisterAchievementUnlockHandler();

    /// <summary>
    /// Unregisters the achievement unlock handler.
    /// </summary>
    void UnregisterAchievementUnlockHandler();

    /// <summary>
    /// Dismisses all active achievement notifications.
    /// </summary>
    void DismissAllNotifications();

    /// <summary>
    /// Gets whether there are any active notifications.
    /// </summary>
    bool HasActiveNotifications { get; }

    /// <summary>
    /// Event raised when an achievement is unlocked and displayed.
    /// </summary>
    event EventHandler<AchievementNotification>? OnAchievementUnlocked;

    /// <summary>
    /// Event raised when a notification is dismissed.
    /// </summary>
    event EventHandler<AchievementNotification>? OnNotificationDismissed;

    /// <summary>
    /// Event raised when all notifications are dismissed.
    /// </summary>
    event EventHandler? OnAllNotificationsDismissed;
}
