using Microsoft.EntityFrameworkCore;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Repositories;

/// <summary>
/// Repository for RomHashInfo entities.
/// </summary>
public class RomHashInfoRepository : IRomHashInfoRepository
{
    private readonly SaveStateDbContext _context;

    public RomHashInfoRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<RomHashInfo?> GetByRomFileIdAsync(Guid romFileId, CancellationToken ct = default)
        => await _context.RomHashInfos
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.RomFileId == romFileId, ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IEnumerable<RomHashInfo>> GetByHashAsync(string hash, HashAlgorithmType type, CancellationToken ct = default)
    {
        var query = _context.RomHashInfos.AsNoTracking().AsQueryable();

        query = type switch
        {
            HashAlgorithmType.Crc32 => query.Where(h => h.Crc32 == hash),
            HashAlgorithmType.Md5 => query.Where(h => h.Md5 == hash),
            HashAlgorithmType.Sha1 => query.Where(h => h.Sha1 == hash),
            HashAlgorithmType.Sha256 => query.Where(h => h.Sha256 == hash),
            _ => query.Where(h => h.Sha1 == hash || h.Md5 == hash || h.Crc32 == hash)
        };

        return await query.ToListAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RomHashInfo>> GetAllAsync(CancellationToken ct = default)
        => await _context.RomHashInfos
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task AddAsync(RomHashInfo hashInfo, CancellationToken ct = default)
    {
        await _context.RomHashInfos.AddAsync(hashInfo, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(RomHashInfo hashInfo, CancellationToken ct = default)
    {
        _context.RomHashInfos.Update(hashInfo);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var hashInfo = await _context.RomHashInfos.FindAsync(new object[] { id }, ct).ConfigureAwait(false);
        if (hashInfo != null)
        {
            _context.RomHashInfos.Remove(hashInfo);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
