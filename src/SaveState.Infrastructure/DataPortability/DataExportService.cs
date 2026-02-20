using System.IO.Compression;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.DataPortability;
using SaveState.Core.GameLibrary;
using SaveState.Infrastructure.Persistence;

using SaveState.Core.Analytics.Services;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Infrastructure.DataPortability;

/// <summary>
/// Implementation of data export service for creating backups and portable exports.
/// </summary>
public partial class DataExportService : IDataExportService
{
    private readonly IGameRepository _gameRepository;
    private readonly SaveStateDbContext _dbContext;
    private readonly ICompletionPredictionService _predictionService;
    private readonly ILogger<DataExportService> _logger;
    private readonly ITimeProvider _timeProvider;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DataExportService(
        IGameRepository gameRepository,
        SaveStateDbContext dbContext,
        ICompletionPredictionService predictionService,
        ILogger<DataExportService> logger,
        ITimeProvider timeProvider)
    {
        _gameRepository = gameRepository;
        _dbContext = dbContext;
        _predictionService = predictionService;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    #region LoggerMessage Definitions
    [LoggerMessage(Level = LogLevel.Information, Message = "Exporting game library to {FilePath}")]
    private static partial void LogExportingGameLibrary(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully exported {Count} games to {FilePath}")]
    private static partial void LogExportGameLibrarySuccess(ILogger logger, int count, string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to export game library")]
    private static partial void LogExportGameLibraryFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Exporting user settings to {FilePath}")]
    private static partial void LogExportingUserSettings(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully exported user settings to {FilePath}")]
    private static partial void LogExportUserSettingsSuccess(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to export user settings")]
    private static partial void LogExportUserSettingsFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Exporting save file metadata to {FilePath}")]
    private static partial void LogExportingSaveFileMetadata(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully exported {Count} save file entries to {FilePath}")]
    private static partial void LogExportSaveFileMetadataSuccess(ILogger logger, int count, string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to export save file metadata")]
    private static partial void LogExportSaveFileMetadataFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Exporting achievements to {FilePath}")]
    private static partial void LogExportingAchievements(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully exported {Count} achievements to {FilePath}")]
    private static partial void LogExportAchievementsSuccess(ILogger logger, int count, string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to export achievements")]
    private static partial void LogExportAchievementsFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Exporting session history to {FilePath}")]
    private static partial void LogExportingSessionHistory(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully exported {Count} sessions to {FilePath}")]
    private static partial void LogExportSessionHistorySuccess(ILogger logger, int count, string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to export session history")]
    private static partial void LogExportSessionHistoryFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating full backup at {OutputPath}")]
    private static partial void LogCreatingFullBackup(ILogger logger, string outputPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully created backup: {FilePath} ({Size:N0} bytes)")]
    private static partial void LogFullBackupSuccess(ILogger logger, string filePath, long size);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to create full backup")]
    private static partial void LogFullBackupFailed(ILogger logger, Exception ex);
    #endregion

    public async Task<Result<string>> ExportGameLibraryAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            LogExportingGameLibrary(_logger, filePath);

            var games = await _gameRepository.GetAllAsync(ct);
            var predictions = new Dictionary<Guid, object>();
            foreach (var game in games)
            {
                if (game.Status == GameStatus.Running || (game.Status == GameStatus.Installed && game.TotalPlayTime.TotalHours > 2))
                {
                    try
                    {
                        var predictionResult = await _predictionService.GetPredictionForGameAsync(GameId.From(game.Id), ct);
                        if (predictionResult.IsSuccess)
                        {
                            predictions[game.Id] = new
                            {
                                EstimatedRemainingHours = predictionResult.Value.EstimatedTimeRemaining.TotalHours,
                                predictionResult.Value.ConfidenceScore,
                                Factors = predictionResult.Value.ReasoningFactors
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        // Prediction failures shouldn't fail the export, but we log them for diagnostics
                        _logger.LogDebug(ex, "Prediction failed for game {GameId} during export, continuing without prediction", game.Id);
                    }
                }
            }

            var exportData = new
            {
                ExportVersion = "1.0",
                ExportedAt = _timeProvider.UtcNow,
                TotalGames = games.Count,
                Games = games.Select(g => new
                {
                    g.Id,
                    g.Title,
                    g.Description,
                    g.CoverImagePath,
                    g.InstallPath,
                    Status = g.Status.ToString(),
                    g.PlatformId,
                    PlatformName = g.Platform?.Name,
                    g.Source,
                    g.SourceId,
                    g.CreatedAt,
                    g.UpdatedAt,
                    g.LastPlayedAt,
                    TotalPlayTime = g.TotalPlayTime.ToString(),
                    g.ReleaseDate,
                    g.UserRating,
                    Tags = g.Tags.ToList(),
                    Genres = g.Genres.Select(gen => gen.Name).ToList(),
                    CompletionPrediction = predictions.TryGetValue(g.Id, out var pred) ? pred : null
                }).ToList()
            };

            var json = JsonSerializer.Serialize(exportData, JsonOptions);
            await File.WriteAllTextAsync(filePath, json, ct);

            LogExportGameLibrarySuccess(_logger, games.Count, filePath);
            return Result.Success(filePath);
        }
        catch (Exception ex)
        {
            LogExportGameLibraryFailed(_logger, ex);
            return Result.Failure<string>($"Export failed: {ex.Message}");
        }
    }

    public async Task<Result<string>> ExportUserSettingsAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            LogExportingUserSettings(_logger, filePath);

            // Read current appsettings.json
            var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

            if (!File.Exists(appSettingsPath))
            {
                return Result.Failure<string>("appsettings.json not found");
            }

            var currentJson = await File.ReadAllTextAsync(appSettingsPath, ct);
            using var document = JsonDocument.Parse(currentJson);
            var root = document.RootElement;

            // Extract all settings sections
            var settings = new Dictionary<string, object?>();
            var settingsSections = new[] { "Database", "OpenAI", "Groq", "Steam", "GOG", "Epic",
                "IGDB", "SteamGridDB", "Resilience", "Mugen", "RomScanning", "RetroArch", "Logging" };

            foreach (var section in settingsSections)
            {
                if (root.TryGetProperty(section, out var sectionElement))
                {
                    settings[section] = JsonSerializer.Deserialize<object>(sectionElement.GetRawText());
                }
            }

            // Create export data structure
            var exportData = new
            {
                ExportVersion = "1.0",
                ExportedAt = _timeProvider.UtcNow,
                Settings = settings
            };

            var json = JsonSerializer.Serialize(exportData, JsonOptions);
            await File.WriteAllTextAsync(filePath, json, ct);

            LogExportUserSettingsSuccess(_logger, filePath);
            return Result.Success(filePath);
        }
        catch (Exception ex)
        {
            LogExportUserSettingsFailed(_logger, ex);
            return Result.Failure<string>($"Export failed: {ex.Message}");
        }
    }

    public async Task<Result<string>> ExportSaveFileMetadataAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            LogExportingSaveFileMetadata(_logger, filePath);

            var saveStates = await _dbContext.SaveStates.ToListAsync(ct);
            var exportData = new
            {
                ExportVersion = "1.0",
                ExportedAt = _timeProvider.UtcNow,
                TotalSaveFiles = saveStates.Count,
                SaveFiles = saveStates.Select(sf => new
                {
                    sf.Id,
                    sf.GameId,
                    sf.FilePath,
                    sf.Description,
                    sf.CreatedAt,
                    sf.ThumbnailPath,
                    PlaytimeAtSave = sf.PlaytimeAtSave.ToString(),
                    sf.IsFavorite,
                    sf.IsAutoSave
                }).ToList()
            };

            var json = JsonSerializer.Serialize(exportData, JsonOptions);
            await File.WriteAllTextAsync(filePath, json, ct);

            LogExportSaveFileMetadataSuccess(_logger, saveStates.Count, filePath);
            return Result.Success(filePath);
        }
        catch (Exception ex)
        {
            LogExportSaveFileMetadataFailed(_logger, ex);
            return Result.Failure<string>($"Export failed: {ex.Message}");
        }
    }

    public async Task<Result<string>> ExportAchievementsAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            LogExportingAchievements(_logger, filePath);

            var userAchievements = await _dbContext.UserAchievements
                .Include(ua => ua.Achievement)
                .ToListAsync(ct);

            var exportData = new
            {
                ExportVersion = "1.0",
                ExportedAt = _timeProvider.UtcNow,
                TotalAchievements = userAchievements.Count,
                Achievements = userAchievements.Select(ua => new
                {
                    ua.Id,
                    ua.UserId,
                    ua.AchievementId,
                    AchievementName = ua.Achievement?.Name,
                    AchievementDescription = ua.Achievement?.Description,
                    ua.CurrentProgress,
                    ua.TargetProgress,
                    ua.IsUnlocked,
                    ua.UnlockedAt,
                    IconPath = ua.Achievement?.IconPath,
                    Points = ua.Achievement?.Points ?? 0
                }).ToList()
            };

            var json = JsonSerializer.Serialize(exportData, JsonOptions);
            await File.WriteAllTextAsync(filePath, json, ct);

            LogExportAchievementsSuccess(_logger, userAchievements.Count, filePath);
            return Result.Success(filePath);
        }
        catch (Exception ex)
        {
            LogExportAchievementsFailed(_logger, ex);
            return Result.Failure<string>($"Export failed: {ex.Message}");
        }
    }

    public async Task<Result<string>> ExportSessionHistoryAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            LogExportingSessionHistory(_logger, filePath);

            var sessions = await _dbContext.GameSessions
                .Include(s => s.Game)
                .OrderByDescending(s => s.StartedAt)
                .ToListAsync(ct);

            var exportData = new
            {
                ExportVersion = "1.0",
                ExportedAt = _timeProvider.UtcNow,
                TotalSessions = sessions.Count,
                Sessions = sessions.Select(s => new
                {
                    s.Id,
                    s.GameId,
                    GameTitle = s.Game?.Title,
                    s.StartedAt,
                    s.EndedAt,
                    Duration = s.GetDuration().ToString(),
                    s.Notes,
                    EndReason = s.EndReason?.ToString()
                }).ToList()
            };

            var json = JsonSerializer.Serialize(exportData, JsonOptions);
            await File.WriteAllTextAsync(filePath, json, ct);

            LogExportSessionHistorySuccess(_logger, sessions.Count, filePath);
            return Result.Success(filePath);
        }
        catch (Exception ex)
        {
            LogExportSessionHistoryFailed(_logger, ex);
            return Result.Failure<string>($"Export failed: {ex.Message}");
        }
    }

    public async Task<Result<string>> CreateFullBackupAsync(string outputPath, bool includeActualSaveFiles = false, CancellationToken ct = default)
    {
        try
        {
            LogCreatingFullBackup(_logger, outputPath);

            // Create a temporary directory for export files
            var tempDir = Path.Combine(Path.GetTempPath(), $"SaveState_Backup_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Export all data types to temporary directory
                var gameLibraryPath = Path.Combine(tempDir, "game_library.json");
                var settingsPath = Path.Combine(tempDir, "user_settings.json");
                var saveFilesPath = Path.Combine(tempDir, "save_files.json");
                var achievementsPath = Path.Combine(tempDir, "achievements.json");
                var sessionsPath = Path.Combine(tempDir, "sessions.json");

                await ExportGameLibraryAsync(gameLibraryPath, ct);
                await ExportUserSettingsAsync(settingsPath, ct);
                await ExportSaveFileMetadataAsync(saveFilesPath, ct);
                await ExportAchievementsAsync(achievementsPath, ct);
                await ExportSessionHistoryAsync(sessionsPath, ct);

                // Create backup manifest
                var manifest = new
                {
                    BackupVersion = "1.0",
                    CreatedAt = _timeProvider.UtcNow,
                    Application = "SaveState Reborn",
                    ApplicationVersion = "2.3.0",
                    IncludesActualSaveFiles = includeActualSaveFiles,
                    Contents = new[]
                    {
                        "game_library.json",
                        "user_settings.json",
                        "save_files.json",
                        "achievements.json",
                        "sessions.json"
                    }
                };

                var manifestPath = Path.Combine(tempDir, "manifest.json");
                var manifestJson = JsonSerializer.Serialize(manifest, JsonOptions);
                await File.WriteAllTextAsync(manifestPath, manifestJson, ct);

                // If including actual save files, copy them to backup
                if (includeActualSaveFiles)
                {
                    var saveFilesDir = Path.Combine(tempDir, "save_files_data");
                    Directory.CreateDirectory(saveFilesDir);

                    var saveStates = await _dbContext.SaveStates.ToListAsync(ct);
                    foreach (var saveState in saveStates)
                    {
                        if (!string.IsNullOrEmpty(saveState.FilePath) && File.Exists(saveState.FilePath))
                        {
                            var destFileName = $"{saveState.Id}_{Path.GetFileName(saveState.FilePath)}";
                            var destPath = Path.Combine(saveFilesDir, destFileName);
                            File.Copy(saveState.FilePath, destPath, overwrite: true);
                        }
                    }
                }

                // Create ZIP archive
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }

                ZipFile.CreateFromDirectory(tempDir, outputPath, CompressionLevel.Optimal, includeBaseDirectory: false);

                var fileInfo = new FileInfo(outputPath);
                LogFullBackupSuccess(_logger, outputPath, fileInfo.Length);

                return Result.Success(outputPath);
            }
            finally
            {
                // Clean up temporary directory
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }
        catch (Exception ex)
        {
            LogFullBackupFailed(_logger, ex);
            return Result.Failure<string>($"Backup creation failed: {ex.Message}");
        }
    }
}

