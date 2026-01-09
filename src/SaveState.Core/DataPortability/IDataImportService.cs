using SaveState.Core.Common;

namespace SaveState.Core.DataPortability;

/// <summary>
/// Service for importing SaveState data from exports and backups.
/// </summary>
public interface IDataImportService
{
    /// <summary>
    /// Imports game library from a JSON export file.
    /// </summary>
    /// <param name="filePath">Path to the JSON file</param>
    /// <param name="mergeWithExisting">If true, merges with existing library; if false, replaces it</param>
    Task<Result<DataImportResult>> ImportGameLibraryAsync(string filePath, bool mergeWithExisting = true, CancellationToken ct = default);

    /// <summary>
    /// Imports user settings from a JSON export file.
    /// </summary>
    Task<Result<DataImportResult>> ImportUserSettingsAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Imports save file metadata from a JSON export file.
    /// </summary>
    Task<Result<DataImportResult>> ImportSaveFileMetadataAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Restores from a complete backup ZIP archive.
    /// </summary>
    /// <param name="backupPath">Path to the backup ZIP file</param>
    /// <param name="restoreOptions">Options controlling what to restore</param>
    Task<Result<DataImportResult>> RestoreFromBackupAsync(string backupPath, RestoreOptions restoreOptions, CancellationToken ct = default);

    /// <summary>
    /// Validates a backup file before restoration.
    /// </summary>
    Task<Result<BackupValidationResult>> ValidateBackupAsync(string backupPath, CancellationToken ct = default);

    /// <summary>
    /// Imports achievement progress from a JSON export file.
    /// </summary>
    Task<Result<DataImportResult>> ImportAchievementsAsync(string filePath, bool mergeWithExisting = true, CancellationToken ct = default);

    /// <summary>
    /// Imports session history from a JSON export file.
    /// </summary>
    Task<Result<DataImportResult>> ImportSessionHistoryAsync(string filePath, bool mergeWithExisting = true, CancellationToken ct = default);
}

/// <summary>
/// Result of a data import operation.
/// </summary>
public record DataImportResult(
    bool Success,
    int ItemsImported,
    int ItemsSkipped,
    int ItemsFailed,
    List<string> Errors,
    string Message);

/// <summary>
/// Options for restoring from a backup.
/// </summary>
public record RestoreOptions(
    bool RestoreGameLibrary = true,
    bool RestoreUserSettings = true,
    bool RestoreSaveFileMetadata = true,
    bool RestoreAchievements = true,
    bool RestoreSessionHistory = true,
    bool RestoreActualSaveFiles = false,
    bool CreateBackupBeforeRestore = true);

/// <summary>
/// Result of backup validation.
/// </summary>
public record BackupValidationResult(
    bool IsValid,
    string BackupVersion,
    DateTime CreatedAt,
    long SizeInBytes,
    List<string> ContainedFiles,
    List<string> ValidationErrors);
