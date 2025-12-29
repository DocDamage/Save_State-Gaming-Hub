namespace SaveState.Application.GameLibrary.Services;

using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;

/// <summary>
/// Implementation of the achievement service for managing user achievements.
/// </summary>
public class AchievementService : IAchievementService
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly IGameRepository _gameRepository;

    /// <summary>
    /// Initializes a new instance of the AchievementService.
    /// </summary>
    /// <param name="achievementRepository">The achievement repository.</param>
    /// <param name="gameRepository">The game repository.</param>
    public AchievementService(
        IAchievementRepository achievementRepository,
        IGameRepository gameRepository)
    {
        _achievementRepository = achievementRepository;
        _gameRepository = gameRepository;
    }

    /// <summary>
    /// Checks if a user has unlocked any achievements based on current progress.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of newly unlocked achievements.</returns>
    public async Task<IReadOnlyList<Achievement>> CheckForUnlockedAchievementsAsync(Guid userId, CancellationToken ct = default)
    {
        var userAchievements = await _achievementRepository.GetUserAchievementsAsync(userId, ct);
        var newlyUnlocked = new List<Achievement>();

        foreach (var userAchievement in userAchievements)
        {
            if (!userAchievement.IsUnlocked && userAchievement.CurrentProgress >= userAchievement.TargetProgress)
            {
                userAchievement.Unlock();
                await _achievementRepository.UpdateUserAchievementAsync(userAchievement, ct);

                if (userAchievement.Achievement != null)
                {
                    newlyUnlocked.Add(userAchievement.Achievement);
                }
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
                // Calculate target progress based on achievement type and criteria
                var targetProgress = CalculateTargetProgress(achievement, userId);

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
    public async Task<IReadOnlyList<Achievement>> GetUnlockedAchievementsAsync(Guid userId, CancellationToken ct = default)
    {
        var userAchievements = await _achievementRepository.GetUserAchievementsAsync(userId, ct);

        return userAchievements
            .Where(ua => ua.IsUnlocked && ua.Achievement != null)
            .Select(ua => ua.Achievement!)
            .ToList();
    }

    /// <summary>
    /// Gets user achievement progress including locked achievements.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of user achievement progress.</returns>
    public async Task<IReadOnlyList<UserAchievement>> GetUserProgressAsync(Guid userId, CancellationToken ct = default)
    {
        return await _achievementRepository.GetUserAchievementsAsync(userId, ct);
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

    private static int CalculateTargetProgress(Achievement achievement, Guid userId)
    {
        // This is a simplified implementation. In a real system, you'd have
        // more sophisticated logic based on achievement criteria.
        return achievement.Type switch
        {
            AchievementType.GameCompletion => 10, // Complete 10 games
            AchievementType.PlayTime => 100, // 100 hours of playtime
            AchievementType.Collection => 50, // Collect 50 items
            AchievementType.Social => 5, // 5 social interactions
            AchievementType.Special => 1, // One-time special achievement
            _ => 1
        };
    }
}
