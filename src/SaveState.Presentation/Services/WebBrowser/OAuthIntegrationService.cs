using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.WebBrowser.Models;
using SaveState.Core.WebBrowser.Services;

namespace SaveState.Presentation.Services.WebBrowser;

/// <summary>
/// Implementation of OAuth integration service for gaming platforms.
/// </summary>
public class OAuthIntegrationService : IOAuthIntegrationService
{
    private readonly ILogger<OAuthIntegrationService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, OAuthState> _pendingStates = new();
    private readonly string _redirectUri = "http://localhost:53215/auth/callback";

    // OAuth endpoints for various platforms
    private static readonly Dictionary<string, OAuthEndpoints> Endpoints = new()
    {
        ["Xbox"] = new OAuthEndpoints(
            "https://login.live.com/oauth20_authorize.srf",
            "https://login.live.com/oauth20_token.srf",
            "XboxLive.signin offline_access"),
        ["PlayStation"] = new OAuthEndpoints(
            "https://auth.api.sonyentertainmentnetwork.com/2.0/oauth/authorize",
            "https://auth.api.sonyentertainmentnetwork.com/2.0/oauth/token",
            "psn:social psn:account"),
        ["Steam"] = new OAuthEndpoints(
            "https://steamcommunity.com/openid/login",
            "",
            ""),
        ["Epic"] = new OAuthEndpoints(
            "https://www.epicgames.com/id/authorize",
            "https://api.epicgames.dev/epic/oauth/v1/token",
            "basic_profile presence"),
        ["GOG"] = new OAuthEndpoints(
            "https://auth.gog.com/auth",
            "https://auth.gog.com/token",
            "offline"),
        ["GeForceNow"] = new OAuthEndpoints(
            "https://login.nvgs.nvidia.com/oauth2/auth",
            "https://login.nvgs.nvidia.com/oauth2/token",
            "openid offline_access"),
        ["XboxCloud"] = new OAuthEndpoints(
            "https://login.live.com/oauth20_authorize.srf",
            "https://login.live.com/oauth20_token.srf",
            "XboxLive.signin XboxLive.offline_access"),
        ["AmazonLuna"] = new OAuthEndpoints(
            "https://www.amazon.com/ap/oa",
            "https://api.amazon.com/auth/o2/token",
            "profile cloud_gaming")
    };

    public OAuthIntegrationService(
        ILogger<OAuthIntegrationService> logger,
        ITimeProvider timeProvider,
        HttpClient httpClient)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public event EventHandler<OAuthCallback>? OnOAuthCallback;

    /// <inheritdoc />
    public Task<Result<string>> AuthenticateXboxAsync(CancellationToken ct = default)
        => StartProviderAuthAsync("Xbox", ct);

    /// <inheritdoc />
    public Task<Result<string>> AuthenticatePlayStationAsync(CancellationToken ct = default)
        => StartProviderAuthAsync("PlayStation", ct);

    /// <inheritdoc />
    public Task<Result<string>> AuthenticateSteamAsync(CancellationToken ct = default)
        => StartProviderAuthAsync("Steam", ct);

    /// <inheritdoc />
    public Task<Result<string>> AuthenticateEpicAsync(CancellationToken ct = default)
        => StartProviderAuthAsync("Epic", ct);

    /// <inheritdoc />
    public Task<Result<string>> AuthenticateGogAsync(CancellationToken ct = default)
        => StartProviderAuthAsync("GOG", ct);

    /// <inheritdoc />
    public Task<Result<string>> AuthenticateGeForceNowAsync(CancellationToken ct = default)
        => StartProviderAuthAsync("GeForceNow", ct);

    /// <inheritdoc />
    public Task<Result<string>> AuthenticateXboxCloudAsync(CancellationToken ct = default)
        => StartProviderAuthAsync("XboxCloud", ct);

    /// <inheritdoc />
    public Task<Result<string>> AuthenticateAmazonLunaAsync(CancellationToken ct = default)
        => StartProviderAuthAsync("AmazonLuna", ct);

    /// <inheritdoc />
    public async Task<Result<string>> StartOAuthFlowAsync(
        string providerName,
        string authorizationEndpoint,
        string tokenEndpoint,
        string clientId,
        string redirectUri,
        string[] scopes,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting OAuth flow for {Provider}", providerName);

            // Generate PKCE parameters
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);
            var state = GenerateState();

            // Store state for verification
            _pendingStates[state] = new OAuthState
            {
                Provider = providerName,
                CodeVerifier = codeVerifier,
                TokenEndpoint = tokenEndpoint,
                ClientId = clientId,
                RedirectUri = redirectUri,
                CreatedAt = _timeProvider.UtcNow
            };

            // Build authorization URL
            var scopeString = string.Join(" ", scopes);
            var authUrl = $"{authorizationEndpoint}?" +
                $"client_id={Uri.EscapeDataString(clientId)}&" +
                $"response_type=code&" +
                $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                $"scope={Uri.EscapeDataString(scopeString)}&" +
                $"state={state}&" +
                $"code_challenge={codeChallenge}&" +
                $"code_challenge_method=S256";

            // Open browser
            OpenBrowser(authUrl);

            // Wait for callback (in real implementation, this would use a proper callback listener)
            // For now, return pending state
            return Result<string>.Success(state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start OAuth flow for {Provider}", providerName);
            return Result<string>.Failure($"OAuth initiation failed: {ex.Message}", ErrorType.External);
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> RefreshTokenAsync(
        string providerName,
        string refreshToken,
        CancellationToken ct = default)
    {
        try
        {
            if (!Endpoints.TryGetValue(providerName, out var endpoints))
            {
                return Result<string>.Failure($"Unknown provider: {providerName}", ErrorType.Validation);
            }

            var clientId = GetClientId(providerName);
            if (string.IsNullOrEmpty(clientId))
            {
                return Result<string>.Failure($"Client ID not configured for {providerName}", ErrorType.Internal);
            }

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = clientId
            });

            var response = await _httpClient.PostAsync(endpoints.TokenEndpoint, content, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token refresh failed for {Provider}: {Response}", providerName, responseBody);
                return Result<string>.Failure("Token refresh failed", ErrorType.External);
            }

            // In real implementation, parse the JSON response
            return Result<string>.Success(responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh token for {Provider}", providerName);
            return Result<string>.Failure($"Token refresh failed: {ex.Message}", ErrorType.External);
        }
    }

    /// <inheritdoc />
    public async Task<Result> RevokeTokenAsync(
        string providerName,
        string accessToken,
        CancellationToken ct = default)
    {
        try
        {
            // Provider-specific revocation logic would go here
            _logger.LogInformation("Revoking token for {Provider}", providerName);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke token for {Provider}", providerName);
            return Result.Failure($"Token revocation failed: {ex.Message}", ErrorType.External);
        }
    }

    /// <summary>
    /// Handles an OAuth callback.
    /// </summary>
    public async Task<Result<string>> HandleCallbackAsync(string state, string code, string? error = null)
    {
        if (!_pendingStates.TryGetValue(state, out var oauthState))
        {
            return Result<string>.Failure("Invalid or expired state", ErrorType.Validation);
        }

        _pendingStates.Remove(state);

        if (!string.IsNullOrEmpty(error))
        {
            return Result<string>.Failure($"OAuth error: {error}", ErrorType.External);
        }

        try
        {
            // Exchange code for token
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = oauthState.RedirectUri,
                ["client_id"] = oauthState.ClientId,
                ["code_verifier"] = oauthState.CodeVerifier
            });

            var response = await _httpClient.PostAsync(oauthState.TokenEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token exchange failed: {Response}", responseBody);
                return Result<string>.Failure("Token exchange failed", ErrorType.External);
            }

            OnOAuthCallback?.Invoke(this, new OAuthCallback
            {
                Provider = oauthState.Provider,
                Code = code,
                State = state
            });

            return Result<string>.Success(responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle OAuth callback");
            return Result<string>.Failure($"Callback handling failed: {ex.Message}", ErrorType.External);
        }
    }

    private async Task<Result<string>> StartProviderAuthAsync(string provider, CancellationToken ct)
    {
        if (!Endpoints.TryGetValue(provider, out var endpoints))
        {
            return Result<string>.Failure($"Unknown provider: {provider}", ErrorType.Validation);
        }

        var clientId = GetClientId(provider);
        if (string.IsNullOrEmpty(clientId))
        {
            return Result<string>.Failure(
                $"Client ID not configured for {provider}. Please configure in settings.",
                ErrorType.Internal);
        }

        return await StartOAuthFlowAsync(
            provider,
            endpoints.AuthEndpoint,
            endpoints.TokenEndpoint,
            clientId,
            _redirectUri,
            endpoints.Scopes.Split(' '),
            ct);
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Fallback for different platforms
            if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", url);
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", url);
            }
        }
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateCodeChallenge(string verifier)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier));
        return Base64UrlEncode(hash);
    }

    private static string GenerateState()
    {
        var bytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private static string GetClientId(string provider) => provider switch
    {
        "Xbox" => GetEnvironmentClientId("XBOX_CLIENT_ID"),
        "PlayStation" => GetEnvironmentClientId("PSN_CLIENT_ID"),
        "Steam" => GetEnvironmentClientId("STEAM_API_KEY"),
        "Epic" => GetEnvironmentClientId("EPIC_CLIENT_ID"),
        "GOG" => GetEnvironmentClientId("GOG_CLIENT_ID"),
        "GeForceNow" => GetEnvironmentClientId("GFN_CLIENT_ID"),
        "XboxCloud" => GetEnvironmentClientId("XBOX_CLIENT_ID"),
        "AmazonLuna" => GetEnvironmentClientId("LUNA_CLIENT_ID"),
        _ => string.Empty
    };

    private static string GetEnvironmentClientId(string variableName)
    {
        return Environment.GetEnvironmentVariable(variableName) ?? string.Empty;
    }

    private record OAuthEndpoints(string AuthEndpoint, string TokenEndpoint, string Scopes);

    private class OAuthState
    {
        public string Provider { get; set; } = string.Empty;
        public string CodeVerifier { get; set; } = string.Empty;
        public string TokenEndpoint { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
