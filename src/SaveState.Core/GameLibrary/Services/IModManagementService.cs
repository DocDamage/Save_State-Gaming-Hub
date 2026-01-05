namespace SaveState.Core.GameLibrary.Services;

using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Service for managing game mods.
/// </summary>
public interface IModManagementService
{
    /// <summary>
    /// Installs a mod from a file.
    /// </summary>
    /// <param name="gameId">The game ID.</param>
    /// <param name="sourceFilePath">Path to the mod archive or file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The installed mod.</returns>
    Task<Result<GameMod>> InstallModAsync(GameId gameId, string sourceFilePath, CancellationToken ct = default);

    /// <summary>
    /// Uninstalls a mod.
    /// </summary>
    /// <param name="modId">The mod ID.</param>
    /// <param name="deleteFiles">Whether to delete mod files from disk.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    Task<Result> UninstallModAsync(Guid modId, bool deleteFiles = true, CancellationToken ct = default);

    /// <summary>
    /// Toggles a mod's enabled state.
    /// </summary>
    /// <param name="modId">The mod ID.</param>
    /// <param name="enabled">The new enabled state.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    Task<Result> ToggleModAsync(Guid modId, bool enabled, CancellationToken ct = default);

    /// <summary>
    /// Updates the load order of mods.
    /// </summary>
    /// <param name="gameId">The game ID.</param>
    /// <param name="modIdsInOrder">List of mod IDs in the desired load order.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    Task<Result> UpdateLoadOrderAsync(GameId gameId, IList<Guid> modIdsInOrder, CancellationToken ct = default);

    /// <summary>
    /// Scans the game's mod directory for changes.
    /// </summary>
    /// <param name="gameId">The game ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of found/updated mods.</returns>
    Task<Result<IReadOnlyList<GameMod>>> ScanForModsAsync(GameId gameId, CancellationToken ct = default);

    /// <summary>
    /// Gets a list of external mod sources.
    /// </summary>
    Task<Result<IReadOnlyList<ModSource>>> GetExternalModSourcesAsync(CancellationToken ct = default);
}

public record ModSource(string Name, string Url, string Provider);
