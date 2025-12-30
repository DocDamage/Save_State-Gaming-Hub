using SaveState.Core.UserManagement.Entities;

namespace SaveState.Core.UserManagement.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IEnumerable<Role>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<Role>> GetSystemRolesAsync(CancellationToken ct = default);
    Task AddAsync(Role role, CancellationToken ct = default);
    Task UpdateAsync(Role role, CancellationToken ct = default);
}
