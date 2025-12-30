namespace SaveState.Infrastructure.Mugen;

using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common;
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

    public async Task<int> CountAsync(string? nameFilter = null, string? authorFilter = null, CancellationToken ct = default)
    {
        var query = _context.MugenCharacters.AsQueryable();

        if (!string.IsNullOrWhiteSpace(nameFilter))
        {
            query = query.Where(c => c.Name.Contains(nameFilter));
        }

        if (!string.IsNullOrWhiteSpace(authorFilter))
        {
            query = query.Where(c => c.Author.Contains(authorFilter));
        }

        return await query.CountAsync(ct);
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
    /// Retrieves MUGEN characters with pagination and filtering support.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="nameFilter">Optional name filter.</param>
    /// <param name="authorFilter">Optional author filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated result containing the characters.</returns>
    public async Task<PagedResult<MugenCharacter>> GetCharactersAsync(
        int pageNumber = 1,
        int pageSize = 50,
        string? nameFilter = null,
        string? authorFilter = null,
        CancellationToken ct = default)
    {
        var query = _context.MugenCharacters.AsQueryable();

        // Apply filters at database level
        if (!string.IsNullOrWhiteSpace(nameFilter))
        {
            query = query.Where(c => c.Name.Contains(nameFilter));
        }

        if (!string.IsNullOrWhiteSpace(authorFilter))
        {
            query = query.Where(c => c.Author.Contains(authorFilter));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(ct);

        // Apply default sorting (by name)
        query = query.OrderBy(c => c.Name);

        // Apply pagination
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<MugenCharacter>(items, totalCount, pageNumber, pageSize);
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