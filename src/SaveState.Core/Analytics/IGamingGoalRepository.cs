using SaveState.Core.Analytics.Entities;
using SaveState.Core.Common;

namespace SaveState.Core.Analytics;

public interface IGamingGoalRepository
{
    Task<GamingGoal?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<GamingGoal>> GetActiveGoalsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<GamingGoal>> GetCompletedGoalsAsync(int year, CancellationToken ct = default);
    Task<IReadOnlyList<GamingGoal>> GetGoalsByTypeAsync(GoalType type, CancellationToken ct = default);
    Task AddAsync(GamingGoal goal, CancellationToken ct = default);
    Task UpdateAsync(GamingGoal goal, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> CountAsync(GoalStatus? status = null, CancellationToken ct = default);
}