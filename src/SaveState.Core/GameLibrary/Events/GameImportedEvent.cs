namespace SaveState.Core.GameLibrary.Events;

using SaveState.Core.Common.Events;
using SaveState.Core.Common.Services;

/// <summary>
/// Event raised when a new game is imported into the library.
/// </summary>
public class GameImportedEvent : EventBase
{
    public Guid GameId { get; }
    public string Source { get; }
    public string? SourceId { get; }
    public DateTime ImportedAt { get; }

    public GameImportedEvent(Guid gameId, string source, string? sourceId = null) : base(SystemTimeProvider.Instance)
    {
        GameId = gameId;
        Source = source;
        SourceId = sourceId;
        ImportedAt = OccurredOn;
    }

    public GameImportedEvent(Guid gameId, string source, string? sourceId, ITimeProvider timeProvider) : base(timeProvider)
    {
        GameId = gameId;
        Source = source;
        SourceId = sourceId;
        ImportedAt = OccurredOn;
    }
}
