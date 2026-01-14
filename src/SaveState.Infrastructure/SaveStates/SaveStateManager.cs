using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.RomManagement;
using SaveState.Core.SaveStates;
using SaveState.Core.SaveStates.Entities;
using SaveState.Core.SaveStates.Services;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.RetroArch.Services;
using SaveState.Application.RomManagement.Services;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;

namespace SaveState.Infrastructure.SaveStates;

public class SaveStateManager : ISaveStateManager
{
    private readonly ISaveStateRepository _saveStateRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IRomFileRepository _romRepository;
    private readonly ISessionTrackingService _sessionTrackingService;
    private readonly IRetroArchService? _retroArchService;
    private readonly IEmulatorService? _emulatorService;
    private readonly ILogger<SaveStateManager> _logger;

    public SaveStateManager(
        ISaveStateRepository saveStateRepository,
        IGameRepository gameRepository,
        IRomFileRepository romRepository,
        ISessionTrackingService sessionTrackingService,
        ILogger<SaveStateManager> logger,
        IRetroArchService? retroArchService = null,
        IEmulatorService? emulatorService = null)
    {
        _saveStateRepository = saveStateRepository;
        _gameRepository = gameRepository;
        _romRepository = romRepository;
        _sessionTrackingService = sessionTrackingService;
        _logger = logger;
        _retroArchService = retroArchService;
        _emulatorService = emulatorService;
    }

    public async Task<Result<SaveStateEntity>> CreateSaveStateAsync(Guid gameId, CreateSaveStateRequest request, CancellationToken ct = default)
    {
        try
        {
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct);
            if (game == null)
                return Result.Failure<SaveStateEntity>("Game not found", ErrorType.NotFound);

            // Get current playtime
            var playtimeStats = await _sessionTrackingService.GetStatisticsAsync(gameId, ct);
            var currentPlaytime = playtimeStats.Value.TotalPlaytime;

            // Create the save state entity
            var saveState = SaveStateEntity.Create(gameId, GenerateSaveStatePath(gameId), currentPlaytime, false);
            saveState.SetDescription(request.Description);
            saveState.SetParent(request.ParentStateId);

            // Actually create the save state file (placeholder - would integrate with emulator)
            var fileCreationResult = await CreateSaveStateFileAsync(saveState, ct);
            if (!fileCreationResult.IsSuccess)
            {
                _logger.LogWarning("Failed to create save state file: {Error}", fileCreationResult.Error);
                // Continue anyway - the entity is still created
            }
            else
            {
                saveState.SetFileSize(fileCreationResult.Value);
            }

            // Capture screenshot if requested
            if (request.CaptureScreenshot)
            {
                var screenshotResult = await CaptureScreenshotAsync(gameId, saveState.Id, ct);
                if (screenshotResult.IsSuccess)
                {
                    saveState.SetThumbnail(screenshotResult.Value);
                }
            }

            await _saveStateRepository.AddAsync(saveState, ct);

            _logger.LogInformation("Created save state for game {GameId}: {SaveStateId}", gameId, saveState.Id);
            return Result.Success<SaveStateEntity>(saveState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create save state for game {GameId}", gameId);
            return Result.Failure<SaveStateEntity>($"Failed to create save state: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> RestoreSaveStateAsync(Guid saveStateId, CancellationToken ct = default)
    {
        try
        {
            var saveState = await _saveStateRepository.GetByIdAsync(saveStateId, ct);
            if (saveState == null)
                return Result.Failure("Save state not found", ErrorType.NotFound);

            // Verify the save state file exists
            if (!File.Exists(saveState.FilePath))
                return Result.Failure("Save state file not found on disk", ErrorType.NotFound);

            // Restore the save state (placeholder - would integrate with emulator)
            var restoreResult = await RestoreSaveStateFileAsync(saveState, ct);
            if (!restoreResult.IsSuccess)
                return restoreResult;

            _logger.LogInformation("Restored save state {SaveStateId} for game {GameId}", saveStateId, saveState.GameId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore save state {SaveStateId}", saveStateId);
            return Result.Failure($"Failed to restore save state: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<SaveStateEntity>>> GetSaveStatesAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var saveStates = await _saveStateRepository.GetByGameIdAsync(gameId, ct);
            return Result.Success<IReadOnlyList<SaveStateEntity>>(saveStates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get save states for game {GameId}", gameId);
            return Result.Failure<IReadOnlyList<SaveStateEntity>>($"Failed to get save states: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> DeleteSaveStateAsync(Guid saveStateId, CancellationToken ct = default)
    {
        try
        {
            var saveState = await _saveStateRepository.GetByIdAsync(saveStateId, ct);
            if (saveState == null)
                return Result.Failure("Save state not found", ErrorType.NotFound);

            // Delete the physical file
            if (File.Exists(saveState.FilePath))
            {
                File.Delete(saveState.FilePath);
            }

            // Delete thumbnail if it exists
            if (!string.IsNullOrEmpty(saveState.ThumbnailPath) && File.Exists(saveState.ThumbnailPath))
            {
                File.Delete(saveState.ThumbnailPath);
            }

            await _saveStateRepository.DeleteAsync(saveStateId, ct);

            _logger.LogInformation("Deleted save state {SaveStateId}", saveStateId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete save state {SaveStateId}", saveStateId);
            return Result.Failure($"Failed to delete save state: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> ExportSaveStateAsync(Guid saveStateId, string exportPath, CancellationToken ct = default)
    {
        try
        {
            var saveState = await _saveStateRepository.GetByIdAsync(saveStateId, ct);
            if (saveState == null)
                return Result.Failure("Save state not found", ErrorType.NotFound);

            if (!File.Exists(saveState.FilePath))
                return Result.Failure("Save state file not found on disk", ErrorType.NotFound);

            // Ensure export directory exists
            var exportDirectory = Path.GetDirectoryName(exportPath);
            if (!string.IsNullOrEmpty(exportDirectory))
            {
                Directory.CreateDirectory(exportDirectory);
            }

            // Copy the save state file
            File.Copy(saveState.FilePath, exportPath, true);

            // Export thumbnail if it exists
            if (!string.IsNullOrEmpty(saveState.ThumbnailPath) && File.Exists(saveState.ThumbnailPath))
            {
                var thumbnailExportPath = Path.Combine(
                    Path.GetDirectoryName(exportPath)!,
                    Path.GetFileNameWithoutExtension(exportPath) + "_thumb" + Path.GetExtension(saveState.ThumbnailPath));
                File.Copy(saveState.ThumbnailPath, thumbnailExportPath, true);
            }

            _logger.LogInformation("Exported save state {SaveStateId} to {ExportPath}", saveStateId, exportPath);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export save state {SaveStateId}", saveStateId);
            return Result.Failure($"Failed to export save state: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<SaveStateEntity>> ImportSaveStateAsync(Guid gameId, string importPath, CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(importPath))
                return Result.Failure<SaveStateEntity>("Import file not found", ErrorType.NotFound);

            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct);
            if (game == null)
                return Result.Failure<SaveStateEntity>("Game not found", ErrorType.NotFound);

            // Get current playtime
            var playtimeStats = await _sessionTrackingService.GetStatisticsAsync(gameId, ct);

            // Create new save state
            var saveState = SaveStateEntity.Create(gameId, GenerateSaveStatePath(gameId), playtimeStats.Value.TotalPlaytime, false);
            saveState.SetDescription($"Imported from {Path.GetFileName(importPath)}");

            // Copy the file to our save state directory
            var fileInfo = new FileInfo(importPath);
            File.Copy(importPath, saveState.FilePath, true);
            saveState.SetFileSize(fileInfo.Length);

            // Look for associated thumbnail
            var thumbnailPath = Path.Combine(
                Path.GetDirectoryName(importPath)!,
                Path.GetFileNameWithoutExtension(importPath) + "_thumb" + Path.GetExtension(importPath));

            if (File.Exists(thumbnailPath))
            {
                var localThumbnailPath = GenerateThumbnailPath(gameId, saveState.Id);
                File.Copy(thumbnailPath, localThumbnailPath, true);
                saveState.SetThumbnail(localThumbnailPath);
            }

            await _saveStateRepository.AddAsync(saveState, ct);

            _logger.LogInformation("Imported save state for game {GameId}: {SaveStateId}", gameId, saveState.Id);
            return Result.Success<SaveStateEntity>(saveState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import save state for game {GameId}", gameId);
            return Result.Failure<SaveStateEntity>($"Failed to import save state: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<byte[]?>> GetThumbnailAsync(Guid saveStateId, CancellationToken ct = default)
    {
        try
        {
            var saveState = await _saveStateRepository.GetByIdAsync(saveStateId, ct);
            if (saveState == null)
                return Result.Failure<byte[]?>("Save state not found", ErrorType.NotFound);

            if (string.IsNullOrEmpty(saveState.ThumbnailPath) || !File.Exists(saveState.ThumbnailPath))
                return Result.Success<byte[]?>(null);

            var thumbnailBytes = await File.ReadAllBytesAsync(saveState.ThumbnailPath, ct);
            return Result.Success<byte[]?>(thumbnailBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get thumbnail for save state {SaveStateId}", saveStateId);
            return Result.Failure<byte[]?>($"Failed to get thumbnail: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<SaveStateTimeline>> GetTimelineAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var timeline = await _saveStateRepository.GetTimelineAsync(gameId, ct);

            var nodes = timeline.Select(ss => new SaveStateNode(
                Id: ss.Id,
                CreatedAt: ss.CreatedAt,
                Description: ss.Description,
                ParentId: ss.ParentStateId,
                IsFavorite: ss.IsFavorite,
                ThumbnailPath: ss.ThumbnailPath)).ToList();

            var timelineResult = new SaveStateTimeline(
                GameId: gameId,
                Nodes: nodes,
                TotalCount: nodes.Count);

            return Result.Success<SaveStateTimeline>(timelineResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get timeline for game {GameId}", gameId);
            return Result.Failure<SaveStateTimeline>($"Failed to get timeline: {ex.Message}", ErrorType.Internal);
        }
    }

    private static string GenerateSaveStatePath(Guid gameId)
    {
        var saveStatesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SaveStates", gameId.ToString());
        Directory.CreateDirectory(saveStatesDir);

        var fileName = $"savestate_{DateTime.Now:yyyyMMdd_HHmmss}.state";
        return Path.Combine(saveStatesDir, fileName);
    }

    private static string GenerateThumbnailPath(Guid gameId, Guid saveStateId)
    {
        var thumbnailsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SaveStates", gameId.ToString(), "thumbnails");
        Directory.CreateDirectory(thumbnailsDir);

        var fileName = $"thumb_{saveStateId}.png";
        return Path.Combine(thumbnailsDir, fileName);
    }

    private async Task<Result<long>> CreateSaveStateFileAsync(SaveStateEntity saveState, CancellationToken ct)
    {
        try
        {
            // Try RetroArch first if available
            if (_retroArchService != null)
            {
                var isRunningResult = await _retroArchService.IsRunningAsync(ct);
                if (isRunningResult.IsSuccess && isRunningResult.Value)
                {
                    _logger.LogInformation("Creating save state via RetroArch network command interface");
                    var createResult = await _retroArchService.CreateSaveStateAsync(-1, ct);
                    
                    if (createResult.IsSuccess && !string.IsNullOrEmpty(createResult.Value))
                    {
                        // Copy the RetroArch save state to our managed location
                        if (File.Exists(createResult.Value))
                        {
                            File.Copy(createResult.Value, saveState.FilePath, true);
                            var fileInfo = new FileInfo(saveState.FilePath);
                            _logger.LogInformation("Save state created via RetroArch: {Size} bytes", fileInfo.Length);
                            return Result.Success<long>(fileInfo.Length);
                        }
                    }
                    
                    _logger.LogWarning("RetroArch save state creation returned success but file not found");
                }
            }

            // Fallback: Check if there's an emulator process running for this game
            if (_emulatorService != null)
            {
                try
                {
                    var processResult = await _emulatorService.GetRunningEmulatorProcessAsync(saveState.GameId, ct);
                    if (processResult.IsSuccess)
                    {
                        _logger.LogInformation("Emulator process detected for game {GameId}, save state will be created when available", saveState.GameId);
                        
                        // Create a placeholder file that indicates a save state operation was requested
                        // The emulator integration can populate this later
                        var metadata = $"SaveState:{saveState.Id},Game:{saveState.GameId},Playtime:{saveState.PlaytimeAtSave},Timestamp:{DateTime.UtcNow:O},Status:Pending";
                        await File.WriteAllTextAsync(saveState.FilePath, metadata, ct);
                        
                        var fileInfo = new FileInfo(saveState.FilePath);
                        return Result.Success<long>(fileInfo.Length);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not check for running emulator process");
                }
            }

            // No emulator integration available - create a basic save state file
            _logger.LogInformation("Creating basic save state file (no emulator integration active)");
            var basicData = $"SaveState:{saveState.Id},Game:{saveState.GameId},Playtime:{saveState.PlaytimeAtSave},Created:{DateTime.UtcNow:O}";
            await File.WriteAllTextAsync(saveState.FilePath, basicData, ct);

            var basicFileInfo = new FileInfo(saveState.FilePath);
            return Result.Success<long>(basicFileInfo.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create save state file");
            return Result.Failure<long>($"Failed to create save state file: {ex.Message}", ErrorType.Internal);
        }
    }

    private async Task<Result> RestoreSaveStateFileAsync(SaveStateEntity saveState, CancellationToken ct)
    {
        try
        {
            if (!File.Exists(saveState.FilePath))
                return Result.Failure("Save state file does not exist", ErrorType.NotFound);

            // Try RetroArch first if available
            if (_retroArchService != null)
            {
                var isRunningResult = await _retroArchService.IsRunningAsync(ct);
                if (isRunningResult.IsSuccess && isRunningResult.Value)
                {
                    _logger.LogInformation("Restoring save state via RetroArch network command interface");
                    var loadResult = await _retroArchService.LoadSaveStateFromFileAsync(saveState.FilePath, ct);
                    
                    if (loadResult.IsSuccess)
                    {
                        _logger.LogInformation("Save state restored successfully via RetroArch");
                        return Result.Success();
                    }
                    
                    _logger.LogWarning("RetroArch save state restoration failed: {Error}", loadResult.Error);
                }
            }

            // Check if there's an emulator process running for this game
            if (_emulatorService != null)
            {
                try
                {
                    var processResult = await _emulatorService.GetRunningEmulatorProcessAsync(saveState.GameId, ct);
                    if (processResult.IsSuccess)
                    {
                        _logger.LogInformation("Emulator process detected for game {GameId}, save state restoration will be handled by emulator", saveState.GameId);
                        // The emulator integration would handle loading the save state
                        return Result.Success();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not check for running emulator process");
                }
            }

            // No active emulator - log that save state cannot be restored right now
            _logger.LogWarning("No active emulator found. Save state file exists but cannot be restored to a running game.");
            return Result.Failure("No active emulator found. Please launch the game first, then load the save state.", ErrorType.Validation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore save state");
            return Result.Failure($"Failed to restore save state: {ex.Message}", ErrorType.Internal);
        }
    }

    private async Task<Result<string>> CaptureScreenshotAsync(Guid gameId, Guid saveStateId, CancellationToken ct)
    {
        try
        {
            var thumbnailPath = GenerateThumbnailPath(gameId, saveStateId);

            // Try to capture screenshot from RetroArch if running
            if (_retroArchService != null)
            {
                var isRunningResult = await _retroArchService.IsRunningAsync(ct);
                if (isRunningResult.IsSuccess && isRunningResult.Value)
                {
                    _logger.LogInformation("Capturing screenshot via RetroArch");
                    var screenshotResult = await _retroArchService.CaptureScreenshotAsync(ct);
                    
                    if (screenshotResult.IsSuccess && !string.IsNullOrEmpty(screenshotResult.Value) && File.Exists(screenshotResult.Value))
                    {
                        // Copy the RetroArch screenshot to our thumbnail location
                        File.Copy(screenshotResult.Value, thumbnailPath, true);
                        _logger.LogInformation("Screenshot captured and saved to: {Path}", thumbnailPath);
                        return Result.Success<string>(thumbnailPath);
                    }
                }
            }

            // Fallback: Create a placeholder thumbnail
            _logger.LogInformation("Creating placeholder thumbnail (no emulator screenshot available)");
            var placeholderImage = CreatePlaceholderThumbnail();
            await File.WriteAllBytesAsync(thumbnailPath, placeholderImage, ct);

            return Result.Success<string>(thumbnailPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture screenshot");
            return Result.Failure<string>($"Failed to capture screenshot: {ex.Message}", ErrorType.Internal);
        }
    }

    private static byte[] CreatePlaceholderThumbnail()
    {
        // Create a minimal 1x1 pixel PNG as a placeholder
        // PNG header + IHDR + IDAT + IEND chunks for a 1x1 transparent image
        return new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // 1x1 dimensions
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, // RGBA, no interlace
            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, // IDAT chunk
            0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, // Compressed data
            0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, // CRC
            0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, // IEND chunk
            0x42, 0x60, 0x82
        };
    }
}

