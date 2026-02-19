namespace SaveState.Core.GameLibrary.Services;

using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.Common;

/// <summary>
/// Service interface for managing achievements and tracking user progress.
/// </summary>
public interface IAchievementService
{
    /// <summary>
    /// Checks if a user has unlocked any achievements based on current progress.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of newly unlocked achievements.</returns>
    Task<IReadOnlyList<Achievement>> CheckForUnlockedAchievementsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Updates user progress for a specific achievement type.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="achievementType">The type of achievement to update.</param>
    /// <param name="progressIncrement">The amount to increment progress by.</param>
    /// <param name="metadata">Optional metadata for the progress update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated user achievements.</returns>
    Task<IReadOnlyList<UserAchievement>> UpdateProgressAsync(
        Guid userId,
        AchievementType achievementType,
        int progressIncrement,
        string? metadata = null,
        CancellationToken ct = default);

    /// <summary>
    /// Manually awards an achievement to a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="achievementId">The achievement ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the achievement was awarded, false if already unlocked.</returns>
    Task<bool> AwardAchievementAsync(Guid userId, Guid achievementId, CancellationToken ct = default);

    /// <summary>
    /// Gets all achievements that a user has unlocked.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of unlocked achievements.</returns>
    Task<Result<IReadOnlyList<Achievement>>> GetUnlockedAchievementsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets user achievement progress including locked achievements.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of user achievement progress.</returns>
    Task<Result<IReadOnlyList<UserAchievement>>> GetUserProgressAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Resets all user achievement progress (for testing/debugging).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ResetUserProgressAsync(Guid userId, CancellationToken ct = default);
}
