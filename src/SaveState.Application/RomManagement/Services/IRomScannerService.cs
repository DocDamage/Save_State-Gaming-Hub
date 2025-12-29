using SaveState.Core.RomManagement.Entities;

namespace SaveState.Application.RomManagement.Services;

public interface IRomScannerService
{
    Task<IReadOnlyList<RomFile>> ScanFolderAsync(
        string folderPath,
        Guid platformId,
        bool recursive,
        IProgress<ScanProgress>? progress,
        CancellationToken ct = default);
}

public record ScanProgress(
    int FilesScanned,
    int FilesTotal,
    string CurrentFile,
    int RomsFound);
