namespace SaveState.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Monitoring;
using SaveState.Infrastructure.Persistence;

/// <summary>
/// Repository implementation for managing MUGEN match history entities.
/// </summary>
public class MugenMatchHistoryRepository : IMugenMatchHistoryRepository
{
    private readonly SaveStateDbContext _context;
    private readonly IApplicationMetrics _metrics;

    public MugenMatchHistoryRepository(SaveStateDbContext context, IApplicationMetrics metrics)
    {
        _context = context;
        _metrics = metrics;
    }

    public async Task<Result<MugenMatchHistory>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var matchHistory = await _context.MugenMatchHistories
                .FirstOrDefaultAsync(m => m.Id == id, ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.GetByIdAsync", duration);

            if (matchHistory == null)
            {
                return Result<MugenMatchHistory>.Failure($"Match history with ID {id} not found", ErrorType.NotFound);
            }

            return Result<MugenMatchHistory>.Success(matchHistory);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.GetByIdAsync", duration);
            _metrics.RecordDatabaseError("MugenMatchHistoryRepository.GetByIdAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<IReadOnlyList<MugenMatchHistory>> GetAllAsync(CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var matchHistories = await _context.MugenMatchHistories
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.GetAllAsync", duration);

            return matchHistories.AsReadOnly();
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.GetAllAsync", duration);
            _metrics.RecordDatabaseError("MugenMatchHistoryRepository.GetAllAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<PagedResult<MugenMatchHistory>> GetMatchHistoriesAsync(
        int pageNumber = 1,
        int pageSize = 50,
        Guid? characterId = null,
        GameMode? gameMode = null,
        CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var query = _context.MugenMatchHistories.AsQueryable();

            // Apply filters
            if (characterId.HasValue)
            {
                query = query.Where(m => m.Player1CharacterId == characterId.Value || m.Player2CharacterId == characterId.Value);
            }

            if (gameMode.HasValue)
            {
                query = query.Where(m => m.Mode == gameMode.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

            // Apply pagination and ordering
            var matchHistories = await query
                .OrderByDescending(m => m.PlayedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.GetMatchHistoriesAsync", duration);

            return new PagedResult<MugenMatchHistory>(
                matchHistories.AsReadOnly(),
                totalCount,
                pageNumber,
                pageSize);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.GetMatchHistoriesAsync", duration);
            _metrics.RecordDatabaseError("MugenMatchHistoryRepository.GetMatchHistoriesAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<IReadOnlyList<MugenMatchHistory>> GetByCharacterAsync(Guid characterId, int limit = 100, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var matchHistories = await _context.MugenMatchHistories
                .Where(m => m.Player1CharacterId == characterId || m.Player2CharacterId == characterId)
                .OrderByDescending(m => m.PlayedAt)
                .Take(limit)
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.GetByCharacterAsync", duration);

            return matchHistories.AsReadOnly();
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.GetByCharacterAsync", duration);
            _metrics.RecordDatabaseError("MugenMatchHistoryRepository.GetByCharacterAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<Result<MugenMatchupStats>> GetMatchupStatsAsync(Guid character1Id, Guid character2Id, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            // Ensure consistent ordering (smaller ID first)
            var (char1, char2) = character1Id.CompareTo(character2Id) < 0
                ? (character1Id, character2Id)
                : (character2Id, character1Id);

            var matchupStats = await _context.MugenMatchupStats
                .Include(m => m.Character1)
                .Include(m => m.Character2)
                .FirstOrDefaultAsync(m =>
                    (m.Character1Id == char1 && m.Character2Id == char2) ||
                    (m.Character1Id == char2 && m.Character2Id == char1), ct)
                .ConfigureAwait(false);

            if (matchupStats == null)
            {
                // Create new matchup stats if none exist
                matchupStats = MugenMatchupStats.Create(char1, char2);
                await _context.MugenMatchupStats.AddAsync(matchupStats, ct).ConfigureAwait(false);
                await _context.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.GetMatchupStatsAsync", duration);

            return Result<MugenMatchupStats>.Success(matchupStats);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.GetMatchupStatsAsync", duration);
            _metrics.RecordDatabaseError("MugenMatchHistoryRepository.GetMatchupStatsAsync", ex.GetType().Name);
            return Result<MugenMatchupStats>.Failure($"Failed to get matchup stats: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<MugenMatchHistory>> RecordMatchAsync(MugenMatchHistory match, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            await _context.MugenMatchHistories.AddAsync(match, ct).ConfigureAwait(false);

            // Update matchup statistics
            var matchupResult = await GetMatchupStatsAsync(match.Player1CharacterId, match.Player2CharacterId, ct).ConfigureAwait(false);
            if (matchupResult.IsSuccess)
            {
                var matchup = matchupResult.Value;
                var character1Won = match.Result == MatchResult.Player1Win;
                var wasDraw = match.Result == MatchResult.Draw || match.Result == MatchResult.Timeout;

                matchup.RecordMatch(character1Won, wasDraw, match.MatchDuration);
                _context.MugenMatchupStats.Update(matchup);
            }

            await _context.SaveChangesAsync(ct).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.RecordMatchAsync", duration);

            return Result<MugenMatchHistory>.Success(match);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.RecordMatchAsync", duration);
            _metrics.RecordDatabaseError("MugenMatchHistoryRepository.RecordMatchAsync", ex.GetType().Name);
            return Result<MugenMatchHistory>.Failure($"Failed to record match: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<int> CountAsync(Guid? characterId = null, GameMode? gameMode = null, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var query = _context.MugenMatchHistories.AsQueryable();

            if (characterId.HasValue)
            {
                query = query.Where(m => m.Player1CharacterId == characterId.Value || m.Player2CharacterId == characterId.Value);
            }

            if (gameMode.HasValue)
            {
                query = query.Where(m => m.Mode == gameMode.Value);
            }

            var count = await query.CountAsync(ct).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.CountAsync", duration);

            return count;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.CountAsync", duration);
            _metrics.RecordDatabaseError("MugenMatchHistoryRepository.CountAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task AddAsync(MugenMatchHistory matchHistory, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            await _context.MugenMatchHistories.AddAsync(matchHistory, ct).ConfigureAwait(false);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.AddAsync", duration);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.AddAsync", duration);
            _metrics.RecordDatabaseError("MugenMatchHistoryRepository.AddAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task UpdateAsync(MugenMatchHistory matchHistory, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            _context.MugenMatchHistories.Update(matchHistory);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.UpdateAsync", duration);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.UpdateAsync", duration);
            _metrics.RecordDatabaseError("MugenMatchHistoryRepository.UpdateAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task DeleteAsync(MugenMatchHistory matchHistory, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            _context.MugenMatchHistories.Remove(matchHistory);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.DeleteAsync", duration);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenMatchHistoryRepository.DeleteAsync", duration);
            _metrics.RecordDatabaseError("MugenMatchHistoryRepository.DeleteAsync", ex.GetType().Name);
            throw;
        }
    }
}
