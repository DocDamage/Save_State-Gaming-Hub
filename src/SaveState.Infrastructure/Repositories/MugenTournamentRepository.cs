namespace SaveState.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Monitoring;
using SaveState.Infrastructure.Persistence;

/// <summary>
/// Repository implementation for managing MUGEN tournament entities.
/// </summary>
public class MugenTournamentRepository : IMugenTournamentRepository
{
    private readonly SaveStateDbContext _context;
    private readonly IApplicationMetrics _metrics;

    public MugenTournamentRepository(SaveStateDbContext context, IApplicationMetrics metrics)
    {
        _context = context;
        _metrics = metrics;
    }

    public async Task<Result<MugenTournament>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var tournament = await _context.MugenTournaments
                .Include(t => t.Participants)
                .Include(t => t.Matches)
                .FirstOrDefaultAsync(t => t.Id == id, ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTournamentRepository.GetByIdAsync", duration);

            if (tournament == null)
            {
                return Result<MugenTournament>.Failure($"Tournament with ID {id} not found", ErrorType.NotFound);
            }

            return Result<MugenTournament>.Success(tournament);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTournamentRepository.GetByIdAsync", duration);
            _metrics.RecordDatabaseError("MugenTournamentRepository.GetByIdAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<IReadOnlyList<MugenTournament>> GetAllAsync(CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var tournaments = await _context.MugenTournaments
                .AsNoTracking()
                .Include(t => t.Participants)
                .Include(t => t.Matches)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTournamentRepository.GetAllAsync", duration);

            return tournaments.AsReadOnly();
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTournamentRepository.GetAllAsync", duration);
            _metrics.RecordDatabaseError("MugenTournamentRepository.GetAllAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<PagedResult<MugenTournament>> GetTournamentsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        TournamentStatus? statusFilter = null,
        CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var query = _context.MugenTournaments.AsQueryable();

            // Apply status filter
            if (statusFilter.HasValue)
            {
                query = query.Where(t => t.Status == statusFilter.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

            // Apply pagination and ordering
            var tournaments = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(t => t.Participants)
                .Include(t => t.Matches)
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTournamentRepository.GetTournamentsAsync", duration);

            return new PagedResult<MugenTournament>(
                tournaments.AsReadOnly(),
                totalCount,
                pageNumber,
                pageSize);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTournamentRepository.GetTournamentsAsync", duration);
            _metrics.RecordDatabaseError("MugenTournamentRepository.GetTournamentsAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<int> CountAsync(TournamentStatus? statusFilter = null, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var query = _context.MugenTournaments.AsQueryable();

            if (statusFilter.HasValue)
            {
                query = query.Where(t => t.Status == statusFilter.Value);
            }

            var count = await query.CountAsync(ct).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTournamentRepository.CountAsync", duration);

            return count;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTournamentRepository.CountAsync", duration);
            _metrics.RecordDatabaseError("MugenTournamentRepository.CountAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<IReadOnlyList<MugenTournament>> GetByStatusAsync(TournamentStatus status, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var tournaments = await _context.MugenTournaments
                .Where(t => t.Status == status)
                .Include(t => t.Participants)
                .Include(t => t.Matches)
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTournamentRepository.GetByStatusAsync", duration);

            return tournaments.AsReadOnly();
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTournamentRepository.GetByStatusAsync", duration);
            _metrics.RecordDatabaseError("MugenTournamentRepository.GetByStatusAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task AddAsync(MugenTournament tournament, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            await _context.MugenTournaments.AddAsync(tournament, ct).ConfigureAwait(false);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTournamentRepository.AddAsync", duration);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTournamentRepository.AddAsync", duration);
            _metrics.RecordDatabaseError("MugenTournamentRepository.AddAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task UpdateAsync(MugenTournament tournament, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            _context.MugenTournaments.Update(tournament);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTournamentRepository.UpdateAsync", duration);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTournamentRepository.UpdateAsync", duration);
            _metrics.RecordDatabaseError("MugenTournamentRepository.UpdateAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task DeleteAsync(MugenTournament tournament, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            _context.MugenTournaments.Remove(tournament);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTournamentRepository.DeleteAsync", duration);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenTournamentRepository.DeleteAsync", duration);
            _metrics.RecordDatabaseError("MugenTournamentRepository.DeleteAsync", ex.GetType().Name);
            throw;
        }
    }
}
