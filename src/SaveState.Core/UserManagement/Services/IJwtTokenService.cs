using SaveState.Core.UserManagement.Entities;

namespace SaveState.Core.UserManagement.Services;

/// <summary>
/// Service for generating and validating JWT tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT access token for the specified user.
    /// </summary>
    Task<string> GenerateAccessTokenAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Generates a refresh token for the specified user.
    /// </summary>
    Task<string> GenerateRefreshTokenAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Validates the provided JWT token and returns the claims principal.
    /// </summary>
    Task<System.Security.Claims.ClaimsPrincipal?> ValidateTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Extracts the user ID from a valid JWT token.
    /// </summary>
    Task<Guid?> GetUserIdFromTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Checks if the token is expired.
    /// </summary>
    Task<bool> IsTokenExpiredAsync(string token, CancellationToken ct = default);
}
