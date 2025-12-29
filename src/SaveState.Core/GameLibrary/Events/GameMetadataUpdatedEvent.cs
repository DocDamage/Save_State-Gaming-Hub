namespace SaveState.Core.GameLibrary.Events;

using SaveState.Core.Common.Events;

/// <summary>
/// Event raised when game metadata is updated.
/// </summary>
public class GameMetadataUpdatedEvent : EventBase
{
    public Guid GameId { get; }
    public string? Description { get; }
    public IReadOnlyList<string> Tags { get; }

    public GameMetadataUpdatedEvent(Guid gameId, string? description, IEnumerable<string> tags)
    {
        GameId = gameId;
        Description = description;
        Tags = tags.ToList().AsReadOnly();
    }
}
