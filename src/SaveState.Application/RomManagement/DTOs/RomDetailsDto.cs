using SaveState.Core.Common.ValueObjects;
using SaveState.Core.RomManagement.Enums;

namespace SaveState.Application.RomManagement.DTOs;

public class RomDetailsDto
{
    public required RomFileId Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Description { get; set; }
    public string? Region { get; set; }
    public string? Version { get; set; }
    public RomStatus Status { get; set; }
    public string? Checksum { get; set; }
    public DateTime ScannedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
}
