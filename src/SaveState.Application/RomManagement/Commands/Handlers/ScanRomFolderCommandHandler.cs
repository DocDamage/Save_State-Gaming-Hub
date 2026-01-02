using Microsoft.Extensions.Logging;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Application.RomManagement.DTOs;
using SaveState.Application.RomManagement.Services;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.RomManagement;

namespace SaveState.Application.RomManagement.Commands.Handlers;

/// <summary>
/// Handler for scanning ROM folders.
/// Discovers and catalogs ROM files in specified directories.
/// </summary>
public class ScanRomFolderCommandHandler : MediatR.IRequestHandler<ScanRomFolderCommand, Result<ScanResult>>
{
    private readonly IRomScannerService _scannerService;
    private readonly IRomFileRepository _romRepository;
    private readonly IPlatformRepository _platformRepository;
    private readonly ILogger<ScanRomFolderCommandHandler> _logger;

    public ScanRomFolderCommandHandler(
        IRomScannerService scannerService,
        IRomFileRepository romRepository,
        IPlatformRepository platformRepository,
        ILogger<ScanRomFolderCommandHandler> logger)
    {
        _scannerService = scannerService;
        _romRepository = romRepository;
        _platformRepository = platformRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the command to scan a ROM folder.
    /// </summary>
    /// <param name="request">The scan ROM folder command with folder path.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the scan results or an error.</returns>
    public async Task<Result<ScanResult>> Handle(ScanRomFolderCommand request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Scanning ROM folder {FolderPath} for platform {Platform}",
                request.FolderPath, request.PlatformName);

            // Validate platform exists
            var platform = await _platformRepository.GetByNameAsync(request.PlatformName, ct).ConfigureAwait(false);
            if (platform is null)
                return Result<ScanResult>.Failure($"Platform '{request.PlatformName}' not found");

            // Scan the folder
            var progress = new Progress<ScanProgress>();
            var scanResults = new List<string>();
            var progressCallback = new Progress<ScanProgress>(p =>
            {
                scanResults.Add($"Scanned {p.FilesScanned}/{p.FilesTotal}: {p.CurrentFile}");
            });

            var romFiles = await _scannerService.ScanFolderAsync(
                request.FolderPath,
                (Guid)platform.Id,
                request.Recursive,
                progressCallback,
                ct).ConfigureAwait(false);

            // Save ROM files to database
            var savedCount = 0;
            var errors = new List<string>();

            foreach (var romFile in romFiles)
            {
                try
                {
                    // Note: RomFile.PlatformId is set in constructor, but we need to ensure it matches our platform
                    // For now, we'll accept the platform ID that was passed to the RomFile constructor

                    await _romRepository.AddAsync(romFile, ct).ConfigureAwait(false);
                    savedCount++;
                }
                catch (Exception ex)
                {
                    var error = $"Failed to save ROM {romFile.Title}: {ex.Message}";
                    errors.Add(error);
                    _logger.LogWarning(ex, error);
                }
            }

            var scanResult = new ScanResult
            {
                TotalFilesScanned = romFiles.Count,
                ValidRomsFound = savedCount,
                InvalidFilesSkipped = romFiles.Count - savedCount,
                ChecksumsCalculated = request.VerifyChecksums ? savedCount : 0,
                NewRomIds = romFiles.Select(r => RomFileId.From((Guid)r.Id)).ToArray(),
                Errors = errors.ToArray(),
                ScanDuration = TimeSpan.Zero // Would need to track actual duration
            };

            _logger.LogInformation("ROM scan completed: {ValidRoms} valid ROMs found, {Errors} errors",
                savedCount, errors.Count);

            return Result<ScanResult>.Success(scanResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error scanning ROM folder {FolderPath}", request.FolderPath);
            return Result<ScanResult>.Failure($"Failed to scan ROM folder: {ex.Message}");
        }
    }
}
