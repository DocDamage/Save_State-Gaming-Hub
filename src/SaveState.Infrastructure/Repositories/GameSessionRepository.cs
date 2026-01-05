using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of IGameSessionRepository.
/// </summary>
public class GameSessionRepository : IGameSessionRepository
{
    private readonly SaveStateDbContext _context;
    private readonly ILogger<GameSessionRepository> _logger;

    public GameSessionRepository(SaveStateDbContext context, ILogger<GameSessionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GameSession?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.GameSessions
            .Include(s => s.Game)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<GameSession?> GetActiveSessionAsync(Guid gameId, CancellationToken ct = default)
    {
        return await _context.GameSessions
            .Include(s => s.Game)
            .FirstOrDefaultAsync(s => s.GameId == gameId && s.EndedAt == null, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GameSession>> GetAllActiveSessionsAsync(CancellationToken ct = default)
    {
        return await _context.GameSessions
            .Include(s => s.Game)
            .Where(s => s.EndedAt == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GameSession>> GetByGameIdAsync(Guid gameId, int limit = 50, CancellationToken ct = default)
    {
        return await _context.GameSessions
            .Where(s => s.GameId == gameId)
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(GameSession session, CancellationToken ct = default)
    {
        await _context.GameSessions.AddAsync(session, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogDebug("Created game session {SessionId} for game {GameId}", session.Id, session.GameId);
    }

    public async Task UpdateAsync(GameSession session, CancellationToken ct = default)
    {
        _context.GameSessions.Update(session);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogDebug("Updated game session {SessionId}", session.Id);
    }

    public async Task<int> CountByGameIdAsync(Guid gameId, CancellationToken ct = default)
    {
        return await _context.GameSessions
            .CountAsync(s => s.GameId == gameId, ct)
            .ConfigureAwait(false);
    }

    public async Task<int> CountByGameIdSinceAsync(Guid gameId, DateTime since, CancellationToken ct = default)
    {
        return await _context.GameSessions
            .CountAsync(s => s.GameId == gameId && s.StartedAt >= since, ct)
            .ConfigureAwait(false);
    }

    public async Task<TimeSpan> GetTotalPlaytimeAsync(Guid gameId, CancellationToken ct = default)
    {
        var sessions = await _context.GameSessions
            .Where(s => s.GameId == gameId && s.EndedAt != null)
            .Select(s => new { s.StartedAt, s.EndedAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var totalTicks = sessions.Sum(s => (s.EndedAt!.Value - s.StartedAt).Ticks);
        return TimeSpan.FromTicks(totalTicks);
    }

    public async Task<GameSession?> GetLongestSessionAsync(Guid gameId, CancellationToken ct = default)
    {
        var sessions = await _context.GameSessions
            .Where(s => s.GameId == gameId && s.EndedAt != null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return sessions
            .OrderByDescending(s => s.Duration)
            .FirstOrDefault();
    }

    public async Task<GameSession?> GetFirstSessionAsync(Guid gameId, CancellationToken ct = default)
    {
        return await _context.GameSessions
            .Where(s => s.GameId == gameId)
            .OrderBy(s => s.StartedAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GameSession>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.GameSessions
            .Include(s => s.Game)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GameSession>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken ct = default)
    {
        return await _context.GameSessions
            .Include(s => s.Game)
            .Where(s => s.StartedAt >= start && s.StartedAt <= end)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
