using SaveState.Core.RomManagement.Entities;

namespace SaveState.Core.RomManagement;

public interface IEmulatorRepository
{
    Task<Entities.Emulator?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Entities.Emulator?> GetByPlatformIdAsync(Guid platformId, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Emulator>> GetAllByPlatformIdAsync(Guid platformId, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Emulator>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Emulator>> GetAvailableAsync(CancellationToken ct = default);
    Task AddAsync(Entities.Emulator emulator, CancellationToken ct = default);
    Task UpdateAsync(Entities.Emulator emulator, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
