using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.RetroArch.Models;

namespace SaveState.Infrastructure.RetroArch.Services.RetroArch;

/// <summary>
/// Engine for managing save states.
/// </summary>
public partial class SaveStateEngine : ISaveStateEngine
{
    private readonly ILogger<SaveStateEngine> _logger;
    private readonly INetworkCommandEngine _networkCommand;

    public SaveStateEngine(
        ILogger<SaveStateEngine> logger,
        INetworkCommandEngine networkCommand)
    {
        _logger = logger;
        _networkCommand = networkCommand;
    }

    /// <inheritdoc />
    public async Task<Result<string>> CreateSaveStateAsync(int slot = -1, CancellationToken ct = default)
    {
        try
        {
            var isRunning = await _networkCommand.IsRunningAsync(ct);
            if (!isRunning.IsSuccess || !isRunning.Value)
            {
                return Result.Failure<string>("RetroArch is not currently running");
            }

            // Send SAVE_STATE command to RetroArch
            var command = slot >= 0 ? $"SAVE_STATE_SLOT {slot}\nSAVE_STATE" : "SAVE_STATE";
            var result = await _networkCommand.SendCommandAsync(command, ct);

            if (!result.IsSuccess)
            {
                return Result.Failure<string>($"Failed to create save state: {result.Error}");
            }

            // Note: The actual file path is determined by RetroArch based on its config
            // We return a success message with slot info
            var message = slot >= 0 ? $"Save state created in slot {slot}" : "Auto save state created";
            LogSaveStateCreated(_logger, message);
            return Result.Success(message);
        }
        catch (Exception ex)
        {
            LogCreateSaveStateError(_logger, ex);
            return Result.Failure<string>($"Error creating save state: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> LoadSaveStateAsync(int slot, CancellationToken ct = default)
    {
        try
        {
            var isRunning = await _networkCommand.IsRunningAsync(ct);
            if (!isRunning.IsSuccess || !isRunning.Value)
            {
                return Result.Failure("RetroArch is not currently running");
            }

            // Send LOAD_STATE command to RetroArch
            var command = $"SAVE_STATE_SLOT {slot}\nLOAD_STATE";
            var result = await _networkCommand.SendCommandAsync(command, ct);

            if (!result.IsSuccess)
            {
                return Result.Failure($"Failed to load save state: {result.Error}");
            }

            LogSaveStateLoaded(_logger, slot);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogLoadSaveStateError(_logger, ex);
            return Result.Failure($"Error loading save state: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> LoadSaveStateFromFileAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return Result.Failure($"Save state file not found: {filePath}");
            }

            var isRunning = await _networkCommand.IsRunningAsync(ct);
            if (!isRunning.IsSuccess || !isRunning.Value)
            {
                return Result.Failure("RetroArch is not currently running");
            }

            // Send LOAD_STATE command with file path to RetroArch
            var command = $"LOAD_STATE \"{filePath}\"";
            var result = await _networkCommand.SendCommandAsync(command, ct);

            if (!result.IsSuccess)
            {
                return Result.Failure($"Failed to load save state from file: {result.Error}");
            }

            LogSaveStateLoadedFromFile(_logger, filePath);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogLoadSaveStateError(_logger, ex);
            return Result.Failure($"Error loading save state from file: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<SaveStateInfo>>> GetSaveStatesAsync(string gamePath, CancellationToken ct = default)
    {
        try
        {
            var saveStates = new List<SaveStateInfo>();
            var gameName = Path.GetFileNameWithoutExtension(gamePath);
            var saveDir = Path.GetDirectoryName(gamePath) ?? "";

            // Look for save state files
            var stateFiles = Directory.GetFiles(saveDir, $"{gameName}*.state*");

            foreach (var file in stateFiles)
            {
                var fileInfo = new FileInfo(file);
                var fileName = Path.GetFileNameWithoutExtension(file);
                var isAutoSave = fileName.EndsWith(".auto");
                var slot = -1;

                // Try to parse slot number from filename (e.g., game.state1, game.state2)
                if (!isAutoSave && fileName.Length > gameName.Length + 5)
                {
                    var slotStr = fileName[(gameName.Length + 5)..];
                    _ = int.TryParse(slotStr, out slot);
                }

                saveStates.Add(new SaveStateInfo
                {
                    FilePath = file,
                    FileName = Path.GetFileName(file),
                    Slot = slot,
                    IsAutoSave = isAutoSave,
                    CreatedAt = fileInfo.CreationTimeUtc,
                    ModifiedAt = fileInfo.LastWriteTimeUtc,
                    FileSize = fileInfo.Length,
                    Format = SaveStateFormat.Standard
                });
            }

            return Task.FromResult(Result.Success<IReadOnlyList<SaveStateInfo>>(
                saveStates.OrderByDescending(s => s.ModifiedAt).ToList()));
        }
        catch (Exception ex)
        {
            LogGetSaveStatesError(_logger, ex);
            return Task.FromResult(Result.Failure<IReadOnlyList<SaveStateInfo>>($"Error getting save states: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result> DeleteSaveStateAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return Task.FromResult(Result.Failure($"Save state file not found: {filePath}"));
            }

            File.Delete(filePath);

            // Also delete associated screenshot if exists
            var screenshotPath = Path.ChangeExtension(filePath, ".png");
            if (File.Exists(screenshotPath))
            {
                File.Delete(screenshotPath);
            }

            LogSaveStateDeleted(_logger, filePath);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            LogDeleteSaveStateError(_logger, filePath, ex);
            return Task.FromResult(Result.Failure($"Error deleting save state: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> CaptureScreenshotAsync(string? screenshotDirectory, CancellationToken ct = default)
    {
        try
        {
            var isRunning = await _networkCommand.IsRunningAsync(ct);
            if (!isRunning.IsSuccess || !isRunning.Value)
            {
                return Result.Failure<string>("RetroArch is not currently running");
            }

            // Send SCREENSHOT command to RetroArch
            var result = await _networkCommand.SendCommandAsync("SCREENSHOT", ct);

            if (!result.IsSuccess)
            {
                return Result.Failure<string>($"Failed to capture screenshot: {result.Error}");
            }

            // Try to get the most recent screenshot
            if (!string.IsNullOrEmpty(screenshotDirectory) && Directory.Exists(screenshotDirectory))
            {
                var recentScreenshot = await GetMostRecentScreenshotAsync(screenshotDirectory, ct);
                if (recentScreenshot.IsSuccess && recentScreenshot.Value != null)
                {
                    LogScreenshotCaptured(_logger, recentScreenshot.Value);
                    return Result.Success(recentScreenshot.Value);
                }
            }

            LogScreenshotCaptured(_logger, "Screenshot saved (path unknown)");
            return Result.Success("Screenshot captured successfully");
        }
        catch (Exception ex)
        {
            LogCaptureScreenshotError(_logger, ex);
            return Result.Failure<string>($"Error capturing screenshot: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Task<Result<string?>> GetMostRecentScreenshotAsync(string screenshotDirectory, CancellationToken ct = default)
    {
        try
        {
            if (!Directory.Exists(screenshotDirectory))
            {
                return Task.FromResult(Result.Success<string?>(null));
            }

            var files = Directory.GetFiles(screenshotDirectory, "*.png")
                .OrderByDescending(f => File.GetCreationTimeUtc(f))
                .FirstOrDefault();

            return Task.FromResult(Result.Success<string?>(files));
        }
        catch (Exception ex)
        {
            LogGetScreenshotError(_logger, ex);
            return Task.FromResult(Result.Success<string?>(null));
        }
    }

    #region Logging

    [LoggerMessage(EventId = 501, Level = LogLevel.Information, Message = "Save state created: {FilePath}")]
    static partial void LogSaveStateCreated(ILogger logger, string filePath);

    [LoggerMessage(EventId = 502, Level = LogLevel.Error, Message = "Error creating save state")]
    static partial void LogCreateSaveStateError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 503, Level = LogLevel.Information, Message = "Save state loaded from slot: {Slot}")]
    static partial void LogSaveStateLoaded(ILogger logger, int slot);

    [LoggerMessage(EventId = 504, Level = LogLevel.Error, Message = "Error loading save state")]
    static partial void LogLoadSaveStateError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 505, Level = LogLevel.Information, Message = "Save state loaded from file: {FilePath}")]
    static partial void LogSaveStateLoadedFromFile(ILogger logger, string filePath);

    [LoggerMessage(EventId = 506, Level = LogLevel.Error, Message = "Error getting save states")]
    static partial void LogGetSaveStatesError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 507, Level = LogLevel.Information, Message = "Save state deleted: {FilePath}")]
    static partial void LogSaveStateDeleted(ILogger logger, string filePath);

    [LoggerMessage(EventId = 508, Level = LogLevel.Error, Message = "Error deleting save state: {FilePath}")]
    static partial void LogDeleteSaveStateError(ILogger logger, string filePath, Exception ex);

    [LoggerMessage(EventId = 509, Level = LogLevel.Information, Message = "Screenshot captured: {FilePath}")]
    static partial void LogScreenshotCaptured(ILogger logger, string filePath);

    [LoggerMessage(EventId = 510, Level = LogLevel.Error, Message = "Error capturing screenshot")]
    static partial void LogCaptureScreenshotError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 511, Level = LogLevel.Debug, Message = "Error getting screenshot")]
    static partial void LogGetScreenshotError(ILogger logger, Exception ex);

    #endregion
}
