namespace SaveState.Core.GameLibrary;

using SaveState.Core.GameLibrary.Entities;

public interface IPlatformRepository
{
    Task<Platform?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Platform?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Platform>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Platform platform, CancellationToken ct = default);
}
