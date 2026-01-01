using SaveState.Core.Common;
using SaveState.Core.Input.Entities;

namespace SaveState.Core.Input;

public interface IControllerProfileRepository
{
    Task<ControllerProfile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ControllerProfile>> GetByGameIdAsync(Guid gameId, CancellationToken ct = default);
    Task<IReadOnlyList<ControllerProfile>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ControllerProfile>> GetByTypeAsync(ControllerType type, CancellationToken ct = default);
    Task<ControllerProfile?> GetDefaultForGameAsync(Guid gameId, CancellationToken ct = default);
    Task AddAsync(ControllerProfile profile, CancellationToken ct = default);
    Task UpdateAsync(ControllerProfile profile, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> CountAsync(ControllerType? type = null, CancellationToken ct = default);
}