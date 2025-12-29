namespace SaveState.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.ValueObjects;
using SaveState.Infrastructure.Persistence;

public class PlatformRepository : IPlatformRepository
{
    private readonly SaveStateDbContext _context;

    public PlatformRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    public async Task<Platform?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Platforms
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<Platform?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _context.Platforms
            .FirstOrDefaultAsync(p => p.Name.Value.ToLower() == name.ToLower(), ct)
            .ConfigureAwait(false);

    public async Task AddAsync(Platform platform, CancellationToken ct = default)
    {
        await _context.Platforms.AddAsync(platform, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
