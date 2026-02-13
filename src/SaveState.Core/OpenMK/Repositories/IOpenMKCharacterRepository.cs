using SaveState.Core.OpenMK.Entities;
using SaveState.Core.OpenMK.ValueObjects;

namespace SaveState.Core.OpenMK.Repositories;

/// <summary>
/// Repository interface for OpenMK character data access.
/// </summary>
public interface IOpenMKCharacterRepository
{
    /// <summary>
    /// Gets all OpenMK characters.
    /// </summary>
    Task<IReadOnlyList<OpenMKCharacter>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets an OpenMK character by ID.
    /// </summary>
    Task<OpenMKCharacter?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets characters by realm.
    /// </summary>
    Task<IReadOnlyList<OpenMKCharacter>> GetByRealmAsync(OpenMKRealm realm, CancellationToken ct = default);

    /// <summary>
    /// Gets characters by fighting style.
    /// </summary>
    Task<IReadOnlyList<OpenMKCharacter>> GetByFightingStyleAsync(OpenMKFightingStyle style, CancellationToken ct = default);

    /// <summary>
    /// Gets characters by alignment.
    /// </summary>
    Task<IReadOnlyList<OpenMKCharacter>> GetByAlignmentAsync(OpenMKAlignment alignment, CancellationToken ct = default);

    /// <summary>
    /// Gets default unlocked characters.
    /// </summary>
    Task<IReadOnlyList<OpenMKCharacter>> GetDefaultUnlockedAsync(CancellationToken ct = default);

    /// <summary>
    /// Adds a new OpenMK character.
    /// </summary>
    Task AddAsync(OpenMKCharacter character, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing OpenMK character.
    /// </summary>
    Task UpdateAsync(OpenMKCharacter character, CancellationToken ct = default);

    /// <summary>
    /// Deletes an OpenMK character.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Repository interface for OpenMK user progress and unlocks.
/// </summary>
public interface IOpenMKProgressRepository
{
    /// <summary>
    /// Gets unlocked characters for a user.
    /// </summary>
    Task<IReadOnlyList<OpenMKCharacter>> GetUnlockedCharactersAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a character is unlocked for a user.
    /// </summary>
    Task<bool> IsCharacterUnlockedAsync(Guid userId, Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Unlocks a character for a user.
    /// </summary>
    Task UnlockCharacterAsync(Guid userId, Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Gets the user's koins (currency).
    /// </summary>
    Task<int> GetKoinCountAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Adds koins to a user's account.
    /// </summary>
    Task AddKoinsAsync(Guid userId, int amount, CancellationToken ct = default);

    /// <summary>
    /// Spends koins from a user's account.
    /// </summary>
    Task<bool> SpendKoinsAsync(Guid userId, int amount, CancellationToken ct = default);
}