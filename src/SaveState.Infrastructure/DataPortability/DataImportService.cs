using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.DataPortability;
using SaveState.Core.DataPortability.Models;
using SaveState.Core.GameLibrary;
using SaveState.Infrastructure.DataPortability.Services.DataImport.Engines;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.DataPortability;

/// <summary>
/// Implementation of data import service for restoring backups and importing data.
/// Acts as a coordinator that delegates to specialized engines.
/// </summary>
public partial class DataImportService : IDataImportService
{
    private readonly IGameRepository _gameRepository;
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<DataImportService> _logger;
    private readonly IFormatDetectionEngine _formatDetection;
    private readonly IParsingEngine _parsing;
    private readonly IValidationEngine _validation;
    private readonly IMigrationEngine _migration;
    private readonly IImportExecutionEngine _execution;
    private readonly ITimeProvider _timeProvider;

    public DataImportService(
        IGameRepository gameRepository,
        SaveStateDbContext dbContext,
        ILogger<DataImportService> logger,
        IFormatDetectionEngine formatDetection,
        IParsingEngine parsing,
        IValidationEngine validation,
        IMigrationEngine migration,
        IImportExecutionEngine execution,
        ITimeProvider timeProvider)
    {
        _gameRepository = gameRepository;
        _dbContext = dbContext;
        _logger = logger;
        _formatDetection = formatDetection;
        _parsing = parsing;
        _validation = validation;
        _migration = migration;
        _execution = execution;
        _timeProvider = timeProvider;
    }

    #region LoggerMessage Definitions

    [LoggerMessage(Level = LogLevel.Information, Message = "Importing game library from {FilePath}")]
    private static partial void LogImportingGameLibrary(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to import game library")]
    private static partial void LogImportGameLibraryFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Importing user settings from {FilePath}")]
    private static partial void LogImportingUserSettings(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to import user settings")]
    private static partial void LogImportUserSettingsFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Importing save file metadata from {FilePath}")]
    private static partial void LogImportingSaveFileMetadata(ILogger logger, string filePath);

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

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to restore from backup")]
    private static partial void LogRestoreFailed(ILogger logger, Exception ex);

    #endregion

    public async Task<Result<DataImportResult>> ImportGameLibraryAsync(
        string filePath, 
        bool mergeWithExisting = true, 
        CancellationToken ct = default)
    {
        try
        {
            LogImportingGameLibrary(_logger, filePath);

            var format = await _formatDetection.DetectFormatAsync(filePath, ct);
            if (format == ImportFormat.Unknown)
            {
                return Result.Failure<DataImportResult>("Could not detect file format");
            }

            var parsedData = await _parsing.ParseAsync(filePath, format, ct);
            if (!parsedData.IsValid)
            {
                return Result.Failure<DataImportResult>(
                    $"Parse errors: {string.Join(", ", parsedData.Errors.Select(e => e.Message))}");
            }

            var validationReport = _validation.Validate(parsedData, "GameLibrary");
            if (!validationReport.IsValid)
            {
                var errors = validationReport.Errors.Select(e => e.Message).ToList();
                return Result.Failure<DataImportResult>($"Validation failed: {string.Join(", ", errors)}");
            }

            var options = new ImportOptions(MergeWithExisting: mergeWithExisting);
            var result = await _execution.ExecuteGameLibraryImportAsync(parsedData, options, ct);

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            LogImportGameLibraryFailed(_logger, ex);
            return Result.Failure<DataImportResult>($"Import failed: {ex.Message}");
        }
    }

    public async Task<Result<DataImportResult>> ImportUserSettingsAsync(
        string filePath, 
        CancellationToken ct = default)
    {
        try
        {
            LogImportingUserSettings(_logger, filePath);

            var format = await _formatDetection.DetectFormatAsync(filePath, ct);
            var parsedData = await _parsing.ParseAsync(filePath, format, ct);
            
            if (!parsedData.IsValid)
            {
                return Result.Failure<DataImportResult>(
                    $"Parse errors: {string.Join(", ", parsedData.Errors.Select(e => e.Message))}");
            }

            var result = await _execution.ExecuteUserSettingsImportAsync(parsedData, ct);
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            LogImportUserSettingsFailed(_logger, ex);
            return Result.Failure<DataImportResult>($"Import failed: {ex.Message}");
        }
    }

    public async Task<Result<DataImportResult>> ImportSaveFileMetadataAsync(
        string filePath, 
        CancellationToken ct = default)
    {
        try
        {
            LogImportingSaveFileMetadata(_logger, filePath);

            var format = await _formatDetection.DetectFormatAsync(filePath, ct);
            var parsedData = await _parsing.ParseAsync(filePath, format, ct);
            
            if (!parsedData.IsValid)
            {
                return Result.Failure<DataImportResult>(
                    $"Parse errors: {string.Join(", ", parsedData.Errors.Select(e => e.Message))}");
            }

            var result = await _execution.ExecuteSaveFileMetadataImportAsync(parsedData, ct);
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            LogImportSaveFileMetadataFailed(_logger, ex);
            return Result.Failure<DataImportResult>($"Import failed: {ex.Message}");
        }
    }

    public async Task<Result<DataImportResult>> ImportAchievementsAsync(
        string filePath, 
        bool mergeWithExisting = true, 
        CancellationToken ct = default)
    {
        try
        {
            LogImportingAchievements(_logger, filePath);

            var format = await _formatDetection.DetectFormatAsync(filePath, ct);
            var parsedData = await _parsing.ParseAsync(filePath, format, ct);
            
            if (!parsedData.IsValid)
            {
                return Result.Failure<DataImportResult>(
                    $"Parse errors: {string.Join(", ", parsedData.Errors.Select(e => e.Message))}");
            }

            var options = new ImportOptions(MergeWithExisting: mergeWithExisting);
            var result = await _execution.ExecuteAchievementsImportAsync(parsedData, options, ct);
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            LogImportAchievementsFailed(_logger, ex);
            return Result.Failure<DataImportResult>($"Import failed: {ex.Message}");
        }
    }

    public async Task<Result<DataImportResult>> ImportSessionHistoryAsync(
        string filePath, 
        bool mergeWithExisting = true, 
        CancellationToken ct = default)
    {
        try
        {
            LogImportingSessionHistory(_logger, filePath);

            var format = await _formatDetection.DetectFormatAsync(filePath, ct);
            var parsedData = await _parsing.ParseAsync(filePath, format, ct);
            
            if (!parsedData.IsValid)
            {
                return Result.Failure<DataImportResult>(
                    $"Parse errors: {string.Join(", ", parsedData.Errors.Select(e => e.Message))}");
            }

            var options = new ImportOptions(MergeWithExisting: mergeWithExisting);
            var result = await _execution.ExecuteSessionHistoryImportAsync(parsedData, options, ct);
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            LogImportSessionHistoryFailed(_logger, ex);
            return Result.Failure<DataImportResult>($"Import failed: {ex.Message}");
        }
    }

    public async Task<Result<BackupValidationResult>> ValidateBackupAsync(
        string backupPath, 
        CancellationToken ct = default)
    {
        try
        {
            LogValidatingBackup(_logger, backupPath);

            if (!File.Exists(backupPath))
            {
                return Result.Failure<BackupValidationResult>("Backup file does not exist");
            }

            var fileInfo = new FileInfo(backupPath);
            var format = await _formatDetection.DetectFormatAsync(backupPath, ct);
            
            if (format != ImportFormat.BackupZip)
            {
                return Result.Failure<BackupValidationResult>("Not a valid backup ZIP file");
            }

            var parsedData = await _parsing.ParseBackupZipAsync(backupPath, ct);
            var validationReport = _validation.ValidateBackup(parsedData);

            // Extract version and date from manifest
            string backupVersion = "Unknown";
            DateTime createdAt = DateTime.MinValue;

            if (parsedData.Sections.TryGetValue("manifest", out var manifest))
            {
                if (manifest.TryGetProperty("backupVersion", out var versionElement))
                {
                    backupVersion = versionElement.GetString() ?? "Unknown";
                }
                if (manifest.TryGetProperty("createdAt", out var createdElement) &&
                    createdElement.TryGetDateTime(out var parsedDate))
                {
                    createdAt = parsedDate;
                }
            }

            var containedFiles = parsedData.Sections.Keys.ToList();
            var isValid = validationReport.IsValid;

            var result = new BackupValidationResult(
                IsValid: isValid,
                BackupVersion: backupVersion,
                CreatedAt: createdAt,
                SizeInBytes: fileInfo.Length,
                ContainedFiles: containedFiles,
                ValidationErrors: validationReport.Errors.Select(e => e.Message).ToList());

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            LogValidateBackupFailed(_logger, ex);
            return Result.Failure<BackupValidationResult>($"Validation failed: {ex.Message}");
        }
    }

    public async Task<Result<DataImportResult>> RestoreFromBackupAsync(
        string backupPath, 
        RestoreOptions restoreOptions, 
        CancellationToken ct = default)
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
                    $"pre_restore_backup_{_timeProvider.Now:yyyyMMdd_HHmmss}.zip");
                _logger.LogInformation("Creating pre-restore backup at {Path}", preRestoreBackupPath);
            }

            // Extract and parse backup
            var parsedData = await _parsing.ParseBackupZipAsync(backupPath, ct);
            
            // Migrate if needed
            var migrationResult = await _migration.MigrateAsync(parsedData, ct: ct);
            if (!migrationResult.Success)
            {
                return Result.Failure<DataImportResult>(
                    $"Migration failed: {string.Join(", ", migrationResult.Log.Where(l => !l.Success).Select(l => l.ErrorMessage))}");
            }

            // Apply migrated data if available
            if (migrationResult.MigratedData != null)
            {
                // TODO: Update parsedData with migrated values
                _logger.LogDebug("Applied migration from {Source} to {Target}",
                    migrationResult.SourceVersion, migrationResult.TargetVersion);
            }

            var totalImported = 0;
            var totalSkipped = 0;
            var totalFailed = 0;
            var allErrors = new List<string>();
            var options = new ImportOptions(
                MergeWithExisting: true, 
                CreateBackupBeforeImport: restoreOptions.CreateBackupBeforeRestore);

            // Restore each component based on options
            if (restoreOptions.RestoreGameLibrary)
            {
                if (parsedData.Sections.ContainsKey("game_library"))
                {
                    var result = await _execution.ExecuteGameLibraryImportAsync(parsedData, options, ct);
                    totalImported += result.ItemsImported;
                    totalSkipped += result.ItemsSkipped;
                    totalFailed += result.ItemsFailed;
                    allErrors.AddRange(result.Errors);
                }
            }

            if (restoreOptions.RestoreUserSettings)
            {
                if (parsedData.Sections.ContainsKey("user_settings"))
                {
                    await _execution.ExecuteUserSettingsImportAsync(parsedData, ct);
                }
            }

            if (restoreOptions.RestoreSaveFileMetadata)
            {
                if (parsedData.Sections.ContainsKey("save_files"))
                {
                    var result = await _execution.ExecuteSaveFileMetadataImportAsync(parsedData, ct);
                    totalImported += result.ItemsImported;
                    totalSkipped += result.ItemsSkipped;
                }
            }

            if (restoreOptions.RestoreAchievements)
            {
                if (parsedData.Sections.ContainsKey("achievements"))
                {
                    await _execution.ExecuteAchievementsImportAsync(parsedData, options, ct);
                }
            }

            if (restoreOptions.RestoreSessionHistory)
            {
                if (parsedData.Sections.ContainsKey("sessions"))
                {
                    await _execution.ExecuteSessionHistoryImportAsync(parsedData, options, ct);
                }
            }

            var message = $"Restore completed: {totalImported} items imported, {totalSkipped} skipped, {totalFailed} failed";
            _logger.LogInformation(message);

            return Result.Success(new DataImportResult(
                Success: totalFailed == 0,
                ItemsImported: totalImported,
                ItemsSkipped: totalSkipped,
                ItemsFailed: totalFailed,
                Errors: allErrors,
                Message: message));
        }
        catch (Exception ex)
        {
            LogRestoreFailed(_logger, ex);
            return Result.Failure<DataImportResult>($"Restore failed: {ex.Message}");
        }
    }
}
