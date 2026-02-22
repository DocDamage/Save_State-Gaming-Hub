using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common.Services;
using SaveState.Core.UserManagement.Entities;
using SaveState.Core.UserManagement.Repositories;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.UserManagement;

/// <summary>
/// Repository implementation for managing API keys in the database.
/// Provides CRUD operations and specialized queries for API key management.
/// </summary>
public class ApiKeyRepository : IApiKeyRepository
{
    private readonly SaveStateDbContext _context;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyRepository"/> class.
    /// </summary>
    /// <param name="context">The database context for data access operations.</param>
    /// <param name="timeProvider">The time provider for date/time operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public ApiKeyRepository(SaveStateDbContext context, ITimeProvider timeProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Retrieves an API key by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the API key.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The API key if found; otherwise, null.</returns>
    public async Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Set<ApiKey>()
            .Include(ak => ak.User)
            .FirstOrDefaultAsync(ak => ak.Id == id, ct);
    }

    /// <summary>
    /// Retrieves an active API key by its key prefix.
    /// </summary>
    /// <param name="keyPrefix">The prefix of the API key to search for.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The active API key if found; otherwise, null.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="keyPrefix"/> is null or whitespace.</exception>
    public async Task<ApiKey?> GetByKeyPrefixAsync(string keyPrefix, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(keyPrefix))
            throw new ArgumentException("Key prefix cannot be null or whitespace.", nameof(keyPrefix));

        return await _context.Set<ApiKey>()
            .Include(ak => ak.User)
            .FirstOrDefaultAsync(ak => ak.KeyPrefix == keyPrefix && ak.IsActive, ct);
    }

    /// <summary>
    /// Retrieves all API keys associated with a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A collection of API keys ordered by creation date (newest first).</returns>
    public async Task<IEnumerable<ApiKey>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Set<ApiKey>()
            .Include(ak => ak.User)
            .Where(ak => ak.UserId == userId)
            .OrderByDescending(ak => ak.CreatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Retrieves all active API keys that have not expired.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A collection of active API keys ordered by last used date or creation date.</returns>
    public async Task<IEnumerable<ApiKey>> GetActiveKeysAsync(CancellationToken ct = default)
    {
        return await _context.Set<ApiKey>()
            .Include(ak => ak.User)
            .Where(ak => ak.IsActive && (!ak.ExpiresAt.HasValue || ak.ExpiresAt.Value > DateTimeOffset.UtcNow))
            .OrderByDescending(ak => ak.LastUsedAt ?? ak.CreatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Adds a new API key to the database.
    /// </summary>
    /// <param name="apiKey">The API key entity to add.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiKey"/> is null.</exception>
    public async Task AddAsync(ApiKey apiKey, CancellationToken ct = default)
    {
        if (apiKey == null)
            throw new ArgumentNullException(nameof(apiKey));

        await _context.Set<ApiKey>().AddAsync(apiKey, ct);
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Updates an existing API key in the database.
    /// </summary>
    /// <param name="apiKey">The API key entity to update.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiKey"/> is null.</exception>
    public async Task UpdateAsync(ApiKey apiKey, CancellationToken ct = default)
    {
        if (apiKey == null)
            throw new ArgumentNullException(nameof(apiKey));

        _context.Set<ApiKey>().Update(apiKey);
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Authenticates a user using their API key and retrieves the associated user with full role and permission information.
    /// Updates the API key's last used timestamp upon successful authentication.
    /// </summary>
    /// <param name="apiKey">The full API key string to validate.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The authenticated user with roles and permissions if the API key is valid; otherwise, null.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="apiKey"/> is null or whitespace.</exception>
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

        if (apiKeyEntity == null || !apiKeyEntity.ValidateKey(apiKey, _timeProvider))
        {
            return null;
        }

        // Update last used timestamp
        apiKeyEntity.UpdateLastUsed(_timeProvider);
        _context.Set<ApiKey>().Update(apiKeyEntity);
        await _context.SaveChangesAsync(ct);

        return apiKeyEntity.User;
    }
}
