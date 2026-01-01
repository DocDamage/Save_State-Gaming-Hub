using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.RomManagement;
using SaveState.Core.SaveStates;
using SaveState.Core.SaveStates.Entities;
using SaveState.Core.SaveStates.Services;
using SaveState.Core.GameLibrary.Services;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;

namespace SaveState.Infrastructure.SaveStates;

public class SaveStateManager : ISaveStateManager
{
    private readonly ISaveStateRepository _saveStateRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IRomFileRepository _romRepository;
    private readonly ISessionTrackingService _sessionTrackingService;
    private readonly ILogger<SaveStateManager> _logger;

    public SaveStateManager(
        ISaveStateRepository saveStateRepository,
        IGameRepository gameRepository,
        IRomFileRepository romRepository,
        ISessionTrackingService sessionTrackingService,
        ILogger<SaveStateManager> logger)
    {
        _saveStateRepository = saveStateRepository;
        _gameRepository = gameRepository;
        _romRepository = romRepository;
        _sessionTrackingService = sessionTrackingService;
        _logger = logger;
    }

    public async Task<Result<SaveStateEntity>> CreateSaveStateAsync(Guid gameId, CreateSaveStateRequest request, CancellationToken ct = default)
    {
        try
        {
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct);
            if (game == null)
                return Result<SaveStateEntity>.Failure("Game not found", ErrorType.NotFound);

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
            return Result<SaveStateEntity>.Success(saveState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create save state for game {GameId}", gameId);
            return Result<SaveStateEntity>.Failure($"Failed to create save state: {ex.Message}", ErrorType.Internal);
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
            return Result<IReadOnlyList<SaveStateEntity>>.Success(saveStates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get save states for game {GameId}", gameId);
            return Result<IReadOnlyList<SaveStateEntity>>.Failure($"Failed to get save states: {ex.Message}", ErrorType.Internal);
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
                return Result<SaveStateEntity>.Failure("Import file not found", ErrorType.NotFound);

            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct);
            if (game == null)
                return Result<SaveStateEntity>.Failure("Game not found", ErrorType.NotFound);

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
            return Result<SaveStateEntity>.Success(saveState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import save state for game {GameId}", gameId);
            return Result<SaveStateEntity>.Failure($"Failed to import save state: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<byte[]?>> GetThumbnailAsync(Guid saveStateId, CancellationToken ct = default)
    {
        try
        {
            var saveState = await _saveStateRepository.GetByIdAsync(saveStateId, ct);
            if (saveState == null)
                return Result<byte[]?>.Failure("Save state not found", ErrorType.NotFound);

            if (string.IsNullOrEmpty(saveState.ThumbnailPath) || !File.Exists(saveState.ThumbnailPath))
                return Result<byte[]?>.Success(null);

            var thumbnailBytes = await File.ReadAllBytesAsync(saveState.ThumbnailPath, ct);
            return Result<byte[]?>.Success(thumbnailBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get thumbnail for save state {SaveStateId}", saveStateId);
            return Result<byte[]?>.Failure($"Failed to get thumbnail: {ex.Message}", ErrorType.Internal);
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

            return Result<SaveStateTimeline>.Success(timelineResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get timeline for game {GameId}", gameId);
            return Result<SaveStateTimeline>.Failure($"Failed to get timeline: {ex.Message}", ErrorType.Internal);
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

    private static Task<Result<long>> CreateSaveStateFileAsync(SaveStateEntity saveState, CancellationToken ct)
    {
        // Placeholder implementation - would integrate with actual emulator save state creation
        try
        {
            // Create a dummy file for now
            var dummyData = $"SaveState:{saveState.Id},Game:{saveState.GameId},Playtime:{saveState.PlaytimeAtSave}";
            File.WriteAllText(saveState.FilePath, dummyData);

            var fileInfo = new FileInfo(saveState.FilePath);
            return Task.FromResult(Result<long>.Success(fileInfo.Length));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<long>.Failure($"Failed to create save state file: {ex.Message}", ErrorType.Internal));
        }
    }

    private static Task<Result> RestoreSaveStateFileAsync(SaveStateEntity saveState, CancellationToken ct)
    {
        // Placeholder implementation - would integrate with actual emulator save state restoration
        try
        {
            if (!File.Exists(saveState.FilePath))
                return Task.FromResult(Result.Failure("Save state file does not exist", ErrorType.NotFound));

            // In real implementation, this would load the save state into the emulator
            // Logger access removed for static method
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure($"Failed to restore save state: {ex.Message}", ErrorType.Internal));
        }
    }

    private static Task<Result<string>> CaptureScreenshotAsync(Guid gameId, Guid saveStateId, CancellationToken ct)
    {
        // Placeholder implementation - would capture screenshot from running game
        try
        {
            var thumbnailPath = GenerateThumbnailPath(gameId, saveStateId);

            // Create a dummy thumbnail file for now
            var dummyImageBytes = new byte[1024]; // Small dummy image
            File.WriteAllBytes(thumbnailPath, dummyImageBytes);

            return Task.FromResult(Result<string>.Success(thumbnailPath));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<string>.Failure($"Failed to capture screenshot: {ex.Message}", ErrorType.Internal));
        }
    }
}