using SaveState.Core.Entities;

namespace SaveState.Core.Interfaces;

public interface IEmulatorService
{
    Task<IEnumerable<Emulator>> GetAllAsync();
    Task<Emulator?> GetByIdAsync(Guid id);
    Task<Emulator?> GetDefaultForPlatformAsync(string platformName);
    Task<Emulator> AddAsync(Emulator emulator);
    Task UpdateAsync(Emulator emulator);
    Task DeleteAsync(Guid id);
    Task<bool> LaunchRomAsync(Game rom, Emulator? emulator = null);
}
