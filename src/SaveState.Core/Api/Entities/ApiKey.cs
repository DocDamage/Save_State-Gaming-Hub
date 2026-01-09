using SaveState.Core.Common.Base;

namespace SaveState.Core.Api.Entities;

/// <summary>
/// Represents an API key for external application access to the system.
/// </summary>
public class ApiKey : EntityBase
{
    public string Key { get; private set; } = string.Empty;
    public string AppName { get; private set; } = string.Empty;
    public string[] Scopes { get; private set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ApiKey() { }

    public static ApiKey Create(string key, string appName, string[] scopes, DateTime? expiresAt = null)
    {
        Guard.Against.NullOrWhiteSpace(key, nameof(key));
        Guard.Against.NullOrWhiteSpace(appName, nameof(appName));
        Guard.Against.Null(scopes, nameof(scopes));

        return new ApiKey
        {
            Key = key,
            AppName = appName,
            Scopes = scopes,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsActive = true
        };
    }

    public bool IsExpired()
    {
        return ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    }

    public void Revoke()
    {
        IsActive = false;
    }

    public void UpdateLastUsed()
    {
        LastUsedAt = DateTime.UtcNow;
    }

    public bool ValidateScopes(string[] requiredScopes)
    {
        if (requiredScopes == null || requiredScopes.Length == 0)
            return true;

        return requiredScopes.All(required => Scopes.Contains(required));
    }
}