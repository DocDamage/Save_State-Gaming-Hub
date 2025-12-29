using SaveState.Core.Common.Base;
using SaveState.Core.Common.Interfaces;

namespace SaveState.Core.GameLibrary.Entities;

public class Achievement : EntityBase, IAggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string IconPath { get; private set; } = string.Empty;
    public int Points { get; private set; }
    public AchievementType Type { get; private set; }
    public string? Criteria { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected Achievement() { } // EF Core

    public Achievement(string name, string description, string iconPath, int points, AchievementType type)
    {
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Description = Guard.Against.NullOrWhiteSpace(description, nameof(description));
        IconPath = Guard.Against.NullOrWhiteSpace(iconPath, nameof(iconPath));
        Points = Guard.Against.Negative(points, nameof(points));
        Type = type;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
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
