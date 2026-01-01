using Microsoft.EntityFrameworkCore;
using SaveState.Core.Analytics;
using SaveState.Core.Analytics.Entities;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Repositories;

public class GamingGoalRepository : IGamingGoalRepository
{
    private readonly SaveStateDbContext _context;

    public GamingGoalRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    public async Task<GamingGoal?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.GamingGoals
            .FirstOrDefaultAsync(g => g.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GamingGoal>> GetActiveGoalsAsync(CancellationToken ct = default)
    {
        return await _context.GamingGoals
            .Where(g => g.Status == GoalStatus.Active)
            .OrderBy(g => g.EndDate ?? DateOnly.MaxValue)
            .ThenBy(g => g.StartDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GamingGoal>> GetCompletedGoalsAsync(int year, CancellationToken ct = default)
    {
        return await _context.GamingGoals
            .Where(g => g.Status == GoalStatus.Completed &&
                       g.StartDate.Year <= year &&
                       (g.EndDate == null || g.EndDate.Value.Year >= year))
            .OrderByDescending(g => g.EndDate ?? g.StartDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GamingGoal>> GetGoalsByTypeAsync(GoalType type, CancellationToken ct = default)
    {
        return await _context.GamingGoals
            .Where(g => g.Type == type)
            .OrderBy(g => g.Status)
            .ThenByDescending(g => g.StartDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(GamingGoal goal, CancellationToken ct = default)
    {
        await _context.GamingGoals.AddAsync(goal, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(GamingGoal goal, CancellationToken ct = default)
    {
        _context.GamingGoals.Update(goal);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var goal = await GetByIdAsync(id, ct).ConfigureAwait(false);
        if (goal != null)
        {
            _context.GamingGoals.Remove(goal);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task<int> CountAsync(GoalStatus? status = null, CancellationToken ct = default)
    {
        var query = _context.GamingGoals.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(g => g.Status == status.Value);
        }

        return await query.CountAsync(ct).ConfigureAwait(false);
    }
}