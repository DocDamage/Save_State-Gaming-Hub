namespace SaveState.Core.RomManagement.Events;

using SaveState.Core.Common.Events;

/// <summary>
/// Event raised when a ROM file is scanned and discovered.
/// </summary>
public class RomFileScannedEvent : EventBase
{
    public Guid RomFileId { get; }
    public string FilePath { get; }
    public long FileSize { get; }
    public string Platform { get; }

    public RomFileScannedEvent(Guid romFileId, string filePath, long fileSize, string platform)
    {
        RomFileId = romFileId;
        FilePath = filePath;
        FileSize = fileSize;
        Platform = platform;
    }
}
