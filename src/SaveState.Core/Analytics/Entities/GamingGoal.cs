using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;

namespace SaveState.Core.Analytics.Entities;

public class GamingGoal : EntityBase
{
    public string Title { get; private set; } = string.Empty;
    public GoalType Type { get; private set; }
    public int TargetValue { get; private set; }
    public int CurrentValue { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public Guid? SpecificGameId { get; private set; }
    public bool IsCompleted => CurrentValue >= TargetValue;
    public float ProgressPercent => TargetValue > 0 ? (float)CurrentValue / TargetValue * 100 : 0;
    public GoalStatus Status { get; private set; }

    private GamingGoal() { }

    public static GamingGoal Create(string title, GoalType type, int targetValue, ITimeProvider timeProvider, DateOnly? endDate = null, Guid? gameId = null)
    {
        return new GamingGoal
        {
            Id = Guid.NewGuid(),
            Title = Guard.Against.NullOrWhiteSpace(title, nameof(title)),
            Type = type,
            TargetValue = Guard.Against.NegativeOrZero(targetValue, nameof(targetValue)),
            CurrentValue = 0,
            StartDate = DateOnly.FromDateTime(timeProvider.UtcNow),
            EndDate = endDate,
            SpecificGameId = gameId,
            Status = GoalStatus.Active
        };
    }

    public void UpdateProgress(int newValue)
    {
        CurrentValue = Math.Max(0, newValue);
        if (IsCompleted && Status == GoalStatus.Active)
            Status = GoalStatus.Completed;
    }

    public void Cancel() => Status = GoalStatus.Cancelled;
    public void Fail() => Status = GoalStatus.Failed;
}

public enum GoalType
{
    GamesCompleted,
    PlaytimeHours,
    PlaytimePerGame,
    AchievementsEarned,
    DailyStreak,
    GenreExploration,
    SessionsCount
}

public enum GoalStatus
{
    Active,
    Completed,
    Failed,
    Cancelled
}