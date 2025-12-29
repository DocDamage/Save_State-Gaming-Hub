using SaveState.Core.Common.Events;

namespace SaveState.Core.RomManagement.Events;

public class RomFileVerifiedEvent : EventBase
{
    public Guid RomFileId { get; }
    public string FilePath { get; }
    public DateTime VerifiedAt { get; }

    public RomFileVerifiedEvent(Guid romFileId, string filePath)
    {
        RomFileId = romFileId;
        FilePath = Guard.Against.NullOrWhiteSpace(filePath, nameof(filePath));
        VerifiedAt = DateTime.UtcNow;
    }
}
