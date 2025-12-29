using SaveState.Core.Common.Base;

namespace SaveState.Core.GameLibrary.Entities;

public class Genre : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected Genre() { } // EF Core

    public Genre(string name, string? description = null)
    {
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string description)
    {
        Description = Guard.Against.NullOrWhiteSpace(description, nameof(description));
    }
}
