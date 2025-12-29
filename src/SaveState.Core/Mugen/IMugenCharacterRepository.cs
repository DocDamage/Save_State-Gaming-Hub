namespace SaveState.Core.Mugen;

using SaveState.Core.Mugen.Entities;

/// <summary>
/// Repository interface for managing MUGEN character entities.
/// </summary>
public interface IMugenCharacterRepository
{
    /// <summary>
    /// Retrieves a MUGEN character by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the character.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The character if found, null otherwise.</returns>
    Task<MugenCharacter?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all MUGEN characters.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of all MUGEN characters.</returns>
    Task<IReadOnlyList<MugenCharacter>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Finds a character by its name.
    /// </summary>
    /// <param name="name">The character name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The character if found, null otherwise.</returns>
    Task<MugenCharacter?> GetByNameAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Finds characters by author.
    /// </summary>
    /// <param name="author">The author name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of characters by the specified author.</returns>
    Task<IReadOnlyList<MugenCharacter>> GetByAuthorAsync(string author, CancellationToken ct = default);

    /// <summary>
    /// Adds a new MUGEN character to the repository.
    /// </summary>
    /// <param name="character">The character to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(MugenCharacter character, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing MUGEN character.
    /// </summary>
    /// <param name="character">The character to update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(MugenCharacter character, CancellationToken ct = default);

    /// <summary>
    /// Deletes a MUGEN character.
    /// </summary>
    /// <param name="character">The character to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(MugenCharacter character, CancellationToken ct = default);
}