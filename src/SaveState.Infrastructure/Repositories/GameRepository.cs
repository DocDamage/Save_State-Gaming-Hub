namespace SaveState.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Infrastructure.Persistence;

public class GameRepository : IGameRepository
{
    private readonly SaveStateDbContext _context;

    public GameRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    public async Task<Game?> GetByIdAsync(GameId id, CancellationToken ct = default)
        => await _context.Games.FindAsync(new object[] { (Guid)id }, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken ct = default)
        => await _context.Games.ToListAsync(ct).ConfigureAwait(false);

    public async Task<Game?> GetByTitleAndPlatformAsync(GameTitle title, Guid platformId, CancellationToken ct = default)
        => await _context.Games
            .FirstOrDefaultAsync(g =>
                g.Title.ToLower() == title.Value.ToLower() &&
                g.PlatformId == platformId,
                ct)
            .ConfigureAwait(false);

    public async Task<Game?> GetBySourceAndSourceIdAsync(string source, string sourceId, CancellationToken ct = default)
        => await _context.Games
            .FirstOrDefaultAsync(g =>
                g.Source == source &&
                g.SourceId == sourceId,
                ct)
            .ConfigureAwait(false);

    public async Task AddAsync(Game game, CancellationToken ct = default)
    {
        await _context.Games.AddAsync(game, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Game game, CancellationToken ct = default)
    {
        _context.Games.Update(game);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
