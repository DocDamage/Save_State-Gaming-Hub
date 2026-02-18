using SaveState.Core.DataPortability;
using SaveState.Core.DataPortability.Models;

namespace SaveState.Infrastructure.DataPortability.Services.DataImport.Engines;

/// <summary>
/// Engine responsible for executing the actual import operations.
/// </summary>
public interface IImportExecutionEngine
{
    /// <summary>
    /// Executes import of game library data.
    /// </summary>
    Task<DataImportResult> ExecuteGameLibraryImportAsync(ParsedData data, ImportOptions options, CancellationToken ct = default);

    /// <summary>
    /// Executes import of user settings.
    /// </summary>
    Task<DataImportResult> ExecuteUserSettingsImportAsync(ParsedData data, CancellationToken ct = default);

    /// <summary>
    /// Executes import of save file metadata.
    /// </summary>
    Task<DataImportResult> ExecuteSaveFileMetadataImportAsync(ParsedData data, CancellationToken ct = default);

    /// <summary>
    /// Executes import of achievements.
    /// </summary>
    Task<DataImportResult> ExecuteAchievementsImportAsync(ParsedData data, ImportOptions options, CancellationToken ct = default);

    /// <summary>
    /// Executes import of session history.
    /// </summary>
    Task<DataImportResult> ExecuteSessionHistoryImportAsync(ParsedData data, ImportOptions options, CancellationToken ct = default);
}
