using SaveState.Core.RomManagement;
using EmulatorEntity = SaveState.Core.RomManagement.Entities.Emulator;
using SaveState.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace SaveState.Infrastructure.Repositories;

public class EmulatorRepository : IEmulatorRepository
{
    private readonly SaveStateDbContext _context;

    public EmulatorRepository(SaveStateDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<EmulatorEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Emulators
            .Include(e => e.Platform)
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<EmulatorEntity?> GetByPlatformIdAsync(Guid platformId, CancellationToken ct = default)
    {
        return await _context.Emulators
            .Include(e => e.Platform)
            .Where(e => e.PlatformId == platformId && e.IsAvailable)
            .OrderBy(e => e.Name)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<EmulatorEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Emulators
            .Include(e => e.Platform)
            .OrderBy(e => e.Platform.Name)
            .ThenBy(e => e.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<EmulatorEntity>> GetAvailableAsync(CancellationToken ct = default)
    {
        return await _context.Emulators
            .Include(e => e.Platform)
            .Where(e => e.IsAvailable)
            .OrderBy(e => e.Platform.Name)
            .ThenBy(e => e.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(EmulatorEntity emulator, CancellationToken ct = default)
    {
        await _context.Emulators.AddAsync(emulator, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(EmulatorEntity emulator, CancellationToken ct = default)
    {
        _context.Emulators.Update(emulator);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var emulator = await GetByIdAsync(id, ct).ConfigureAwait(false);
        if (emulator != null)
        {
            _context.Emulators.Remove(emulator);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
