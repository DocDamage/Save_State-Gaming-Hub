using SaveState.Core.Common.Events;

namespace SaveState.Core.RomManagement.Events;

public class RomFileCorruptedEvent : EventBase
{
    public Guid RomFileId { get; }
    public string FilePath { get; }
    public DateTime DetectedAt { get; }

    public RomFileCorruptedEvent(Guid romFileId, string filePath, DateTime detectedAt)
    {
        RomFileId = romFileId;
        FilePath = Guard.Against.NullOrWhiteSpace(filePath, nameof(filePath));
        DetectedAt = detectedAt;
    }
}
