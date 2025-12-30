using Microsoft.EntityFrameworkCore;
using SaveState.Core.UserManagement.Entities;
using SaveState.Core.UserManagement.Repositories;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.UserManagement;

public class ApiKeyRepository : IApiKeyRepository
{
    private readonly SaveStateDbContext _context;

    public ApiKeyRepository(SaveStateDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Set<ApiKey>()
            .Include(ak => ak.User)
            .FirstOrDefaultAsync(ak => ak.Id == id, ct);
    }

    public async Task<ApiKey?> GetByKeyPrefixAsync(string keyPrefix, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(keyPrefix))
            throw new ArgumentException("Key prefix cannot be null or whitespace.", nameof(keyPrefix));

        return await _context.Set<ApiKey>()
            .Include(ak => ak.User)
            .FirstOrDefaultAsync(ak => ak.KeyPrefix == keyPrefix && ak.IsActive, ct);
    }

    public async Task<IEnumerable<ApiKey>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Set<ApiKey>()
            .Include(ak => ak.User)
            .Where(ak => ak.UserId == userId)
            .OrderByDescending(ak => ak.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<ApiKey>> GetActiveKeysAsync(CancellationToken ct = default)
    {
        return await _context.Set<ApiKey>()
            .Include(ak => ak.User)
            .Where(ak => ak.IsActive && (!ak.ExpiresAt.HasValue || ak.ExpiresAt.Value > DateTimeOffset.UtcNow))
            .OrderByDescending(ak => ak.LastUsedAt ?? ak.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(ApiKey apiKey, CancellationToken ct = default)
    {
        if (apiKey == null)
            throw new ArgumentNullException(nameof(apiKey));

        await _context.Set<ApiKey>().AddAsync(apiKey, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ApiKey apiKey, CancellationToken ct = default)
    {
        if (apiKey == null)
            throw new ArgumentNullException(nameof(apiKey));

        _context.Set<ApiKey>().Update(apiKey);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<User?> GetUserByApiKeyAsync(string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key cannot be null or whitespace.", nameof(apiKey));

        // Extract prefix for efficient lookup
        var prefix = apiKey.Length >= 8 ? apiKey[..8] : apiKey;

        var apiKeyEntity = await _context.Set<ApiKey>()
            .Include(ak => ak.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(ak => ak.KeyPrefix == prefix && ak.IsActive, ct);

        if (apiKeyEntity == null || !apiKeyEntity.ValidateKey(apiKey))
        {
            return null;
        }

        // Update last used timestamp
        apiKeyEntity.UpdateLastUsed();
        _context.Set<ApiKey>().Update(apiKeyEntity);
        await _context.SaveChangesAsync(ct);

        return apiKeyEntity.User;
    }
}
