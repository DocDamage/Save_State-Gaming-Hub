using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.GameLibrary;
using SaveState.Core.RomManagement.ValueObjects;
using SaveState.Application.RomManagement.DTOs;

namespace SaveState.Application.RomManagement.Services;

public class RomScannerService : IRomScannerService
{
    private readonly IPlatformRepository _platformRepository;
    private readonly IPlatformExtensionRegistry _extensionRegistry;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<RomScannerService> _logger;

    public RomScannerService(
        IPlatformRepository platformRepository,
        IPlatformExtensionRegistry extensionRegistry,
        ITimeProvider timeProvider,
        ILogger<RomScannerService> logger)
    {
        _platformRepository = platformRepository ?? throw new ArgumentNullException(nameof(platformRepository));
        _extensionRegistry = extensionRegistry ?? throw new ArgumentNullException(nameof(extensionRegistry));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<RomFile>> ScanFolderAsync(
        string folderPath,
        Guid platformId,
        bool recursive,
        IProgress<ScanProgress>? progress,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path cannot be null or empty", nameof(folderPath));

        if (platformId == Guid.Empty)
            throw new ArgumentException("Platform ID cannot be empty", nameof(platformId));

        if (!Directory.Exists(folderPath))
        {
            _logger.LogWarning("Scan folder does not exist: {FolderPath}", folderPath);
            return Array.Empty<RomFile>();
        }

        var platform = await _platformRepository.GetByIdAsync(platformId, ct).ConfigureAwait(false);
        if (platform == null)
        {
            _logger.LogWarning("Platform not found: {PlatformId}", platformId);
            return Array.Empty<RomFile>();
        }
        var platformNameValue = platform.Name.Value;
        var extensions = _extensionRegistry.GetExtensions(platformNameValue);
        if (extensions.Length == 0)
        {
            _logger.LogWarning("No file extensions defined for platform: {PlatformName}", platformNameValue);
            return Array.Empty<RomFile>();
        }

        _logger.LogInformation("Starting ROM scan for {Platform} in {FolderPath}", platformNameValue, folderPath);

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        // First pass: count total files to scan for progress reporting
        var allFiles = Directory.EnumerateFiles(folderPath, "*.*", searchOption)
            .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToList();

        if (allFiles.Count == 0)
        {
            _logger.LogInformation("No ROM files found for {Platform} in {FolderPath}", platformNameValue, folderPath);
            return Array.Empty<RomFile>();
        }

        _logger.LogInformation("Found {Count} potential ROM files to scan", allFiles.Count);

        var romFiles = new List<RomFile>();
        var processedCount = 0;

        foreach (var filePath in allFiles)
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogInformation("ROM scan cancelled after processing {Processed}/{Total} files", processedCount, allFiles.Count);
                break;
            }

            try
            {
                var fileInfo = new FileInfo(filePath);
                var romTitle = Path.GetFileNameWithoutExtension(filePath);

                // Create ROM file entity
                var romFile = new RomFile(
                    romTitle,
                    platform.Id,
                    new FilePath(filePath),
                    fileInfo.Length,
                    _timeProvider);

                romFiles.Add(romFile);
                processedCount++;

                progress?.Report(new ScanProgress(
                    FilesScanned: processedCount,
                    FilesTotal: allFiles.Count,
                    CurrentFile: Path.GetFileName(filePath),
                    RomsFound: romFiles.Count));

                _logger.LogDebug("Scanned ROM: {FileName} ({Size} bytes)", romTitle, fileInfo.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scan ROM file: {FilePath}", filePath);
                processedCount++;
            }
        }

        _logger.LogInformation("ROM scan completed: {Processed} files processed, {Valid} valid ROMs found for {Platform}",
            processedCount, romFiles.Count, platformNameValue);

        return romFiles;
    }

}
