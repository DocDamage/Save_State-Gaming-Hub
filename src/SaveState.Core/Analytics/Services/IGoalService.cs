using SaveState.Core.Analytics.Entities;
using SaveState.Core.Common;

namespace SaveState.Core.Analytics.Services;

public interface IGoalService
{
    Task<Result<GamingGoal>> CreateGoalAsync(CreateGoalRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GamingGoal>>> GetActiveGoalsAsync(CancellationToken ct = default);
    Task<Result> UpdateProgressAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<GamingGoal>>> GetCompletedGoalsAsync(int year, CancellationToken ct = default);
    Task<Result> CancelGoalAsync(Guid goalId, CancellationToken ct = default);
    Task<Result<GamingGoal?>> GetGoalAsync(Guid goalId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GamingGoal>>> GetGoalsByTypeAsync(GoalType type, CancellationToken ct = default);
}

public sealed record CreateGoalRequest(
    string Title,
    GoalType Type,
    int TargetValue,
    DateOnly? EndDate = null,
    Guid? SpecificGameId = null);