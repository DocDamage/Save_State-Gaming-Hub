namespace SaveState.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of the achievement repository.
/// </summary>
public class AchievementRepository : IAchievementRepository
{
    private readonly ISaveStateDbContext _context;

    /// <summary>
    /// Initializes a new instance of the AchievementRepository.
    /// </summary>
    /// <param name="context">The database context.</param>
    public AchievementRepository(ISaveStateDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves an achievement by its ID.
    /// </summary>
    /// <param name="id">The achievement ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The achievement if found, null otherwise.</returns>
    public async Task<Achievement?> GetAchievementByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Achievements.FindAsync(new object[] { id }, ct);
    }

    /// <summary>
    /// Retrieves all active achievements.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of active achievements.</returns>
    public async Task<IReadOnlyList<Achievement>> GetActiveAchievementsAsync(CancellationToken ct = default)
    {
        return await _context.Achievements
            .Where(a => a.IsActive)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Retrieves achievements by type.
    /// </summary>
    /// <param name="type">The achievement type.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of achievements of the specified type.</returns>
    public async Task<IReadOnlyList<Achievement>> GetAchievementsByTypeAsync(AchievementType type, CancellationToken ct = default)
    {
        return await _context.Achievements
            .Where(a => a.Type == type && a.IsActive)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Retrieves user achievement progress for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of user achievements for the specified user.</returns>
    public async Task<IReadOnlyList<UserAchievement>> GetUserAchievementsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.UserAchievements
            .Include(ua => ua.Achievement)
            .Where(ua => ua.UserId == userId)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Retrieves a specific user achievement progress.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="achievementId">The achievement ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user achievement if found, null otherwise.</returns>
    public async Task<UserAchievement?> GetUserAchievementAsync(Guid userId, Guid achievementId, CancellationToken ct = default)
    {
        return await _context.UserAchievements
            .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId, ct);
    }

    /// <summary>
    /// Retrieves achievements with pagination and filtering support.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="type">Optional achievement type to filter by.</param>
    /// <param name="isActive">Optional active status filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated result containing the achievements.</returns>
    public async Task<PagedResult<Achievement>> GetAchievementsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        AchievementType? type = null,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        var query = _context.Achievements.AsQueryable();

        // Apply filters at database level
        if (type.HasValue)
        {
            query = query.Where(a => a.Type == type.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(a => a.IsActive == isActive.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(ct);

        // Apply default sorting (by name)
        query = query.OrderBy(a => a.Name);

        // Apply pagination
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Achievement>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<int> CountAsync(AchievementType? type = null, bool? isActive = null, CancellationToken ct = default)
    {
        var query = _context.Achievements.AsQueryable();

        if (type.HasValue)
        {
            query = query.Where(a => a.Type == type.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(a => a.IsActive == isActive.Value);
        }

        return await query.CountAsync(ct);
    }

    /// <summary>
    /// Adds a new achievement definition.
    /// </summary>
    /// <param name="achievement">The achievement to add.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task AddAchievementAsync(Achievement achievement, CancellationToken ct = default)
    {
        await _context.Achievements.AddAsync(achievement, ct);
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Adds or updates user achievement progress.
    /// </summary>
    /// <param name="userAchievement">The user achievement to add/update.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task AddOrUpdateUserAchievementAsync(UserAchievement userAchievement, CancellationToken ct = default)
    {
        var existing = await GetUserAchievementAsync(userAchievement.UserId, userAchievement.AchievementId, ct);

        if (existing == null)
        {
            await _context.UserAchievements.AddAsync(userAchievement, ct);
        }
        else
        {
            _context.UserAchievements.Update(userAchievement);
        }

        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Updates an existing achievement.
    /// </summary>
    /// <param name="achievement">The achievement to update.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task UpdateAchievementAsync(Achievement achievement, CancellationToken ct = default)
    {
        _context.Achievements.Update(achievement);
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Updates user achievement progress.
    /// </summary>
    /// <param name="userAchievement">The user achievement to update.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task UpdateUserAchievementAsync(UserAchievement userAchievement, CancellationToken ct = default)
    {
        _context.UserAchievements.Update(userAchievement);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<UserAchievement>> GetRecentUnlockedAsync(int limit, CancellationToken ct = default)
    {
        return await _context.UserAchievements
            .Include(ua => ua.Achievement)
            .Where(ua => ua.UnlockedAt != null)
            .OrderByDescending(ua => ua.UnlockedAt)
            .Take(limit)
            .ToListAsync(ct);
    }
}
