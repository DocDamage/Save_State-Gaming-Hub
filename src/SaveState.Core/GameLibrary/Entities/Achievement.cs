using SaveState.Core.Common.Base;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.Common.Services;

namespace SaveState.Core.GameLibrary.Entities;

public class Achievement : EntityBase, IAggregateRoot
{
    public Guid? GameId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string IconPath { get; private set; } = string.Empty;
    public int Points { get; private set; }
    public AchievementType Type { get; private set; }
    public int TargetValue { get; private set; }
    public string? Criteria { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected Achievement() { } // EF Core

    public Achievement(string name, string description, string iconPath, int points, AchievementType type, ITimeProvider timeProvider, int targetValue = 1, Guid? gameId = null)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Description = Guard.Against.NullOrWhiteSpace(description, nameof(description));
        IconPath = Guard.Against.NullOrWhiteSpace(iconPath, nameof(iconPath));
        Points = Guard.Against.Negative(points, nameof(points));
        Type = type;
        TargetValue = Guard.Against.NegativeOrZero(targetValue, nameof(targetValue));
        GameId = gameId;
        IsActive = true;
        CreatedAt = timeProvider.UtcNow;
    }

    public Achievement(string name, string description, string iconPath, int points, AchievementType type, DateTime createdAt, int targetValue = 1, Guid? gameId = null)
    {
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Description = Guard.Against.NullOrWhiteSpace(description, nameof(description));
        IconPath = Guard.Against.NullOrWhiteSpace(iconPath, nameof(iconPath));
        Points = Guard.Against.Negative(points, nameof(points));
        Type = type;
        TargetValue = Guard.Against.NegativeOrZero(targetValue, nameof(targetValue));
        GameId = gameId;
        IsActive = true;
        CreatedAt = createdAt;
    }

    [Obsolete("Use constructor with ITimeProvider or DateTime parameter")]
    public Achievement(string name, string description, string iconPath, int points, AchievementType type, int targetValue = 1, Guid? gameId = null)
    {
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Description = Guard.Against.NullOrWhiteSpace(description, nameof(description));
        IconPath = Guard.Against.NullOrWhiteSpace(iconPath, nameof(iconPath));
        Points = Guard.Against.Negative(points, nameof(points));
        Type = type;
        TargetValue = Guard.Against.NegativeOrZero(targetValue, nameof(targetValue));
        GameId = gameId;
        IsActive = true;
        CreatedAt = SystemTimeProvider.Instance.UtcNow;
    }

    public void UpdateDetails(string description, string iconPath, int points)
    {
        Description = Guard.Against.NullOrWhiteSpace(description, nameof(description));
        IconPath = Guard.Against.NullOrWhiteSpace(iconPath, nameof(iconPath));
        Points = Guard.Against.Negative(points, nameof(points));
    }

    public void SetCriteria(string criteria)
    {
        Criteria = Guard.Against.NullOrWhiteSpace(criteria, nameof(criteria));
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}

public enum AchievementType
{
    GameCompletion,
    PlayTime,
    Collection,
    Social,
    Special
}
