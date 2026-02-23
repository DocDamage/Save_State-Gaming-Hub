using SaveState.Core.Common;
using SaveState.Core.WebBrowser.Models;

namespace SaveState.Core.WebBrowser.Services;

/// <summary>
/// Service for handling OAuth authentication flows with various gaming platforms.
/// </summary>
public interface IOAuthIntegrationService
{
    /// <summary>
    /// Authenticates with Xbox Live.
    /// </summary>
    /// <returns>Result containing the access token or error information.</returns>
    Task<Result<string>> AuthenticateXboxAsync(CancellationToken ct = default);

    /// <summary>
    /// Authenticates with PlayStation Network.
    /// </summary>
    /// <returns>Result containing the access token or error information.</returns>
    Task<Result<string>> AuthenticatePlayStationAsync(CancellationToken ct = default);

    /// <summary>
    /// Authenticates with Steam.
    /// </summary>
    /// <returns>Result containing the access token or error information.</returns>
    Task<Result<string>> AuthenticateSteamAsync(CancellationToken ct = default);

    /// <summary>
    /// Authenticates with Epic Games.
    /// </summary>
    /// <returns>Result containing the access token or error information.</returns>
    Task<Result<string>> AuthenticateEpicAsync(CancellationToken ct = default);

    /// <summary>
    /// Authenticates with GOG.
    /// </summary>
    /// <returns>Result containing the access token or error information.</returns>
    Task<Result<string>> AuthenticateGogAsync(CancellationToken ct = default);

    /// <summary>
    /// Authenticates with GeForce Now.
    /// </summary>
    /// <returns>Result containing the access token or error information.</returns>
    Task<Result<string>> AuthenticateGeForceNowAsync(CancellationToken ct = default);

    /// <summary>
    /// Authenticates with Xbox Cloud Gaming.
    /// </summary>
    /// <returns>Result containing the access token or error information.</returns>
    Task<Result<string>> AuthenticateXboxCloudAsync(CancellationToken ct = default);

    /// <summary>
    /// Authenticates with Amazon Luna.
    /// </summary>
    /// <returns>Result containing the access token or error information.</returns>
    Task<Result<string>> AuthenticateAmazonLunaAsync(CancellationToken ct = default);

    /// <summary>
    /// Starts a generic OAuth flow with the specified parameters.
    /// </summary>
    /// <param name="providerName">Name of the OAuth provider.</param>
    /// <param name="authorizationEndpoint">Authorization endpoint URL.</param>
    /// <param name="tokenEndpoint">Token endpoint URL.</param>
    /// <param name="clientId">OAuth client ID.</param>
    /// <param name="redirectUri">Redirect URI for callback.</param>
    /// <param name="scopes">Requested scopes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the access token or error information.</returns>
    Task<Result<string>> StartOAuthFlowAsync(
        string providerName,
        string authorizationEndpoint,
        string tokenEndpoint,
        string clientId,
        string redirectUri,
        string[] scopes,
        CancellationToken ct = default);

    /// <summary>
    /// Refreshes an expired access token.
    /// </summary>
    /// <param name="providerName">Name of the provider.</param>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the new access token or error information.</returns>
    Task<Result<string>> RefreshTokenAsync(
        string providerName,
        string refreshToken,
        CancellationToken ct = default);

    /// <summary>
    /// Revokes an access token.
    /// </summary>
    /// <param name="providerName">Name of the provider.</param>
    /// <param name="accessToken">The access token to revoke.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> RevokeTokenAsync(
        string providerName,
        string accessToken,
        CancellationToken ct = default);

    /// <summary>
    /// Event raised when an OAuth callback is received.
    /// </summary>
    event EventHandler<OAuthCallback>? OnOAuthCallback;
}
