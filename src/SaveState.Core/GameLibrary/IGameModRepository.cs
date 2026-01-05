namespace SaveState.Core.GameLibrary;

using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Repository interface for managing game mods.
/// </summary>
public interface IGameModRepository
{
    /// <summary>
    /// Retrieves all mods for a specific game.
    /// </summary>
    Task<IReadOnlyList<GameMod>> GetByGameIdAsync(GameId gameId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a specific mod by ID.
    /// </summary>
    Task<GameMod?> GetByIdAsync(Guid modId, CancellationToken ct = default);

    /// <summary>
    /// Adds a new mod.
    /// </summary>
    Task<GameMod> AddAsync(GameMod mod, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing mod.
    /// </summary>
    Task UpdateAsync(GameMod mod, CancellationToken ct = default);

    /// <summary>
    /// Deletes a mod.
    /// </summary>
    Task DeleteAsync(Guid modId, CancellationToken ct = default);

    /// <summary>
    /// Gets mods by category for a specific game.
    /// </summary>
    Task<IReadOnlyList<GameMod>> GetByCategoryAsync(GameId gameId, string category, CancellationToken ct = default);

    /// <summary>
    /// Gets all enabled mods for a game in load order.
    /// </summary>
    Task<IReadOnlyList<GameMod>> GetEnabledModsAsync(GameId gameId, CancellationToken ct = default);
}
