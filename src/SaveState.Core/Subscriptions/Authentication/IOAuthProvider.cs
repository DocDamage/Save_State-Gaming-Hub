// Copyright (c) 2026 SaveStateReborn. All rights reserved.

namespace SaveState.Core.Subscriptions.Authentication;

/// <summary>
/// OAuth authentication provider for subscription services.
/// </summary>
public interface IOAuthProvider
{
    /// <summary>
    /// The service type this OAuth provider handles.
    /// </summary>
    SubscriptionServiceType ServiceType { get; }

    /// <summary>
    /// Gets the OAuth authorization URL.
    /// </summary>
    string GetAuthorizationUrl(string redirectUri, string state);

    /// <summary>
    /// Exchanges authorization code for access token.
    /// </summary>
    Task<OAuthTokenResult> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct = default);

    /// <summary>
    /// Refreshes an expired access token.
    /// </summary>
    Task<OAuthTokenResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Validates if the current token is still valid.
    /// </summary>
    Task<bool> ValidateTokenAsync(string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Revokes the access token.
    /// </summary>
    Task RevokeTokenAsync(string accessToken, CancellationToken ct = default);
}

/// <summary>
/// OAuth token exchange result.
/// </summary>
public class OAuthTokenResult
{
    /// <summary>
    /// Indicates if the token exchange was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// The access token.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// The refresh token.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Token expiration time.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Error message if the exchange failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Additional token data (scope, token type, etc.).
    /// </summary>
    public Dictionary<string, string> AdditionalData { get; set; } = new();

    public static OAuthTokenResult Success(string accessToken, string? refreshToken = null, DateTime? expiresAt = null)
    {
        return new OAuthTokenResult
        {
            IsSuccess = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };
    }

    public static OAuthTokenResult Failure(string error)
    {
        return new OAuthTokenResult
        {
            IsSuccess = false,
            Error = error
        };
    }
}

/// <summary>
/// Stores OAuth tokens securely.
/// </summary>
public interface IOAuthTokenStore
{
    /// <summary>
    /// Saves OAuth tokens for a service.
    /// </summary>
    Task SaveTokensAsync(SubscriptionServiceType serviceType, OAuthTokens tokens, CancellationToken ct = default);

    /// <summary>
    /// Retrieves OAuth tokens for a service.
    /// </summary>
    Task<OAuthTokens?> GetTokensAsync(SubscriptionServiceType serviceType, CancellationToken ct = default);

    /// <summary>
    /// Deletes stored tokens for a service.
    /// </summary>
    Task DeleteTokensAsync(SubscriptionServiceType serviceType, CancellationToken ct = default);

    /// <summary>
    /// Checks if valid tokens exist for a service.
    /// </summary>
    Task<bool> HasValidTokensAsync(SubscriptionServiceType serviceType, CancellationToken ct = default);
}

/// <summary>
/// OAuth tokens for a subscription service.
/// </summary>
public class OAuthTokens
{
    /// <summary>
    /// The access token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// The refresh token.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Token expiration time.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When the tokens were obtained.
    /// </summary>
    public DateTime ObtainedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional token data.
    /// </summary>
    public Dictionary<string, string> AdditionalData { get; set; } = new();

    /// <summary>
    /// Checks if the token is expired or about to expire (within 5 minutes).
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt.AddMinutes(-5);
}
