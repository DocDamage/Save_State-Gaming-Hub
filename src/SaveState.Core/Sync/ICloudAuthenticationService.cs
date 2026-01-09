namespace SaveState.Core.Sync;

/// <summary>
/// Service for handling OAuth2 authentication flows for cloud providers.
/// </summary>
public interface ICloudAuthenticationService
{
    /// <summary>
    /// Authenticates with the specified provider using OAuth2.
    /// </summary>
    /// <param name="providerName">Name of the provider (e.g., "OneDrive", "Google Drive").</param>
    /// <param name="clientId">The client ID for the application.</param>
    /// <param name="scopes">The required permission scopes.</param>
    /// <param name="authUrl">The authorization endpoint URL.</param>
    /// <param name="tokenUrl">The token endpoint URL.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An OAuth2 token response containing access and refresh tokens.</returns>
    Task<OAuth2TokenResponse?> AuthenticateAsync(
        string providerName,
        string clientId,
        string[] scopes,
        string authUrl,
        string tokenUrl,
        CancellationToken ct = default);

    /// <summary>
    /// Refreshes an existing OAuth2 token.
    /// </summary>
    Task<OAuth2TokenResponse?> RefreshTokenAsync(
        string clientId,
        string refreshToken,
        string tokenUrl,
        CancellationToken ct = default);
}

/// <summary>
/// Response containing OAuth2 tokens.
/// </summary>
public sealed record OAuth2TokenResponse(
    string AccessToken,
    string? RefreshToken,
    DateTime ExpiresAt,
    string[] Scopes);
