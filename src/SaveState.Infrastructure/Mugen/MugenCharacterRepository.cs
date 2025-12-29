namespace SaveState.Infrastructure.Mugen;

using Microsoft.EntityFrameworkCore;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Entities;
using SaveState.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of the MUGEN character repository.
/// </summary>
public class MugenCharacterRepository : IMugenCharacterRepository
{
    private readonly ISaveStateDbContext _context;

    /// <summary>
    /// Initializes a new instance of the MugenCharacterRepository.
    /// </summary>
    /// <param name="context">The database context.</param>
    public MugenCharacterRepository(ISaveStateDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves a MUGEN character by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the character.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The character if found, null otherwise.</returns>
    public async Task<MugenCharacter?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.MugenCharacters.FindAsync(new object[] { id }, ct);
    }

    /// <summary>
    /// Retrieves all MUGEN characters.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of all MUGEN characters.</returns>
    public async Task<IReadOnlyList<MugenCharacter>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.MugenCharacters.ToListAsync(ct);
    }

    /// <summary>
    /// Finds a character by its name.
    /// </summary>
    /// <param name="name">The character name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The character if found, null otherwise.</returns>
    public async Task<MugenCharacter?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await _context.MugenCharacters
            .FirstOrDefaultAsync(c => c.Name == name, ct);
    }

    /// <summary>
    /// Finds characters by author.
    /// </summary>
    /// <param name="author">The author name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of characters by the specified author.</returns>
    public async Task<IReadOnlyList<MugenCharacter>> GetByAuthorAsync(string author, CancellationToken ct = default)
    {
        return await _context.MugenCharacters
            .Where(c => c.Author.Contains(author))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Adds a new MUGEN character to the repository.
    /// </summary>
    /// <param name="character">The character to add.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task AddAsync(MugenCharacter character, CancellationToken ct = default)
    {
        await _context.MugenCharacters.AddAsync(character, ct);
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Updates an existing MUGEN character.
    /// </summary>
    /// <param name="character">The character to update.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task UpdateAsync(MugenCharacter character, CancellationToken ct = default)
    {
        _context.MugenCharacters.Update(character);
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Deletes a MUGEN character.
    /// </summary>
    /// <param name="character">The character to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task DeleteAsync(MugenCharacter character, CancellationToken ct = default)
    {
        _context.MugenCharacters.Remove(character);
        await _context.SaveChangesAsync(ct);
    }
}