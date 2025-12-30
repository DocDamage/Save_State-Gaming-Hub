using SaveState.Core.UserManagement.Entities;

namespace SaveState.Core.UserManagement.Repositories;

public interface IApiKeyRepository
{
    Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApiKey?> GetByKeyPrefixAsync(string keyPrefix, CancellationToken ct = default);
    Task<IEnumerable<ApiKey>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<ApiKey>> GetActiveKeysAsync(CancellationToken ct = default);
    Task AddAsync(ApiKey apiKey, CancellationToken ct = default);
    Task UpdateAsync(ApiKey apiKey, CancellationToken ct = default);
    Task<User?> GetUserByApiKeyAsync(string apiKey, CancellationToken ct = default);
}
