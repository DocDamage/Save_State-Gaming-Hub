using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.WebBrowser.Models;

namespace SaveState.Infrastructure.WebBrowser.Services;

/// <summary>
/// Specialized handler for OAuth 2.0 authentication flows.
/// Supports multiple providers including Steam, Epic, GOG, Discord, and custom OAuth providers.
/// </summary>
public sealed class OAuthHandler : IDisposable
{
    private readonly ILogger<OAuthHandler> _logger;
    private readonly CefSharpBrowserService _browserService;
    private readonly Dictionary<string, OAuthProviderConfig> _providers;
    private TaskCompletionSource<OAuthCallback>? _activeFlow;
    private string? _activeProvider;
    private string? _expectedRedirectUri;
    private Guid? _oauthTabId;

    public OAuthHandler(
        ILogger<OAuthHandler> logger,
        CefSharpBrowserService browserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _browserService = browserService ?? throw new ArgumentNullException(nameof(browserService));
        
        _providers = InitializeProviders();
        _browserService.AddressChanged += OnAddressChanged;
    }

    /// <summary>
    /// Gets the list of supported OAuth providers.
    /// </summary>
    public IReadOnlyList<string> SupportedProviders => _providers.Keys.ToList();

    /// <summary>
    /// Starts an OAuth authentication flow for the specified provider.
    /// </summary>
    public async Task<Result<OAuthCallback>> StartOAuthFlowAsync(
        string provider,
        string? customAuthorizationUrl = null,
        string? customRedirectUri = null,
        CancellationToken cancellationToken = default)
    {
        if (_activeFlow != null && !_activeFlow.Task.IsCompleted)
        {
            return Result<OAuthCallback>.Failure("Another OAuth flow is already in progress");
        }

        if (!_providers.TryGetValue(provider.ToLowerInvariant(), out var config) && 
            string.IsNullOrEmpty(customAuthorizationUrl))
        {
            return Result<OAuthCallback>.Failure($"Unknown OAuth provider: {provider}");
        }

        try
        {
            _activeProvider = provider;
            _activeFlow = new TaskCompletionSource<OAuthCallback>();
            
            var authUrl = customAuthorizationUrl ?? config!.AuthorizationUrl;
            _expectedRedirectUri = customRedirectUri ?? config!.RedirectUri;

            _logger.LogInformation("Starting OAuth flow for provider: {Provider}", provider);

            // Create a new tab for OAuth
            var tabResult = await _browserService.CreateTabAsync(authUrl, activate: true);
            if (tabResult.IsFailure)
            {
                return Result<OAuthCallback>.Failure($"Failed to create OAuth tab: {tabResult.Error}");
            }

            _oauthTabId = tabResult.Value!.Id;

            // Set up cancellation
            using (cancellationToken.Register(() => 
            {
                _activeFlow?.TrySetCanceled();
                CleanupOAuthTab();
            }))
            {
                var callback = await _activeFlow.Task;
                callback.Provider = provider;
                
                _logger.LogInformation("OAuth flow completed for provider: {Provider}", provider);
                return Result<OAuthCallback>.Success(callback);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OAuth flow cancelled for provider: {Provider}", provider);
            CleanupOAuthTab();
            return Result<OAuthCallback>.Failure("OAuth flow was cancelled", ErrorType.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth flow failed for provider: {Provider}", provider);
            CleanupOAuthTab();
            return Result<OAuthCallback>.Failure($"OAuth flow failed: {ex.Message}");
        }
        finally
        {
            _activeFlow = null;
            _activeProvider = null;
            _expectedRedirectUri = null;
            _oauthTabId = null;
        }
    }

    /// <summary>
    /// Cancels the current OAuth flow if one is active.
    /// </summary>
    public void CancelCurrentFlow()
    {
        if (_activeFlow != null && !_activeFlow.Task.IsCompleted)
        {
            _logger.LogInformation("Cancelling active OAuth flow");
            _activeFlow.TrySetCanceled();
            CleanupOAuthTab();
        }
    }

    /// <summary>
    /// Gets the authorization URL for a provider with the required parameters.
    /// </summary>
    public string BuildAuthorizationUrl(
        string provider,
        string clientId,
        string redirectUri,
        string scope,
        string? state = null)
    {
        if (!_providers.TryGetValue(provider.ToLowerInvariant(), out var config))
        {
            throw new ArgumentException($"Unknown provider: {provider}", nameof(provider));
        }

        var url = config.AuthorizationUrlTemplate
            .Replace("{CLIENT_ID}", Uri.EscapeDataString(clientId))
            .Replace("{REDIRECT_URI}", Uri.EscapeDataString(redirectUri))
            .Replace("{SCOPE}", Uri.EscapeDataString(scope))
            .Replace("{STATE}", Uri.EscapeDataString(state ?? Guid.NewGuid().ToString("N")));

        return url;
    }

    /// <summary>
    /// Registers a custom OAuth provider configuration.
    /// </summary>
    public void RegisterProvider(string name, OAuthProviderConfig config)
    {
        _providers[name.ToLowerInvariant()] = config;
        _logger.LogInformation("Registered OAuth provider: {Provider}", name);
    }

    private void OnAddressChanged(object? sender, (Guid TabId, string Url) e)
    {
        if (_activeFlow?.Task.IsCompleted != false) return;
        if (_oauthTabId != e.TabId) return;
        if (string.IsNullOrEmpty(_expectedRedirectUri)) return;

        try
        {
            var url = e.Url;
            
            // Check if this is a redirect URI match
            if (!IsRedirectUriMatch(url, _expectedRedirectUri))
                return;

            var callback = ParseOAuthCallback(url);
            
            if (!string.IsNullOrEmpty(callback.Error))
            {
                _logger.LogWarning("OAuth error received: {Error}", callback.Error);
            }

            _activeFlow.TrySetResult(callback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OAuth redirect");
        }
    }

    private bool IsRedirectUriMatch(string url, string redirectUri)
    {
        try
        {
            var urlUri = new Uri(url);
            var redirectUriObj = new Uri(redirectUri);

            // Match scheme and host
            return urlUri.Scheme == redirectUriObj.Scheme &&
                   urlUri.Host == redirectUriObj.Host &&
                   urlUri.AbsolutePath == redirectUriObj.AbsolutePath;
        }
        catch
        {
            // Fallback to simple string matching
            return url.StartsWith(redirectUri, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static OAuthCallback ParseOAuthCallback(string url)
    {
        var uri = new Uri(url);
        var query = ParseQueryString(uri.Query);
        var fragment = ParseQueryString(uri.Fragment.TrimStart('#'));

        // Check fragment first (implicit flow), then query (authorization code flow)
        var code = fragment.GetValueOrDefault("code") ?? query.GetValueOrDefault("code") ?? string.Empty;
        var state = fragment.GetValueOrDefault("state") ?? query.GetValueOrDefault("state");
        var error = fragment.GetValueOrDefault("error") ?? query.GetValueOrDefault("error");
        var errorDescription = fragment.GetValueOrDefault("error_description") ?? query.GetValueOrDefault("error_description");

        var callback = new OAuthCallback
        {
            Code = code,
            State = state,
            Error = error
        };

        if (!string.IsNullOrEmpty(errorDescription))
        {
            callback.AdditionalData["error_description"] = errorDescription;
        }

        // Add any additional query parameters
        var allParams = new Dictionary<string, string>(query);
        foreach (var kvp in fragment)
        {
            allParams[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in allParams)
        {
            if (kvp.Key != "code" && kvp.Key != "state" && kvp.Key != "error" && kvp.Key != "error_description")
            {
                callback.AdditionalData[kvp.Key] = kvp.Value;
            }
        }

        // Extract access token if present (implicit flow)
        if (fragment.TryGetValue("access_token", out var accessToken))
        {
            callback.AdditionalData["access_token"] = accessToken;
        }

        return callback;
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(query)) return result;

        // Remove leading ? if present
        if (query.StartsWith('?'))
            query = query.Substring(1);

        var pairs = query.Split('&');
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = Uri.UnescapeDataString(parts[0]);
                var value = Uri.UnescapeDataString(parts[1]);
                result[key] = value;
            }
            else if (parts.Length == 1)
            {
                result[Uri.UnescapeDataString(parts[0])] = string.Empty;
            }
        }

        return result;
    }

    private void CleanupOAuthTab()
    {
        if (_oauthTabId.HasValue)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _browserService.CloseTabAsync(_oauthTabId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing OAuth tab");
                }
            });
        }
    }

    private static Dictionary<string, OAuthProviderConfig> InitializeProviders()
    {
        return new Dictionary<string, OAuthProviderConfig>(StringComparer.OrdinalIgnoreCase)
        {
            ["steam"] = new OAuthProviderConfig
            {
                Name = "Steam",
                AuthorizationUrl = "https://steamcommunity.com/openid/login",
                AuthorizationUrlTemplate = "https://steamcommunity.com/openid/login?openid.ns=http://specs.openid.net/auth/2.0&openid.mode=checkid_setup&openid.return_to={REDIRECT_URI}&openid.realm={REDIRECT_URI}&openid.identity=http://specs.openid.net/auth/2.0/identifier_select&openid.claimed_id=http://specs.openid.net/auth/2.0/identifier_select",
                RedirectUri = "http://localhost:8080/auth/steam/callback",
                Scopes = new[] { "identity" }
            },
            ["epic"] = new OAuthProviderConfig
            {
                Name = "Epic Games",
                AuthorizationUrlTemplate = "https://www.epicgames.com/id/authorize?client_id={CLIENT_ID}&redirect_uri={REDIRECT_URI}&response_type=code&scope={SCOPE}&state={STATE}",
                RedirectUri = "http://localhost:8080/auth/epic/callback",
                Scopes = new[] { "basic_profile" }
            },
            ["gog"] = new OAuthProviderConfig
            {
                Name = "GOG",
                AuthorizationUrlTemplate = "https://auth.gog.com/auth?client_id={CLIENT_ID}&redirect_uri={REDIRECT_URI}&response_type=code&layout=client_gl&state={STATE}",
                RedirectUri = "http://localhost:8080/auth/gog/callback",
                Scopes = new[] { "client" }
            },
            ["discord"] = new OAuthProviderConfig
            {
                Name = "Discord",
                AuthorizationUrlTemplate = "https://discord.com/oauth2/authorize?client_id={CLIENT_ID}&redirect_uri={REDIRECT_URI}&response_type=code&scope={SCOPE}&state={STATE}",
                RedirectUri = "http://localhost:8080/auth/discord/callback",
                Scopes = new[] { "identify", "email" }
            },
            ["twitch"] = new OAuthProviderConfig
            {
                Name = "Twitch",
                AuthorizationUrlTemplate = "https://id.twitch.tv/oauth2/authorize?client_id={CLIENT_ID}&redirect_uri={REDIRECT_URI}&response_type=code&scope={SCOPE}&state={STATE}",
                RedirectUri = "http://localhost:8080/auth/twitch/callback",
                Scopes = new[] { "user:read:email" }
            },
            ["github"] = new OAuthProviderConfig
            {
                Name = "GitHub",
                AuthorizationUrlTemplate = "https://github.com/login/oauth/authorize?client_id={CLIENT_ID}&redirect_uri={REDIRECT_URI}&scope={SCOPE}&state={STATE}",
                RedirectUri = "http://localhost:8080/auth/github/callback",
                Scopes = new[] { "read:user", "user:email" }
            },
            ["google"] = new OAuthProviderConfig
            {
                Name = "Google",
                AuthorizationUrlTemplate = "https://accounts.google.com/o/oauth2/v2/auth?client_id={CLIENT_ID}&redirect_uri={REDIRECT_URI}&response_type=code&scope={SCOPE}&state={STATE}&access_type=offline&prompt=consent",
                RedirectUri = "http://localhost:8080/auth/google/callback",
                Scopes = new[] { "openid", "email", "profile" }
            },
            ["microsoft"] = new OAuthProviderConfig
            {
                Name = "Microsoft",
                AuthorizationUrlTemplate = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id={CLIENT_ID}&redirect_uri={REDIRECT_URI}&response_type=code&scope={SCOPE}&state={STATE}",
                RedirectUri = "http://localhost:8080/auth/microsoft/callback",
                Scopes = new[] { "openid", "email", "profile", "XboxLive.signin" }
            },
            ["xbox"] = new OAuthProviderConfig
            {
                Name = "Xbox Live",
                AuthorizationUrlTemplate = "https://login.live.com/oauth20_authorize.srf?client_id={CLIENT_ID}&redirect_uri={REDIRECT_URI}&response_type=code&scope={SCOPE}&state={STATE}",
                RedirectUri = "http://localhost:8080/auth/xbox/callback",
                Scopes = new[] { "XboxLive.signin", "XboxLive.offline_access" }
            },
            ["playstation"] = new OAuthProviderConfig
            {
                Name = "PlayStation Network",
                AuthorizationUrlTemplate = "https://ca.account.sony.com/api/authz/v3/oauth/authorize?client_id={CLIENT_ID}&redirect_uri={REDIRECT_URI}&response_type=code&scope={SCOPE}&state={STATE}&access_type=offline",
                RedirectUri = "http://localhost:8080/auth/playstation/callback",
                Scopes = new[] { "psn:account.info", "psn:account.manager" }
            },
            ["nintendo"] = new OAuthProviderConfig
            {
                Name = "Nintendo",
                AuthorizationUrlTemplate = "https://accounts.nintendo.com/connect/1.0.0/authorize?client_id={CLIENT_ID}&redirect_uri={REDIRECT_URI}&response_type=code&scope={SCOPE}&state={STATE}",
                RedirectUri = "http://localhost:8080/auth/nintendo/callback",
                Scopes = new[] { "openid", "user", "user.birthday", "user.mii", "user.email" }
            }
        };
    }

    public void Dispose()
    {
        _browserService.AddressChanged -= OnAddressChanged;
        CancelCurrentFlow();
    }
}

/// <summary>
/// Configuration for an OAuth provider.
/// </summary>
public sealed class OAuthProviderConfig
{
    public required string Name { get; set; }
    public string? AuthorizationUrl { get; set; }
    public required string AuthorizationUrlTemplate { get; set; }
    public required string RedirectUri { get; set; }
    public required string[] Scopes { get; set; }
    public string? TokenUrl { get; set; }
    public string? UserInfoUrl { get; set; }
}
