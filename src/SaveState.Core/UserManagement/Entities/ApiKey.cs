using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;

namespace SaveState.Core.UserManagement.Entities;

/// <summary>
/// Represents an API key for programmatic access to the system.
/// </summary>
public class ApiKey : EntityBase
{
    public Guid UserId { get; private set; }
    public required User User { get; init; }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string KeyHash { get; private set; } = string.Empty;
    public string KeyPrefix { get; private set; } = string.Empty; // First 8 characters for identification
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ApiKey() { }

    public static (ApiKey apiKey, string plainKey) Create(User user, string name, string description, ITimeProvider timeProvider, DateTimeOffset? expiresAt = null)
    {
        Guard.Against.Null(user, nameof(user));

        var plainKey = GenerateSecureKey();
        var (keyHash, keyPrefix) = HashApiKey(plainKey);

        var apiKey = new ApiKey
        {
            UserId = user.Id,
            User = user,
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Description = Guard.Against.NullOrWhiteSpace(description, nameof(description)),
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            CreatedAt = timeProvider.UtcNow,
            ExpiresAt = expiresAt,
            IsActive = true
        };

        return (apiKey, plainKey);
    }

    public bool IsExpired(ITimeProvider timeProvider)
    {
        return ExpiresAt.HasValue && timeProvider.UtcNow > ExpiresAt.Value;
    }

    public bool IsExpired(DateTimeOffset comparisonTime)
    {
        return ExpiresAt.HasValue && comparisonTime > ExpiresAt.Value;
    }

    public void Revoke()
    {
        IsActive = false;
    }

    public void UpdateLastUsed(ITimeProvider timeProvider)
    {
        LastUsedAt = timeProvider.UtcNow;
    }

    public bool ValidateKey(string providedKey, ITimeProvider timeProvider)
    {
        if (!IsActive || IsExpired(timeProvider))
            return false;

        var (providedHash, providedPrefix) = HashApiKey(providedKey);
        return KeyPrefix == providedPrefix && KeyHash == providedHash;
    }

    private static string GenerateSecureKey()
    {
        // Generate a secure 32-character API key
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var key = new char[32];

        for (int i = 0; i < key.Length; i++)
        {
            key[i] = chars[random.Next(chars.Length)];
        }

        return new string(key);
    }

    private static (string hash, string prefix) HashApiKey(string key)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));
        var hash = Convert.ToBase64String(hashBytes);
        var prefix = key.Length >= 8 ? key[..8] : key;

        return (hash, prefix);
    }

    /// <summary>
    /// Gets the plain API key (only call this immediately after creation, never store or log it)
    /// </summary>
    public static string GetPlainKey()
    {
        // This method should only be used immediately after creation
        // The plain key should never be stored or returned after initial creation
        throw new InvalidOperationException("Plain API key cannot be retrieved after creation for security reasons.");
    }
}
