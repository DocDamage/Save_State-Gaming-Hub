using SaveState.Core.Common;
using SaveState.Core.Automation.Services.DTOs;

namespace SaveState.Core.Automation.Services;

/// <summary>
/// Service for managing macros, including CRUD operations and organization.
/// </summary>
public interface IMacroManager
{
    /// <summary>
    /// Creates a new macro from a recording session.
    /// </summary>
    Task<Result<Macro>> CreateMacroAsync(
        Guid recordingSessionId,
        MacroMetadata metadata,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a macro by its ID.
    /// </summary>
    Task<Result<Macro>> GetMacroAsync(
        Guid macroId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all macros for a specific game.
    /// </summary>
    Task<Result<IReadOnlyList<Macro>>> GetMacrosForGameAsync(
        Guid gameId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all macros created by a specific user.
    /// </summary>
    Task<Result<IReadOnlyList<Macro>>> GetMacrosByUserAsync(
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Updates macro metadata.
    /// </summary>
    Task<Result> UpdateMacroAsync(
        Guid macroId,
        MacroMetadata metadata,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a macro.
    /// </summary>
    Task<Result> DeleteMacroAsync(
        Guid macroId,
        CancellationToken ct = default);

    /// <summary>
    /// Imports a macro from a file or external source.
    /// </summary>
    Task<Result<Macro>> ImportMacroAsync(
        Stream macroData,
        string format,
        CancellationToken ct = default);

    /// <summary>
    /// Exports a macro to a file or external format.
    /// </summary>
    Task<Result<Stream>> ExportMacroAsync(
        Guid macroId,
        string format,
        CancellationToken ct = default);

    /// <summary>
    /// Gets macro categories and tags for organization.
    /// </summary>
    Task<Result<MacroCategories>> GetCategoriesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Searches for macros by name, description, or tags.
    /// </summary>
    Task<Result<IReadOnlyList<Macro>>> SearchMacrosAsync(
        string query,
        MacroSearchFilters filters,
        CancellationToken ct = default);

    /// <summary>
    /// Gets usage statistics for macros.
    /// </summary>
    Task<Result<MacroStatistics>> GetStatisticsAsync(
        CancellationToken ct = default);
}