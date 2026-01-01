namespace SaveState.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Monitoring;
using SaveState.Infrastructure.Persistence;

/// <summary>
/// Repository implementation for managing MUGEN character entities.
/// </summary>
public class MugenCharacterRepository : IMugenCharacterRepository
{
    private readonly SaveStateDbContext _context;
    private readonly IApplicationMetrics _metrics;

    public MugenCharacterRepository(SaveStateDbContext context, IApplicationMetrics metrics)
    {
        _context = context;
        _metrics = metrics;
    }

    public async Task<Result<MugenCharacter>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var character = await _context.MugenCharacters
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCharacterRepository.GetByIdAsync", duration);

            if (character == null)
            {
                return Result<MugenCharacter>.Failure($"Character with ID {id} not found", ErrorType.NotFound);
            }

            return Result<MugenCharacter>.Success(character);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCharacterRepository.GetByIdAsync", duration);
            _metrics.RecordDatabaseError("MugenCharacterRepository.GetByIdAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<IReadOnlyList<MugenCharacter>> GetAllAsync(CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var characters = await _context.MugenCharacters
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCharacterRepository.GetAllAsync", duration);

            return characters.AsReadOnly();
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCharacterRepository.GetAllAsync", duration);
            _metrics.RecordDatabaseError("MugenCharacterRepository.GetAllAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<PagedResult<MugenCharacter>> GetCharactersAsync(
        int pageNumber = 1,
        int pageSize = 50,
        string? nameFilter = null,
        string? authorFilter = null,
        CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var query = _context.MugenCharacters.AsQueryable();

            query = query.Where(c => !c.IsDeleted);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(nameFilter))
            {
                query = query.Where(c => c.Name.Contains(nameFilter) || c.DisplayName.Contains(nameFilter));
            }

            if (!string.IsNullOrWhiteSpace(authorFilter))
            {
                query = query.Where(c => c.Author.Contains(authorFilter));
            }

            // Get total count
            var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

            // Apply pagination and ordering
            var characters = await query
                .OrderBy(c => c.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCharacterRepository.GetCharactersAsync", duration);

            return new PagedResult<MugenCharacter>(
                characters.AsReadOnly(),
                totalCount,
                pageNumber,
                pageSize);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCharacterRepository.GetCharactersAsync", duration);
            _metrics.RecordDatabaseError("MugenCharacterRepository.GetCharactersAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<int> CountAsync(string? nameFilter = null, string? authorFilter = null, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var query = _context.MugenCharacters.AsQueryable();
            query = query.Where(c => !c.IsDeleted);

            if (!string.IsNullOrWhiteSpace(nameFilter))
            {
                query = query.Where(c => c.Name.Contains(nameFilter) || c.DisplayName.Contains(nameFilter));
            }

            if (!string.IsNullOrWhiteSpace(authorFilter))
            {
                query = query.Where(c => c.Author.Contains(authorFilter));
            }

            var count = await query.CountAsync(ct).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCharacterRepository.CountAsync", duration);

            return count;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCharacterRepository.CountAsync", duration);
            _metrics.RecordDatabaseError("MugenCharacterRepository.CountAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<Result<MugenCharacter>> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var character = await _context.MugenCharacters
                .FirstOrDefaultAsync(c => c.Name == name && !c.IsDeleted, ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCharacterRepository.GetByNameAsync", duration);

            if (character == null)
            {
                return Result<MugenCharacter>.Failure($"Character with name '{name}' not found", ErrorType.NotFound);
            }

            return Result<MugenCharacter>.Success(character);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCharacterRepository.GetByNameAsync", duration);
            _metrics.RecordDatabaseError("MugenCharacterRepository.GetByNameAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<IReadOnlyList<MugenCharacter>> GetByAuthorAsync(string author, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var characters = await _context.MugenCharacters
                .Where(c => c.Author == author && !c.IsDeleted)
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCharacterRepository.GetByAuthorAsync", duration);

            return characters.AsReadOnly();
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCharacterRepository.GetByAuthorAsync", duration);
            _metrics.RecordDatabaseError("MugenCharacterRepository.GetByAuthorAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task AddAsync(MugenCharacter character, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            await _context.MugenCharacters.AddAsync(character, ct).ConfigureAwait(false);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCharacterRepository.AddAsync", duration);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCharacterRepository.AddAsync", duration);
            _metrics.RecordDatabaseError("MugenCharacterRepository.AddAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task UpdateAsync(MugenCharacter character, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            _context.MugenCharacters.Update(character);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCharacterRepository.UpdateAsync", duration);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCharacterRepository.UpdateAsync", duration);
            _metrics.RecordDatabaseError("MugenCharacterRepository.UpdateAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task DeleteAsync(MugenCharacter character, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            // Soft delete
            character.IsDeleted = true;
            character.DeletedAt = DateTime.UtcNow;

            _context.MugenCharacters.Update(character);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCharacterRepository.DeleteAsync", duration);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCharacterRepository.DeleteAsync", duration);
            _metrics.RecordDatabaseError("MugenCharacterRepository.DeleteAsync", ex.GetType().Name);
            throw;
        }
    }
}
