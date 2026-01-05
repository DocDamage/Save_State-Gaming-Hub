using SaveState.Core.Common.Base;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.ValueObjects;

namespace SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Represents a user-defined goal for a game (e.g., "Complete all achievements", "Reach level 50").
/// </summary>
public class GameGoal : EntityBase
{
    public Guid Id { get; private set; }
    public GameId GameId { get; private set; } = null!;
    public UserId UserId { get; private set; } = null!;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? DueDate { get; private set; }
    public int? TargetValue { get; private set; }
    public int CurrentValue { get; private set; }
    public string? Unit { get; private set; } // e.g., "levels", "hours", "achievements"
    public bool IsCompleted => CompletedAt.HasValue;
    public double? Progress => TargetValue.HasValue && TargetValue > 0
        ? (double)CurrentValue / TargetValue * 100
        : null;

    private GameGoal() { } // EF Core

    /// <summary>
    /// Creates a new game goal.
    /// </summary>
    public static GameGoal Create(
        GameId gameId,
        UserId userId,
        string title,
        string? description = null,
        int? targetValue = null,
        string? unit = null,
        DateTime? dueDate = null)
    {
        return new GameGoal
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            UserId = userId,
            Title = Guard.Against.NullOrWhiteSpace(title, nameof(title)),
            Description = description,
            TargetValue = targetValue,
            Unit = unit,
            DueDate = dueDate,
            CurrentValue = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProgress(int currentValue)
    {
        CurrentValue = currentValue;

        if (TargetValue.HasValue && currentValue >= TargetValue.Value && !CompletedAt.HasValue)
        {
            MarkAsCompleted();
        }
    }

    public void MarkAsCompleted()
    {
        if (!CompletedAt.HasValue)
        {
            CompletedAt = DateTime.UtcNow;
        }
    }

    public void UpdateDetails(string? title = null, string? description = null, DateTime? dueDate = null)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title;
        }

        Description = description;
        DueDate = dueDate;
    }
}
