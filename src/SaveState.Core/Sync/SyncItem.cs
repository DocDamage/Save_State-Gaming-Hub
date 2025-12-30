namespace SaveState.Core.Sync;

/// <summary>
/// Represents an item that can be synchronized.
/// </summary>
public sealed record SyncItem(
    Guid Id,
    string EntityType,
    string EntityId,
    DateTimeOffset LastModified,
    string ContentHash,
    SyncItemState State);

public enum SyncItemState
{
    Pending,
    Syncing,
    Synced,
    Conflict,
    Error
}
