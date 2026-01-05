namespace SaveState.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Infrastructure.Persistence;

public class GameMediaRepository : IGameMediaRepository
{
    private readonly SaveStateDbContext _context;

    public GameMediaRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<GameMedia>> GetByGameIdAsync(GameId gameId, UserId userId, CancellationToken ct = default)
    {
        return await _context.GameMedia
            .Where(m => m.GameId == gameId && m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<GameMedia?> GetByIdAsync(Guid mediaId, CancellationToken ct = default)
    {
        return await _context.GameMedia
            .FirstOrDefaultAsync(m => m.Id == mediaId, ct);
    }

    public async Task<GameMedia> AddAsync(GameMedia media, CancellationToken ct = default)
    {
        await _context.GameMedia.AddAsync(media, ct);
        await _context.SaveChangesAsync(ct);
        return media;
    }

    public async Task UpdateAsync(GameMedia media, CancellationToken ct = default)
    {
        _context.GameMedia.Update(media);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid mediaId, CancellationToken ct = default)
    {
        var media = await GetByIdAsync(mediaId, ct);
        if (media != null)
        {
            _context.GameMedia.Remove(media);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<GameMedia>> GetByTypeAsync(GameId gameId, UserId userId, MediaType mediaType, CancellationToken ct = default)
    {
        return await _context.GameMedia
            .Where(m => m.GameId == gameId && m.UserId == userId && m.MediaType == mediaType)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GameMedia>> GetFavoritesAsync(GameId gameId, UserId userId, CancellationToken ct = default)
    {
        return await _context.GameMedia
            .Where(m => m.GameId == gameId && m.UserId == userId && m.IsFavorite)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GameMedia>> GetPublicMediaAsync(GameId gameId, CancellationToken ct = default)
    {
        return await _context.GameMedia
            .Where(m => m.GameId == gameId && m.IsPublic)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);
    }
}
