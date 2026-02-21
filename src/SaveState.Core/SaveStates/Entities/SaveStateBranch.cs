using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;

namespace SaveState.Core.SaveStates.Entities;

public class SaveStateBranch : EntityBase
{
    public Guid RootStateId { get; private set; }
    public string BranchName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public BranchType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SaveStateBranch() { }

    public static SaveStateBranch Create(Guid rootStateId, string name, BranchType type, ITimeProvider timeProvider, string? description = null)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        return new SaveStateBranch
        {
            Id = Guid.NewGuid(),
            RootStateId = rootStateId,
            BranchName = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Description = description ?? string.Empty,
            Type = type,
            CreatedAt = timeProvider.UtcNow
        };
    }

    [Obsolete("Use Create(Guid, string, BranchType, ITimeProvider, string?) instead")]
    public static SaveStateBranch Create(Guid rootStateId, string name, BranchType type, string? description = null)
    {
        return new SaveStateBranch
        {
            Id = Guid.NewGuid(),
            RootStateId = rootStateId,
            BranchName = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Description = description ?? string.Empty,
            Type = type,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDescription(string description)
    {
        Description = Guard.Against.NullOrWhiteSpace(description, nameof(description));
    }
}

public enum BranchType
{
    StoryBranch,      // Different story paths
    SpeedrunBranch,   // Speedrun attempts
    Experimental,     // Testing different strategies
    Backup           // Safety copies
}