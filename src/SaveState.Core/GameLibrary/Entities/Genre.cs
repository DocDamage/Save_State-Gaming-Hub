using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;

namespace SaveState.Core.GameLibrary.Entities;

public class Genre : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected Genre() { } // EF Core

    public Genre(string name, ITimeProvider timeProvider, string? description = null)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Description = description;
        CreatedAt = timeProvider.UtcNow;
    }

    public Genre(string name, DateTime createdAt, string? description = null)
    {
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Description = description;
        CreatedAt = createdAt;
    }

    [Obsolete("Use constructor with ITimeProvider or DateTime parameter")]
    public Genre(string name, string? description = null)
    {
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Description = description;
        CreatedAt = SystemTimeProvider.Instance.UtcNow;
    }

    public void UpdateDescription(string description)
    {
        Description = Guard.Against.NullOrWhiteSpace(description, nameof(description));
    }
}
