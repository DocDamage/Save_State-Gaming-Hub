namespace SaveState.Core.Sync;

/// <summary>
/// Represents the synchronization state for a device.
/// </summary>
public sealed class SyncManifest
{
    public Guid DeviceId { get; init; }
    public string DeviceName { get; init; } = Environment.MachineName;
    public DateTimeOffset LastSyncAt { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public List<SyncItem> Items { get; init; } = new();
    public int Version { get; set; } = 1;
}
