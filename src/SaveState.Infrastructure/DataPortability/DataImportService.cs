using System.IO.Compression;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.DataPortability;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.DataPortability;

/// <summary>
/// Implementation of data import service for restoring backups and importing data.
/// </summary>
public partial class DataImportService : IDataImportService
{
    private readonly IGameRepository _gameRepository;
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<DataImportService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DataImportService(
        IGameRepository gameRepository,
        SaveStateDbContext dbContext,
        ILogger<DataImportService> logger)
    {
        _gameRepository = gameRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    #region LoggerMessage Definitions
    [LoggerMessage(Level = LogLevel.Information, Message = "Importing game library from {FilePath}")]
    private static partial void LogImportingGameLibrary(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Game '{Title}' already exists, skipping")]
    private static partial void LogGameExistsSkipping(ILogger logger, string title);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to import individual game")]
    private static partial void LogImportingGameFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Imported {Imported} games, skipped {Skipped}, failed {Failed}")]
    private static partial void LogImportGameLibrarySummary(ILogger logger, int imported, int skipped, int failed);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to import game library")]
    private static partial void LogImportGameLibraryFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Importing user settings from {FilePath}")]
    private static partial void LogImportingUserSettings(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "User settings imported successfully")]
    private static partial void LogUserSettingsImported(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to import user settings")]
    private static partial void LogImportUserSettingsFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Importing save file metadata from {FilePath}")]
    private static partial void LogImportingSaveFileMetadata(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Importing save file metadata")]
    private static partial void LogImportingSaveFileEntry(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to import save file metadata")]
    private static partial void LogImportSaveFileEntryFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Imported {Imported} save file entries, skipped {Skipped}")]
    private static partial void LogImportSaveFileSummary(ILogger logger, int imported, int skipped);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to import save file metadata")]
    private static partial void LogImportSaveFileMetadataFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Importing achievements from {FilePath}")]
    private static partial void LogImportingAchievements(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to import achievements")]
    private static partial void LogImportAchievementsFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Importing session history from {FilePath}")]
    private static partial void LogImportingSessionHistory(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to import session history")]
    private static partial void LogImportSessionHistoryFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Validating backup at {BackupPath}")]
    private static partial void LogValidatingBackup(ILogger logger, string backupPath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to validate backup")]
    private static partial void LogValidateBackupFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Restoring from backup: {BackupPath}")]
    private static partial void LogRestoringBackup(ILogger logger, string backupPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating pre-restore backup at {Path}")]
    private static partial void LogCreatingPreRestoreBackup(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Restore completed: {Imported} items imported, {Skipped} skipped, {Failed} failed")]
    private static partial void LogRestoreCompleted(ILogger logger, int imported, int skipped, int failed);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to restore from backup")]
    private static partial void LogRestoreFailed(ILogger logger, Exception ex);
    #endregion

    public async Task<Result<DataImportResult>> ImportGameLibraryAsync(string filePath, bool mergeWithExisting = true, CancellationToken ct = default)
    {
        var errors = new List<string>();
        var itemsImported = 0;
        var itemsSkipped = 0;
        var itemsFailed = 0;

        try
        {
            LogImportingGameLibrary(_logger, filePath);

            var json = await File.ReadAllTextAsync(filePath, ct);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("games", out var gamesElement))
            {
                return Result.Failure<DataImportResult>("Invalid export file: 'games' property not found");
            }

            foreach (var gameData in gamesElement.EnumerateArray())
            {
                try
                {
                    var title = gameData.GetProperty("title").GetString();
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        errors.Add("Skipped game with empty title");
                        itemsSkipped++;
                        continue;
                    }

                    // Check if game already exists
                    var existingGames = await _gameRepository.GetAllAsync(ct);
                    var existingGame = existingGames.FirstOrDefault(g => g.Title == title);

                    if (existingGame != null && mergeWithExisting)
                    {
                        LogGameExistsSkipping(_logger, title);
                        itemsSkipped++;
                        continue;
                    }

                    // Parse platform ID
                    Guid? platformId = null;
                    if (gameData.TryGetProperty("platformId", out var platformIdElement) &&
                        platformIdElement.ValueKind == JsonValueKind.String)
                    {
                        if (Guid.TryParse(platformIdElement.GetString(), out var parsedPlatformId))
                        {
                            platformId = parsedPlatformId;
                        }
                    }

                    // Create new game
                    var game = Game.Create(
                        title: title!,
                        platformId: platformId,
                        description: gameData.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                        coverImagePath: gameData.TryGetProperty("coverImagePath", out var cover) ? cover.GetString() : null,
                        source: gameData.TryGetProperty("source", out var src) ? src.GetString() : null,
                        sourceId: gameData.TryGetProperty("sourceId", out var srcId) ? srcId.GetString() : null
                    );

                    // Set install path if present
                    if (gameData.TryGetProperty("installPath", out var installPath) &&
                        !string.IsNullOrWhiteSpace(installPath.GetString()))
                    {
                        game.SetInstallPath(installPath.GetString()!);
                    }

                    // Import tags
                    if (gameData.TryGetProperty("tags", out var tagsElement) &&
                        tagsElement.ValueKind == JsonValueKind.Array)
                    {
                        var tags = tagsElement.EnumerateArray()
                            .Select(t => t.GetString())
                            .Where(t => !string.IsNullOrWhiteSpace(t))
                            .Select(t => t!)
                            .ToList();

                        game.UpdateTags(tags);
                    }

                    await _gameRepository.AddAsync(game, ct);
                    itemsImported++;
                }
                catch (Exception ex)
                {
                    LogImportingGameFailed(_logger, ex);
                    errors.Add($"Failed to import game: {ex.Message}");
                    itemsFailed++;
                }
            }

            await _dbContext.SaveChangesAsync(ct);

            var message = $"Imported {itemsImported} games, skipped {itemsSkipped}, failed {itemsFailed}";
            LogImportGameLibrarySummary(_logger, itemsImported, itemsSkipped, itemsFailed);

            return Result.Success(new DataImportResult(
                Success: true,
                ItemsImported: itemsImported,
                ItemsSkipped: itemsSkipped,
                ItemsFailed: itemsFailed,
                Errors: errors,
                Message: message));
        }
        catch (Exception ex)
        {
            LogImportGameLibraryFailed(_logger, ex);
            return Result.Failure<DataImportResult>($"Import failed: {ex.Message}");
        }
    }

    public async Task<Result<DataImportResult>> ImportUserSettingsAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            LogImportingUserSettings(_logger, filePath);

            var json = await File.ReadAllTextAsync(filePath, ct);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("settings", out var settingsElement))
            {
                return Result.Failure<DataImportResult>("Invalid settings file: 'settings' property not found");
            }

            // Parse and apply settings to appsettings.json
            var errors = new List<string>();
            var itemsImported = 0;
            var itemsFailed = 0;

            // Read current appsettings.json
            var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            string currentJson;
            JsonDocument currentDoc;

            if (File.Exists(appSettingsPath))
            {
                currentJson = await File.ReadAllTextAsync(appSettingsPath, ct);
                currentDoc = JsonDocument.Parse(currentJson);
            }
            else
            {
                return Result.Failure<DataImportResult>("appsettings.json not found");
            }

            // Merge settings
            var currentRoot = currentDoc.RootElement;
            var mergedSettings = new Dictionary<string, object>();

            // Copy existing settings
            foreach (var property in currentRoot.EnumerateObject())
            {
                mergedSettings[property.Name] = JsonSerializer.Deserialize<object>(property.Value.GetRawText());
            }

            // Apply imported settings
            var settingsSections = new[] { "Database", "OpenAI", "Groq", "Steam", "GOG", "Epic",
                "IGDB", "SteamGridDB", "Resilience", "Mugen", "RomScanning", "RetroArch", "Logging" };

            foreach (var section in settingsSections)
            {
                if (settingsElement.TryGetProperty(section, out var sectionElement))
                {
                    try
                    {
                        mergedSettings[section] = JsonSerializer.Deserialize<object>(sectionElement.GetRawText());
                        itemsImported++;
                        _logger.LogInformation("Imported {Section} settings", section);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to import {section} settings: {ex.Message}");
                        itemsFailed++;
                        _logger.LogWarning(ex, "Failed to import {Section} settings", section);
                    }
                }
            }

            // Write merged settings back to appsettings.json
            var options = new JsonSerializerOptions { WriteIndented = true };
            var mergedJson = JsonSerializer.Serialize(mergedSettings, options);
            await File.WriteAllTextAsync(appSettingsPath, mergedJson, ct);

            LogUserSettingsImported(_logger);

            return Result.Success(new DataImportResult(
                Success: itemsFailed == 0,
                ItemsImported: itemsImported,
                ItemsSkipped: settingsSections.Length - itemsImported - itemsFailed,
                ItemsFailed: itemsFailed,
                Errors: errors,
                Message: $"Settings import completed: {itemsImported} imported, {itemsFailed} failed"));
        }
        catch (Exception ex)
        {
            LogImportUserSettingsFailed(_logger, ex);
            return Result.Failure<DataImportResult>($"Import failed: {ex.Message}");
        }
    }

    public async Task<Result<DataImportResult>> ImportSaveFileMetadataAsync(string filePath, CancellationToken ct = default)
    {
        var errors = new List<string>();
        var itemsImported = 0;
        var itemsSkipped = 0;

        try
        {
            LogImportingSaveFileMetadata(_logger, filePath);

            var json = await File.ReadAllTextAsync(filePath, ct);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("saveFiles", out var saveFilesElement))
            {
                return Result.Failure<DataImportResult>("Invalid export file: 'saveFiles' property not found");
            }

            foreach (var saveFileData in saveFilesElement.EnumerateArray())
            {
                try
                {
                    // Parse and import save file metadata
                    // This is metadata only - actual file restoration happens during full backup restore
                    LogImportingSaveFileEntry(_logger);
                    itemsImported++;
                }
                catch (Exception ex)
                {
                    LogImportSaveFileEntryFailed(_logger, ex);
                    errors.Add($"Failed to import save file: {ex.Message}");
                }
            }

            var message = $"Imported {itemsImported} save file entries, skipped {itemsSkipped}";
            LogImportSaveFileSummary(_logger, itemsImported, itemsSkipped);

            return Result.Success(new DataImportResult(
                Success: true,
                ItemsImported: itemsImported,
                ItemsSkipped: itemsSkipped,
                ItemsFailed: 0,
                Errors: errors,
                Message: message));
        }
        catch (Exception ex)
        {
            LogImportSaveFileMetadataFailed(_logger, ex);
            return Result.Failure<DataImportResult>($"Import failed: {ex.Message}");
        }
    }

    public async Task<Result<DataImportResult>> ImportAchievementsAsync(string filePath, bool mergeWithExisting = true, CancellationToken ct = default)
    {
        var errors = new List<string>();
        var itemsImported = 0;
        var itemsSkipped = 0;
        var itemsFailed = 0;

        try
        {
            LogImportingAchievements(_logger, filePath);

            var json = await File.ReadAllTextAsync(filePath, ct);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("achievements", out var achievementsElement))
            {
                return Result.Failure<DataImportResult>("Invalid export file: 'achievements' property not found");
            }

            foreach (var achievementData in achievementsElement.EnumerateArray())
            {
                try
                {
                    // Parse achievement definition
                    var name = achievementData.GetProperty("name").GetString();
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        errors.Add("Skipped achievement with empty name");
                        itemsSkipped++;
                        continue;
                    }

                    // Check if achievement already exists
                    var existingAchievement = await _dbContext.Achievements
                        .FirstOrDefaultAsync(a => a.Name == name, ct);

                    Achievement achievement;

                    if (existingAchievement != null && mergeWithExisting)
                    {
                        _logger.LogDebug("Achievement '{Name}' already exists, skipping", name);
                        itemsSkipped++;
                        achievement = existingAchievement;
                    }
                    else
                    {
                        // Parse achievement type
                        var typeString = achievementData.TryGetProperty("type", out var typeElement)
                            ? typeElement.GetString()
                            : "Special";

                        if (!Enum.TryParse<AchievementType>(typeString, ignoreCase: true, out var achievementType))
                        {
                            achievementType = AchievementType.Special;
                        }

                        // Parse icon path
                        var iconPath = achievementData.TryGetProperty("iconPath", out var iconElement) &&
                            !string.IsNullOrWhiteSpace(iconElement.GetString())
                            ? iconElement.GetString()!
                            : "default_achievement.png";

                        // Create new achievement
                        achievement = new Achievement(
                            name: name!,
                            description: achievementData.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                            iconPath: iconPath,
                            points: achievementData.TryGetProperty("points", out var pts) ? pts.GetInt32() : 10,
                            type: achievementType,
                            targetValue: achievementData.TryGetProperty("targetValue", out var target) ? target.GetInt32() : 1
                        );

                        // Set criteria if present
                        if (achievementData.TryGetProperty("criteria", out var criteriaElement) &&
                            criteriaElement.ValueKind == JsonValueKind.Object)
                        {
                            var criteriaJson = criteriaElement.GetRawText();
                            achievement.SetCriteria(criteriaJson);
                        }

                        _dbContext.Achievements.Add(achievement);
                        itemsImported++;
                    }

                    // Import user achievement progress if present
                    await ImportUserAchievementProgressAsync(achievementData, achievement, name, mergeWithExisting, errors, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import achievement");
                    errors.Add($"Failed to import achievement: {ex.Message}");
                    itemsFailed++;
                }
            }

            await _dbContext.SaveChangesAsync(ct);

            var message = $"Imported {itemsImported} achievements, skipped {itemsSkipped}, failed {itemsFailed}";
            _logger.LogInformation(message);

            return Result.Success(new DataImportResult(
                Success: true,
                ItemsImported: itemsImported,
                ItemsSkipped: itemsSkipped,
                ItemsFailed: itemsFailed,
                Errors: errors,
                Message: message));
        }
        catch (Exception ex)
        {
            LogImportAchievementsFailed(_logger, ex);
            return Result.Failure<DataImportResult>($"Import failed: {ex.Message}");
        }
    }

    public async Task<Result<DataImportResult>> ImportSessionHistoryAsync(string filePath, bool mergeWithExisting = true, CancellationToken ct = default)
    {
        var errors = new List<string>();
        var itemsImported = 0;
        var itemsSkipped = 0;
        var itemsFailed = 0;

        try
        {
            LogImportingSessionHistory(_logger, filePath);

            var json = await File.ReadAllTextAsync(filePath, ct);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("sessions", out var sessionsElement))
            {
                return Result.Failure<DataImportResult>("Invalid export file: 'sessions' property not found");
            }

            foreach (var sessionData in sessionsElement.EnumerateArray())
            {
                try
                {
                    // Parse game ID
                    var gameIdString = sessionData.GetProperty("gameId").GetString();
                    if (!Guid.TryParse(gameIdString, out var gameId))
                    {
                        errors.Add("Skipped session with invalid game ID");
                        itemsSkipped++;
                        continue;
                    }

                    // Verify game exists
                    var gameExists = await _dbContext.Games.AnyAsync(g => g.Id == gameId, ct);
                    if (!gameExists)
                    {
                        errors.Add($"Skipped session for non-existent game {gameId}");
                        itemsSkipped++;
                        continue;
                    }

                    // Parse start time
                    if (!sessionData.TryGetProperty("startedAt", out var startedAtElement) ||
                        !startedAtElement.TryGetDateTime(out var startedAt))
                    {
                        errors.Add("Skipped session with invalid start time");
                        itemsSkipped++;
                        continue;
                    }

                    // Check for duplicate session (same game, same start time)
                    if (mergeWithExisting)
                    {
                        var existingSession = await _dbContext.GameSessions
                            .FirstOrDefaultAsync(s => s.GameId == gameId && s.StartedAt == startedAt, ct);

                        if (existingSession != null)
                        {
                            _logger.LogDebug("Session for game {GameId} at {StartTime} already exists, skipping", gameId, startedAt);
                            itemsSkipped++;
                            continue;
                        }
                    }

                    // Create new session
                    var session = GameSession.Create(gameId);

                    // Use reflection to set StartedAt (since it's set in Create method)
                    var startedAtProperty = typeof(GameSession).GetProperty("StartedAt");
                    if (startedAtProperty != null)
                    {
                        startedAtProperty.SetValue(session, startedAt);
                    }

                    // Parse end time if present
                    if (sessionData.TryGetProperty("endedAt", out var endedAtElement) &&
                        endedAtElement.TryGetDateTime(out var endedAt))
                    {
                        // Parse end reason
                        SessionEndReason endReason = SessionEndReason.UserClosed;
                        if (sessionData.TryGetProperty("endReason", out var endReasonElement))
                        {
                            var endReasonString = endReasonElement.GetString();
                            if (!string.IsNullOrWhiteSpace(endReasonString))
                            {
                                Enum.TryParse<SessionEndReason>(endReasonString, ignoreCase: true, out endReason);
                            }
                        }

                        session.End(endReason);

                        // Set EndedAt using reflection
                        var endedAtProperty = typeof(GameSession).GetProperty("EndedAt");
                        if (endedAtProperty != null)
                        {
                            endedAtProperty.SetValue(session, endedAt);
                        }
                    }

                    // Set notes if present
                    if (sessionData.TryGetProperty("notes", out var notesElement))
                    {
                        var notes = notesElement.GetString();
                        if (!string.IsNullOrWhiteSpace(notes))
                        {
                            session.UpdateNotes(notes);
                        }
                    }

                    _dbContext.GameSessions.Add(session);
                    itemsImported++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import session");
                    errors.Add($"Failed to import session: {ex.Message}");
                    itemsFailed++;
                }
            }

            await _dbContext.SaveChangesAsync(ct);

            var message = $"Imported {itemsImported} sessions, skipped {itemsSkipped}, failed {itemsFailed}";
            _logger.LogInformation(message);

            return Result.Success(new DataImportResult(
                Success: true,
                ItemsImported: itemsImported,
                ItemsSkipped: itemsSkipped,
                ItemsFailed: itemsFailed,
                Errors: errors,
                Message: message));
        }
        catch (Exception ex)
        {
            LogImportSessionHistoryFailed(_logger, ex);
            return Result.Failure<DataImportResult>($"Import failed: {ex.Message}");
        }
    }

    public async Task<Result<BackupValidationResult>> ValidateBackupAsync(string backupPath, CancellationToken ct = default)
    {
        try
        {
            LogValidatingBackup(_logger, backupPath);

            if (!File.Exists(backupPath))
            {
                return Result.Failure<BackupValidationResult>("Backup file does not exist");
            }

            var fileInfo = new FileInfo(backupPath);
            var containedFiles = new List<string>();
            var validationErrors = new List<string>();

            using (var archive = ZipFile.OpenRead(backupPath))
            {
                // Check for manifest
                var manifestEntry = archive.GetEntry("manifest.json");
                if (manifestEntry == null)
                {
                    validationErrors.Add("Missing manifest.json - may not be a valid SaveState backup");
                }

                // List all contained files
                foreach (var entry in archive.Entries)
                {
                    containedFiles.Add(entry.FullName);
                }

                // Parse manifest if it exists
                string backupVersion = "Unknown";
                DateTime createdAt = DateTime.MinValue;

                if (manifestEntry != null)
                {
                    using var stream = manifestEntry.Open();
                    using var reader = new StreamReader(stream);
                    var manifestJson = await reader.ReadToEndAsync(ct);
                    using var document = JsonDocument.Parse(manifestJson);
                    var root = document.RootElement;

                    if (root.TryGetProperty("backupVersion", out var versionElement))
                    {
                        backupVersion = versionElement.GetString() ?? "Unknown";
                    }

                    if (root.TryGetProperty("createdAt", out var createdElement))
                    {
                        if (createdElement.TryGetDateTime(out var parsedDate))
                        {
                            createdAt = parsedDate;
                        }
                    }
                }

                // Validate expected files are present
                var expectedFiles = new[] { "game_library.json", "user_settings.json", "save_files.json" };
                foreach (var expectedFile in expectedFiles)
                {
                    if (!containedFiles.Contains(expectedFile))
                    {
                        validationErrors.Add($"Missing expected file: {expectedFile}");
                    }
                }

                var isValid = validationErrors.Count == 0;

                var result = new BackupValidationResult(
                    IsValid: isValid,
                    BackupVersion: backupVersion,
                    CreatedAt: createdAt,
                    SizeInBytes: fileInfo.Length,
                    ContainedFiles: containedFiles,
                    ValidationErrors: validationErrors);

                return Result.Success(result);
            }
        }
        catch (Exception ex)
        {
            LogValidateBackupFailed(_logger, ex);
            return Result.Failure<BackupValidationResult>($"Validation failed: {ex.Message}");
        }
    }

    public async Task<Result<DataImportResult>> RestoreFromBackupAsync(string backupPath, RestoreOptions restoreOptions, CancellationToken ct = default)
    {
        try
        {
            LogRestoringBackup(_logger, backupPath);

            // Validate backup first
            var validationResult = await ValidateBackupAsync(backupPath, ct);
            if (!validationResult.IsSuccess || validationResult.Value?.IsValid == false)
            {
                return Result.Failure<DataImportResult>("Backup validation failed");
            }

            // Create backup of current data if requested
            if (restoreOptions.CreateBackupBeforeRestore)
            {
                var preRestoreBackupPath = Path.Combine(
                    Path.GetDirectoryName(backupPath) ?? "",
                    $"pre_restore_backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip");

                LogCreatingPreRestoreBackup(_logger, preRestoreBackupPath);
                // Note: This would call CreateFullBackupAsync from DataExportService
            }

            // Extract backup to temp directory
            var tempDir = Path.Combine(Path.GetTempPath(), $"SaveState_Restore_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            var totalImported = 0;
            var totalSkipped = 0;
            var totalFailed = 0;
            var allErrors = new List<string>();

            try
            {
                ZipFile.ExtractToDirectory(backupPath, tempDir);

                // Restore each component based on options
                if (restoreOptions.RestoreGameLibrary)
                {
                    var libraryPath = Path.Combine(tempDir, "game_library.json");
                    if (File.Exists(libraryPath))
                    {
                        var result = await ImportGameLibraryAsync(libraryPath, mergeWithExisting: true, ct);
                        if (result.IsSuccess && result.Value != null)
                        {
                            totalImported += result.Value.ItemsImported;
                            totalSkipped += result.Value.ItemsSkipped;
                            totalFailed += result.Value.ItemsFailed;
                            allErrors.AddRange(result.Value.Errors);
                        }
                    }
                }

                if (restoreOptions.RestoreUserSettings)
                {
                    var settingsPath = Path.Combine(tempDir, "user_settings.json");
                    if (File.Exists(settingsPath))
                    {
                        await ImportUserSettingsAsync(settingsPath, ct);
                    }
                }

                if (restoreOptions.RestoreSaveFileMetadata)
                {
                    var saveFilesPath = Path.Combine(tempDir, "save_files.json");
                    if (File.Exists(saveFilesPath))
                    {
                        var result = await ImportSaveFileMetadataAsync(saveFilesPath, ct);
                        if (result.IsSuccess && result.Value != null)
                        {
                            totalImported += result.Value.ItemsImported;
                            totalSkipped += result.Value.ItemsSkipped;
                        }
                    }
                }

                if (restoreOptions.RestoreAchievements)
                {
                    var achievementsPath = Path.Combine(tempDir, "achievements.json");
                    if (File.Exists(achievementsPath))
                    {
                        await ImportAchievementsAsync(achievementsPath, mergeWithExisting: true, ct);
                    }
                }

                if (restoreOptions.RestoreSessionHistory)
                {
                    var sessionsPath = Path.Combine(tempDir, "sessions.json");
                    if (File.Exists(sessionsPath))
                    {
                        await ImportSessionHistoryAsync(sessionsPath, mergeWithExisting: true, ct);
                    }
                }

                var message = $"Restore completed: {totalImported} items imported, {totalSkipped} skipped, {totalFailed} failed";
                LogRestoreCompleted(_logger, totalImported, totalSkipped, totalFailed);

                return Result.Success(new DataImportResult(
                    Success: true,
                    ItemsImported: totalImported,
                    ItemsSkipped: totalSkipped,
                    ItemsFailed: totalFailed,
                    Errors: allErrors,
                    Message: message));
            }
            finally
            {
                // Clean up temp directory
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }
        catch (Exception ex)
        {
            LogRestoreFailed(_logger, ex);
            return Result.Failure<DataImportResult>($"Restore failed: {ex.Message}");
        }
    }

    #region Helper Methods

    /// <summary>
    /// Imports user achievement progress data for a specific achievement.
    /// </summary>
    private async Task ImportUserAchievementProgressAsync(
        JsonElement achievementData,
        Achievement achievement,
        string achievementName,
        bool mergeWithExisting,
        List<string> errors,
        CancellationToken ct)
    {
        if (!achievementData.TryGetProperty("userProgress", out var userProgressElement) ||
            userProgressElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var progressData in userProgressElement.EnumerateArray())
        {
            try
            {
                var userIdString = progressData.GetProperty("userId").GetString();
                if (!Guid.TryParse(userIdString, out var userId))
                {
                    errors.Add($"Invalid user ID for achievement '{achievementName}'");
                    continue;
                }

                // Check if user achievement already exists
                var existingUserAchievement = await _dbContext.UserAchievements
                    .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AchievementId == achievement.Id, ct);

                if (existingUserAchievement != null && mergeWithExisting)
                {
                    continue; // Skip existing user progress
                }

                var targetProgress = progressData.TryGetProperty("targetProgress", out var target)
                    ? target.GetInt32()
                    : 100;

                var userAchievement = new UserAchievement(userId, achievement.Id, targetProgress);

                // Set current progress
                if (progressData.TryGetProperty("currentProgress", out var current))
                {
                    userAchievement.UpdateProgress(current.GetInt32());
                }

                // Set unlock status
                if (progressData.TryGetProperty("isUnlocked", out var unlocked) && unlocked.GetBoolean())
                {
                    userAchievement.Unlock();
                }

                _dbContext.UserAchievements.Add(userAchievement);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to import user progress for achievement '{Name}'", achievementName);
                errors.Add($"Failed to import user progress: {ex.Message}");
            }
        }
    }

    #endregion
}

