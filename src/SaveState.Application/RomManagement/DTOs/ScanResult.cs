using SaveState.Core.Common.ValueObjects;

namespace SaveState.Application.RomManagement.DTOs;

public class ScanResult
{
    public int TotalFilesScanned { get; set; }
    public int ValidRomsFound { get; set; }
    public int InvalidFilesSkipped { get; set; }
    public int ChecksumsCalculated { get; set; }
    public IReadOnlyList<RomFileId> NewRomIds { get; set; } = Array.Empty<RomFileId>();
    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
    public TimeSpan ScanDuration { get; set; }
}
