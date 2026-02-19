using Microsoft.EntityFrameworkCore;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Repositories;

/// <summary>
/// Repository for RomValidationReport entities.
/// </summary>
public class RomValidationReportRepository : IRomValidationReportRepository
{
    private readonly SaveStateDbContext _context;

    public RomValidationReportRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<RomValidationReport?> GetByRomFileIdAsync(Guid romFileId, CancellationToken ct = default)
        => await _context.RomValidationReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RomFileId == romFileId, ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IEnumerable<RomValidationReport>> GetByStatusAsync(ValidationStatus status, CancellationToken ct = default)
        => await _context.RomValidationReports
            .AsNoTracking()
            .Where(r => r.Status == status)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IEnumerable<RomValidationReport>> GetByPlatformIdAsync(Guid platformId, CancellationToken ct = default)
    {
        // Get all ROM file IDs for this platform
        var romFileIds = await _context.RomFiles
            .Where(r => r.PlatformId == platformId)
            .Select(r => r.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return await _context.RomValidationReports
            .AsNoTracking()
            .Where(r => romFileIds.Contains(r.RomFileId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RomValidationReport>> GetAllAsync(CancellationToken ct = default)
        => await _context.RomValidationReports
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task AddAsync(RomValidationReport report, CancellationToken ct = default)
    {
        await _context.RomValidationReports.AddAsync(report, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(RomValidationReport report, CancellationToken ct = default)
    {
        _context.RomValidationReports.Update(report);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var report = await _context.RomValidationReports.FindAsync(new object[] { id }, ct).ConfigureAwait(false);
        if (report != null)
        {
            _context.RomValidationReports.Remove(report);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
