using SaveState.Core.Common.Base;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.RomManagement.ValueObjects;
using SaveState.Core.RomManagement.Enums;
using SaveState.Core.RomManagement.Events;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.RomManagement.Entities;

public class RomFile : EntityBase, IAggregateRoot, ISoftDelete
{
    public string Title { get; private set; } = string.Empty;
    public FilePath FilePath { get; private set; } = null!;
    public long FileSize { get; private set; }
    public Guid PlatformId { get; private set; }
    public Platform Platform { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? Region { get; private set; }
    public string? Version { get; private set; }
    public RomStatus Status { get; private set; }
    public string? Checksum { get; private set; }
    public string Hash => Checksum ?? string.Empty;
    public DateTime ScannedAt { get; private set; }
    public DateTime? VerifiedAt { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    protected RomFile() { } // EF Core

    public RomFile(
        string title,
        Guid platformId,
        FilePath filePath,
        long fileSize)
    {
        Title = Guard.Against.NullOrWhiteSpace(title, nameof(title));
        PlatformId = platformId;
        FilePath = Guard.Against.Null(filePath, nameof(filePath));
        FileSize = Guard.Against.Negative(fileSize, nameof(fileSize));
        Status = RomStatus.Scanned;
        ScannedAt = DateTime.UtcNow;
    }

    public void UpdateTitle(string title)
    {
        Title = Guard.Against.NullOrWhiteSpace(title, nameof(title));
    }

    public void SetMetadata(string? description, string? region, string? version)
    {
        Description = description;
        Region = region;
        Version = version;
    }

    public void SetChecksum(string checksum)
    {
        Checksum = Guard.Against.NullOrWhiteSpace(checksum, nameof(checksum));
    }

    public void MarkAsVerified()
    {
        if (Status != RomStatus.Scanned && Status != RomStatus.Processing)
            throw new InvalidOperationException("Can only verify scanned or processing ROMs");

        Status = RomStatus.Verified;
        VerifiedAt = DateTime.UtcNow;

        AddDomainEvent(new RomFileVerifiedEvent((Guid)Id, FilePath.Value));
    }

    public void MarkAsCorrupted()
    {
        Status = RomStatus.Corrupted;
        AddDomainEvent(new RomFileCorruptedEvent((Guid)Id, FilePath.Value));
    }

    public void MarkAsProcessing()
    {
        Status = RomStatus.Processing;
    }
}
