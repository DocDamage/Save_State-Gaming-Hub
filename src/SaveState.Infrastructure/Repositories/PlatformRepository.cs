namespace SaveState.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.ValueObjects;
using SaveState.Infrastructure.Persistence;

/// <summary>
/// Repository for managing gaming platform entities in the database.
/// Provides CRUD operations for platforms like PC, PlayStation, Xbox, etc.
/// </summary>
public class PlatformRepository : IPlatformRepository
{
    private readonly SaveStateDbContext _context;

    /// <summary>
    /// Initializes a new instance of the PlatformRepository.
    /// </summary>
    /// <param name="context">The database context for accessing platform data.</param>
    public PlatformRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves a platform by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the platform.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The platform entity if found, null otherwise.</returns>
    public async Task<Platform?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Platforms
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            .ConfigureAwait(false);

    /// <summary>
    /// Retrieves a platform by its name.
    /// </summary>
    /// <param name="name">The name of the platform.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The platform entity if found, null otherwise.</returns>
    public async Task<Platform?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var platformName = PlatformName.From(name);
        return await _context.Platforms
            .FirstOrDefaultAsync(p => p.Name == platformName, ct)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Platform platform, CancellationToken ct = default)
    {
        await _context.Platforms.AddAsync(platform, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
