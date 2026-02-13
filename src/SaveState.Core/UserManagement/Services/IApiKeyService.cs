using SaveState.Core.Common;
using SaveState.Core.UserManagement.DTOs;

namespace SaveState.Core.UserManagement.Services;

/// <summary>
/// Service for managing user API keys.
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Creates a new API key for the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="name">The name of the API key.</param>
    /// <param name="description">The description of the API key.</param>
    /// <param name="expiresAt">Optional expiration date.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created API key DTO with the plain key, or failure result.</returns>
    Task<Result<ApiKeyDto>> CreateApiKeyAsync(Guid userId, string name, string description, DateTimeOffset? expiresAt, CancellationToken ct = default);

    /// <summary>
    /// Gets all API keys for the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of API key DTOs, or failure result.</returns>
    Task<Result<IReadOnlyList<ApiKeyDto>>> GetUserApiKeysAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Revokes an API key.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="apiKeyId">The API key ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success or failure result.</returns>
    Task<Result> RevokeApiKeyAsync(Guid userId, Guid apiKeyId, CancellationToken ct = default);

    /// <summary>
    /// Validates an API key and returns the associated user ID if valid.
    /// </summary>
    /// <param name="apiKey">The API key to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user ID if valid, or failure result.</returns>
    Task<Result<Guid>> ValidateApiKeyAsync(string apiKey, CancellationToken ct = default);
}