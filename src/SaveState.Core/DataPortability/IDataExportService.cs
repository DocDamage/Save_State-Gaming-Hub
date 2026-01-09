using SaveState.Core.Common;

namespace SaveState.Core.DataPortability;

/// <summary>
/// Service for exporting complete SaveState data for backup and portability.
/// </summary>
public interface IDataExportService
{
    /// <summary>
    /// Exports the entire game library with metadata to JSON.
    /// </summary>
    Task<Result<string>> ExportGameLibraryAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Exports all user settings and preferences to JSON.
    /// </summary>
    Task<Result<string>> ExportUserSettingsAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Exports save file metadata and locations to JSON.
    /// </summary>
    Task<Result<string>> ExportSaveFileMetadataAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Creates a complete backup ZIP archive with all data.
    /// </summary>
    /// <param name="outputPath">Path where the ZIP file will be created</param>
    /// <param name="includeActualSaveFiles">Whether to include actual save file contents (can be large)</param>
    Task<Result<string>> CreateFullBackupAsync(string outputPath, bool includeActualSaveFiles = false, CancellationToken ct = default);

    /// <summary>
    /// Exports user achievement progress to JSON.
    /// </summary>
    Task<Result<string>> ExportAchievementsAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Exports game session history to JSON.
    /// </summary>
    Task<Result<string>> ExportSessionHistoryAsync(string filePath, CancellationToken ct = default);
}
