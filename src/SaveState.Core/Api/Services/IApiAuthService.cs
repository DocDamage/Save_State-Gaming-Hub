using SaveState.Core.Common;

namespace SaveState.Core.Api.Services;

/// <summary>
/// Service for managing API authentication and authorization.
/// </summary>
public interface IApiAuthService
{
    /// <summary>
    /// Generates a new API key for external application access.
    /// </summary>
    Task<Result<string>> GenerateApiKeyAsync(string appName, string[] scopes, CancellationToken ct = default);

    /// <summary>
    /// Validates an API key and returns associated metadata.
    /// </summary>
    Task<Result<ApiKeyInfo>> ValidateApiKeyAsync(string apiKey, CancellationToken ct = default);

    /// <summary>
    /// Revokes an existing API key.
    /// </summary>
    Task<Result> RevokeApiKeyAsync(string apiKey, CancellationToken ct = default);

    /// <summary>
    /// Lists all active API keys for the current user.
    /// </summary>
    Task<Result<List<ApiKeyInfo>>> GetActiveApiKeysAsync(CancellationToken ct = default);
}

/// <summary>
/// Information about an API key.
/// </summary>
public record ApiKeyInfo(
    string Key,
    string AppName,
    string[] Scopes,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt,
    bool IsActive);
