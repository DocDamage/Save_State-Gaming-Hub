namespace SaveState.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Monitoring;
using SaveState.Infrastructure.Persistence;

/// <summary>
/// Repository implementation for managing MUGEN training session entities.
/// </summary>
public class MugenTrainingRepository : IMugenTrainingRepository
{
    private readonly SaveStateDbContext _context;
    private readonly IApplicationMetrics _metrics;

    public MugenTrainingRepository(SaveStateDbContext context, IApplicationMetrics metrics)
    {
        _context = context;
        _metrics = metrics;
    }

    public async Task<MugenTrainingSession?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var session = await _context.MugenTrainingSessions
                .Include(s => s.Character)
                .Include(s => s.OpponentCharacter)
                .Include(s => s.Recordings)
                .FirstOrDefaultAsync(s => s.Id == id, ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.GetByIdAsync", duration);

            return session;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.GetByIdAsync", duration);
            _metrics.RecordDatabaseError("MugenTrainingRepository.GetByIdAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<IReadOnlyList<MugenTrainingSession>> GetAllAsync(CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var sessions = await _context.MugenTrainingSessions
                .AsNoTracking()
                .Include(s => s.Character)
                .Include(s => s.OpponentCharacter)
                .Include(s => s.Recordings)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.GetAllAsync", duration);

            return sessions.AsReadOnly();
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.GetAllAsync", duration);
            _metrics.RecordDatabaseError("MugenTrainingRepository.GetAllAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<PagedResult<MugenTrainingSession>> GetTrainingSessionsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        Guid? userId = null,
        Guid? characterId = null,
        TrainingSessionType? sessionType = null,
        CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var query = _context.MugenTrainingSessions.AsQueryable();

            // Apply filters
            if (userId.HasValue)
            {
                query = query.Where(s => s.UserId == userId.Value);
            }

            if (characterId.HasValue)
            {
                query = query.Where(s => s.CharacterId == characterId.Value);
            }

            if (sessionType.HasValue)
            {
                query = query.Where(s => s.SessionType == sessionType.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

            // Apply pagination and ordering
            var sessions = await query
                .OrderByDescending(s => s.StartedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(s => s.Character)
                .Include(s => s.OpponentCharacter)
                .Include(s => s.Recordings)
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.GetTrainingSessionsAsync", duration);

            return new PagedResult<MugenTrainingSession>(
                sessions.AsReadOnly(),
                totalCount,
                pageNumber,
                pageSize);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.GetTrainingSessionsAsync", duration);
            _metrics.RecordDatabaseError("MugenTrainingRepository.GetTrainingSessionsAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<IReadOnlyList<MugenTrainingSession>> GetByUserAsync(Guid userId, int limit = 50, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var sessions = await _context.MugenTrainingSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartedAt)
                .Take(limit)
                .Include(s => s.Character)
                .Include(s => s.OpponentCharacter)
                .Include(s => s.Recordings)
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.GetByUserAsync", duration);

            return sessions.AsReadOnly();
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.GetByUserAsync", duration);
            _metrics.RecordDatabaseError("MugenTrainingRepository.GetByUserAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<IReadOnlyList<MugenTrainingSession>> GetByCharacterAsync(Guid characterId, int limit = 50, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var sessions = await _context.MugenTrainingSessions
                .Where(s => s.CharacterId == characterId)
                .OrderByDescending(s => s.StartedAt)
                .Take(limit)
                .Include(s => s.Character)
                .Include(s => s.OpponentCharacter)
                .Include(s => s.Recordings)
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.GetByCharacterAsync", duration);

            return sessions.AsReadOnly();
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.GetByCharacterAsync", duration);
            _metrics.RecordDatabaseError("MugenTrainingRepository.GetByCharacterAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<IReadOnlyList<MugenTrainingSession>> GetActiveSessionsAsync(Guid? userId = null, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var query = _context.MugenTrainingSessions
                .Where(s => s.EndedAt == null);

            if (userId.HasValue)
            {
                query = query.Where(s => s.UserId == userId.Value);
            }

            var sessions = await query
                .OrderByDescending(s => s.StartedAt)
                .Include(s => s.Character)
                .Include(s => s.OpponentCharacter)
                .Include(s => s.Recordings)
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.GetActiveSessionsAsync", duration);

            return sessions.AsReadOnly();
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.GetActiveSessionsAsync", duration);
            _metrics.RecordDatabaseError("MugenTrainingRepository.GetActiveSessionsAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<int> CountAsync(Guid? userId = null, Guid? characterId = null, TrainingSessionType? sessionType = null, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var query = _context.MugenTrainingSessions.AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(s => s.UserId == userId.Value);
            }

            if (characterId.HasValue)
            {
                query = query.Where(s => s.CharacterId == characterId.Value);
            }

            if (sessionType.HasValue)
            {
                query = query.Where(s => s.SessionType == sessionType.Value);
            }

            var count = await query.CountAsync(ct).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.CountAsync", duration);

            return count;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.CountAsync", duration);
            _metrics.RecordDatabaseError("MugenTrainingRepository.CountAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task AddAsync(MugenTrainingSession session, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            await _context.MugenTrainingSessions.AddAsync(session, ct).ConfigureAwait(false);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.AddAsync", duration);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.AddAsync", duration);
            _metrics.RecordDatabaseError("MugenTrainingRepository.AddAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task UpdateAsync(MugenTrainingSession session, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            _context.MugenTrainingSessions.Update(session);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.UpdateAsync", duration);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.UpdateAsync", duration);
            _metrics.RecordDatabaseError("MugenTrainingRepository.UpdateAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task DeleteAsync(MugenTrainingSession session, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            _context.MugenTrainingSessions.Remove(session);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.DeleteAsync", duration);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTrainingRepository.DeleteAsync", duration);
            _metrics.RecordDatabaseError("MugenTrainingRepository.DeleteAsync", ex.GetType().Name);
            throw;
        }
    }
}