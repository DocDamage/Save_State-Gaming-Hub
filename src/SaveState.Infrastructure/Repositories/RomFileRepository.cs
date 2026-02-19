using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.RomManagement;
using RomFileEntity = SaveState.Core.RomManagement.Entities.RomFile;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Repositories;

public class RomFileRepository : IRomFileRepository
{
    private readonly SaveStateDbContext _context;

    public RomFileRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    public async Task<RomFileEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.RomFiles
            .Include(r => r.Platform)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<RomFileEntity>> GetAllAsync(CancellationToken ct = default)
        => await _context.RomFiles
            .Include(r => r.Platform)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<RomFileEntity>> GetByPlatformIdAsync(Guid platformId, CancellationToken ct = default)
        => await _context.RomFiles
            .Include(r => r.Platform)
            .Where(r => r.PlatformId == platformId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<RomFileEntity>> GetByIdsAsync(IEnumerable<Guid> romFileIds, CancellationToken ct = default)
    {
        var ids = romFileIds.ToHashSet();
        return await _context.RomFiles
            .Include(r => r.Platform)
            .Where(r => ids.Contains(r.Id))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RomFileEntity>> GetByPlatformIdsAsync(IEnumerable<Guid> platformIds, CancellationToken ct = default)
    {
        var ids = platformIds.ToHashSet();
        return await _context.RomFiles
            .Include(r => r.Platform)
            .Where(r => ids.Contains(r.PlatformId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> GetIdsByPlatformAsync(Guid platformId, CancellationToken ct = default)
        => await _context.RomFiles
            .AsNoTracking()
            .Where(r => r.PlatformId == platformId)
            .Select(r => r.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<Platform?> GetPlatformAsync(Guid platformId, CancellationToken ct = default)
        => await _context.Platforms
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == platformId, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Platform>> GetAllPlatformsAsync(CancellationToken ct = default)
        => await _context.Platforms
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<RomFileEntity>> GetByFolderPathAsync(string folderPath, Guid platformId, CancellationToken ct = default)
    {
        var normalizedFolderPath = Path.GetFullPath(folderPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return await _context.RomFiles
            .Include(r => r.Platform)
            .Where(r => r.PlatformId == platformId && r.FilePath.Value.StartsWith(normalizedFolderPath))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<PagedResult<RomFileEntity>> GetRomFilesAsync(
        int pageNumber = 1,
        int pageSize = 100,
        Guid? platformId = null,
        string? folderPath = null,
        CancellationToken ct = default)
    {
        var query = _context.RomFiles
            .Include(r => r.Platform)
            .AsQueryable();

        // Apply filters at database level
        if (platformId.HasValue)
        {
            query = query.Where(r => r.PlatformId == platformId.Value);
        }

        if (!string.IsNullOrWhiteSpace(folderPath))
        {
            var normalizedFolderPath = Path.GetFullPath(folderPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            query = query.Where(r => r.FilePath.Value.StartsWith(normalizedFolderPath));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        // Apply default sorting (by filename for ROMs)
        query = query.OrderBy(r => r.FilePath.Value);

        // Apply pagination
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<RomFileEntity>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<int> CountAsync(Guid? platformId = null, CancellationToken ct = default)
    {
        var query = _context.RomFiles.AsQueryable();

        if (platformId.HasValue)
        {
            query = query.Where(r => r.PlatformId == platformId.Value);
        }

        return await query.CountAsync(ct).ConfigureAwait(false);
    }

    public async Task AddAsync(RomFileEntity romFile, CancellationToken ct = default)
    {
        await _context.RomFiles.AddAsync(romFile, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(RomFileEntity romFile, CancellationToken ct = default)
    {
        _context.RomFiles.Update(romFile);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var romFile = await GetByIdAsync(id, ct).ConfigureAwait(false);
        if (romFile != null)
        {
            _context.RomFiles.Remove(romFile);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
