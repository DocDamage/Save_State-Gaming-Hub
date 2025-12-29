using SaveState.Core.RomManagement.Entities;

namespace SaveState.Core.RomManagement.Services;

public interface IRomScannerService
{
    Task<IReadOnlyList<RomFile>> ScanFolderAsync(
        string folderPath,
        string platformName,
        bool recursive = true,
        IProgress<ScanProgress>? progress = null,
        CancellationToken ct = default);

    Task<RomMetadata> GetRomMetadataAsync(string filePath, CancellationToken ct = default);
}

public class ScanProgress
{
    public string CurrentFile { get; set; } = string.Empty;
    public int Current { get; set; }
    public int Total { get; set; }
    public double Progress => Total > 0 ? (double)Current / Total : 0;
}

public class RomMetadata
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Region { get; set; }
    public string? Version { get; set; }
    public long FileSize { get; set; }
}
