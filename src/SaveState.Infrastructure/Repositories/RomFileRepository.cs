using Microsoft.EntityFrameworkCore;
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

    public async Task<IReadOnlyList<RomFileEntity>> GetByFolderPathAsync(string folderPath, Guid platformId, CancellationToken ct = default)
    {
        var normalizedFolderPath = Path.GetFullPath(folderPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return await _context.RomFiles
            .Include(r => r.Platform)
            .Where(r => r.PlatformId == platformId && r.FilePath.Value.StartsWith(normalizedFolderPath))
            .ToListAsync(ct)
            .ConfigureAwait(false);
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
