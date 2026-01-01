namespace SaveState.Core.Mugen;

using SaveState.Core.Common;
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
    /// <returns>A Result containing the character if found, or a Failure.</returns>
    Task<Result<MugenCharacter>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all MUGEN characters.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of all MUGEN characters.</returns>
    Task<IReadOnlyList<MugenCharacter>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves MUGEN characters with pagination and filtering support.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="nameFilter">Optional name filter.</param>
    /// <param name="authorFilter">Optional author filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated result containing the characters.</returns>
    Task<PagedResult<MugenCharacter>> GetCharactersAsync(
        int pageNumber = 1,
        int pageSize = 50,
        string? nameFilter = null,
        string? authorFilter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the total count of MUGEN characters.
    /// </summary>
    /// <param name="nameFilter">Optional name filter.</param>
    /// <param name="authorFilter">Optional author filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The total number of characters matching the filters.</returns>
    Task<int> CountAsync(string? nameFilter = null, string? authorFilter = null, CancellationToken ct = default);

    /// <summary>
    /// Finds a character by its name.
    /// </summary>
    /// <param name="name">The character name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A Result containing the character if found, or a Failure.</returns>
    Task<Result<MugenCharacter>> GetByNameAsync(string name, CancellationToken ct = default);

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
