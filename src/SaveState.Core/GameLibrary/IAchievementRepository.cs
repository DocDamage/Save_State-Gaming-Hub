namespace SaveState.Core.GameLibrary;

using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Repository interface for managing achievements and user achievement progress.
/// </summary>
public interface IAchievementRepository
{
    /// <summary>
    /// Retrieves an achievement by its ID.
    /// </summary>
    /// <param name="id">The achievement ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The achievement if found, null otherwise.</returns>
    Task<Achievement?> GetAchievementByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all active achievements.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of active achievements.</returns>
    Task<IReadOnlyList<Achievement>> GetActiveAchievementsAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves achievements by type.
    /// </summary>
    /// <param name="type">The achievement type.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of achievements of the specified type.</returns>
    Task<IReadOnlyList<Achievement>> GetAchievementsByTypeAsync(AchievementType type, CancellationToken ct = default);

    /// <summary>
    /// Retrieves achievements with pagination and filtering support.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="type">Optional achievement type to filter by.</param>
    /// <param name="isActive">Optional active status filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated result containing the achievements.</returns>
    Task<PagedResult<Achievement>> GetAchievementsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        AchievementType? type = null,
        bool? isActive = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the total count of achievements.
    /// </summary>
    /// <param name="type">Optional achievement type filter.</param>
    /// <param name="isActive">Optional active status filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The total number of achievements matching the filters.</returns>
    Task<int> CountAsync(AchievementType? type = null, bool? isActive = null, CancellationToken ct = default);

    /// <summary>
    /// Retrieves user achievement progress for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of user achievements for the specified user.</returns>
    Task<IReadOnlyList<UserAchievement>> GetUserAchievementsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a specific user achievement progress.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="achievementId">The achievement ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user achievement if found, null otherwise.</returns>
    Task<UserAchievement?> GetUserAchievementAsync(Guid userId, Guid achievementId, CancellationToken ct = default);

    /// <summary>
    /// Adds a new achievement definition.
    /// </summary>
    /// <param name="achievement">The achievement to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAchievementAsync(Achievement achievement, CancellationToken ct = default);

    /// <summary>
    /// Adds or updates user achievement progress.
    /// </summary>
    /// <param name="userAchievement">The user achievement to add/update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddOrUpdateUserAchievementAsync(UserAchievement userAchievement, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing achievement.
    /// </summary>
    /// <param name="achievement">The achievement to update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAchievementAsync(Achievement achievement, CancellationToken ct = default);

    /// <summary>
    /// Updates user achievement progress.
    /// </summary>
    /// <param name="userAchievement">The user achievement to update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateUserAchievementAsync(UserAchievement userAchievement, CancellationToken ct = default);
}
