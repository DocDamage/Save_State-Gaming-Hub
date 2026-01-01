using Microsoft.Extensions.Logging;
using SaveState.Core.Analytics;
using SaveState.Core.Analytics.Entities;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.UserManagement.Services;

namespace SaveState.Infrastructure.Analytics;

public class GoalService : IGoalService
{
    private readonly IGamingGoalRepository _goalRepository;
    private readonly ISessionTrackingService _sessionTrackingService;
    private readonly IAchievementRepository _achievementRepository;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<GoalService> _logger;

    public GoalService(
        IGamingGoalRepository goalRepository,
        ISessionTrackingService sessionTrackingService,
        IAchievementRepository achievementRepository,
        IUserContextService userContextService,
        ILogger<GoalService> logger)
    {
        _goalRepository = goalRepository;
        _sessionTrackingService = sessionTrackingService;
        _achievementRepository = achievementRepository;
        _userContextService = userContextService;
        _logger = logger;
    }

    public async Task<Result<GamingGoal>> CreateGoalAsync(CreateGoalRequest request, CancellationToken ct = default)
    {
        try
        {
            var goal = GamingGoal.Create(
                request.Title,
                request.Type,
                request.TargetValue,
                request.EndDate,
                request.SpecificGameId);

            await _goalRepository.AddAsync(goal, ct);

            _logger.LogInformation("Created goal '{Title}' of type {Type} with target {Target}",
                goal.Title, goal.Type, goal.TargetValue);

            return Result<GamingGoal>.Success(goal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create goal '{Title}'", request.Title);
            return Result<GamingGoal>.Failure($"Failed to create goal: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<GamingGoal>>> GetActiveGoalsAsync(CancellationToken ct = default)
    {
        try
        {
            var goals = await _goalRepository.GetActiveGoalsAsync(ct);
            return Result<IReadOnlyList<GamingGoal>>.Success(goals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active goals");
            return Result<IReadOnlyList<GamingGoal>>.Failure($"Failed to get active goals: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> UpdateProgressAsync(CancellationToken ct = default)
    {
        try
        {
            var activeGoals = await _goalRepository.GetActiveGoalsAsync(ct);

            foreach (var goal in activeGoals)
            {
                var newValue = await CalculateCurrentValueAsync(goal, ct);
                if (newValue != goal.CurrentValue)
                {
                    goal.UpdateProgress(newValue);
                    await _goalRepository.UpdateAsync(goal, ct);

                    _logger.LogInformation("Updated goal '{Title}' progress: {Current}/{Target}",
                        goal.Title, goal.CurrentValue, goal.TargetValue);
                }
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update goal progress");
            return Result.Failure($"Failed to update goal progress: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<GamingGoal>>> GetCompletedGoalsAsync(int year, CancellationToken ct = default)
    {
        try
        {
            var goals = await _goalRepository.GetCompletedGoalsAsync(year, ct);
            return Result<IReadOnlyList<GamingGoal>>.Success(goals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get completed goals for year {Year}", year);
            return Result<IReadOnlyList<GamingGoal>>.Failure($"Failed to get completed goals: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> CancelGoalAsync(Guid goalId, CancellationToken ct = default)
    {
        try
        {
            var goal = await _goalRepository.GetByIdAsync(goalId, ct);
            if (goal is null)
                return Result.Failure("Goal not found", ErrorType.NotFound);

            goal.Cancel();
            await _goalRepository.UpdateAsync(goal, ct);

            _logger.LogInformation("Cancelled goal '{Title}'", goal.Title);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel goal {GoalId}", goalId);
            return Result.Failure($"Failed to cancel goal: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<GamingGoal?>> GetGoalAsync(Guid goalId, CancellationToken ct = default)
    {
        try
        {
            var goal = await _goalRepository.GetByIdAsync(goalId, ct);
            return Result<GamingGoal?>.Success(goal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get goal {GoalId}", goalId);
            return Result<GamingGoal?>.Failure($"Failed to get goal: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<GamingGoal>>> GetGoalsByTypeAsync(GoalType type, CancellationToken ct = default)
    {
        try
        {
            var goals = await _goalRepository.GetGoalsByTypeAsync(type, ct);
            return Result<IReadOnlyList<GamingGoal>>.Success(goals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get goals by type {Type}", type);
            return Result<IReadOnlyList<GamingGoal>>.Failure($"Failed to get goals by type: {ex.Message}", ErrorType.Internal);
        }
    }

    private async Task<int> CalculateCurrentValueAsync(GamingGoal goal, CancellationToken ct)
    {
        return goal.Type switch
        {
            GoalType.GamesCompleted => await CalculateGamesCompletedAsync(goal, ct),
            GoalType.PlaytimeHours => await CalculatePlaytimeHoursAsync(goal, ct),
            GoalType.PlaytimePerGame => await CalculatePlaytimePerGameAsync(goal, ct),
            GoalType.AchievementsEarned => await CalculateAchievementsEarnedAsync(goal, ct),
            GoalType.DailyStreak => await CalculateDailyStreakAsync(goal, ct),
            GoalType.GenreExploration => await CalculateGenreExplorationAsync(goal, ct),
            GoalType.SessionsCount => await CalculateSessionsCountAsync(goal, ct),
            _ => goal.CurrentValue
        };
    }

    private Task<int> CalculateGamesCompletedAsync(GamingGoal goal, CancellationToken ct)
    {
        // This would require tracking completed games - for now return current value
        // In a full implementation, we'd query games marked as completed
        _logger.LogWarning("CalculateGamesCompletedAsync not fully implemented");
        return Task.FromResult(goal.CurrentValue);
    }

    private Task<int> CalculatePlaytimeHoursAsync(GamingGoal goal, CancellationToken ct)
    {
        // Overall playtime calculation requires aggregating statistics from all games.
        // This would need a new ISessionTrackingService.GetAllUserStatisticsAsync method.
        var userId = _userContextService.GetCurrentUserIdRequired();
        _logger.LogDebug("Playtime hours calculation requires cross-game aggregation - returning 0 for now");
        return Task.FromResult(0);
    }

    private async Task<int> CalculatePlaytimePerGameAsync(GamingGoal goal, CancellationToken ct)
    {
        if (!goal.SpecificGameId.HasValue) return goal.CurrentValue;

        var statistics = await _sessionTrackingService.GetStatisticsAsync(goal.SpecificGameId.Value, ct);
        return (int)statistics.Value.TotalPlaytime.TotalHours;
    }

    private async Task<int> CalculateAchievementsEarnedAsync(GamingGoal goal, CancellationToken ct)
    {
        var userId = _userContextService.GetCurrentUserIdRequired();
        var achievements = await _achievementRepository.GetUserAchievementsAsync(userId, ct);
        return achievements.Count;
    }

    private Task<int> CalculateDailyStreakAsync(GamingGoal goal, CancellationToken ct)
    {
        // This would require daily streak calculation - for now return current value
        _logger.LogWarning("CalculateDailyStreakAsync not fully implemented");
        return Task.FromResult(goal.CurrentValue);
    }

    private Task<int> CalculateGenreExplorationAsync(GamingGoal goal, CancellationToken ct)
    {
        // This would require genre tracking - for now return current value
        _logger.LogWarning("CalculateGenreExplorationAsync not fully implemented");
        return Task.FromResult(goal.CurrentValue);
    }

    private Task<int> CalculateSessionsCountAsync(GamingGoal goal, CancellationToken ct)
    {
        // Overall session counting requires aggregating session counts from all games.
        // This would need a new ISessionTrackingService.GetAllUserSessionCountAsync method.
        var userId = _userContextService.GetCurrentUserIdRequired();
        _logger.LogDebug("Session count calculation requires cross-game aggregation - returning 0 for now");
        return Task.FromResult(0);
    }
}
