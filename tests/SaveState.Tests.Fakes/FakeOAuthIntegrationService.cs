using SaveState.Core.Common;
using SaveState.Core.WebBrowser.Models;
using SaveState.Core.WebBrowser.Services;

namespace SaveState.Tests.Fakes;

/// <summary>
/// Fake implementation of IOAuthIntegrationService for integration testing.
/// </summary>
public class FakeOAuthIntegrationService : IOAuthIntegrationService
{
    public event EventHandler<OAuthCallback>? OnOAuthCallback;

    public Task<Result<string>> AuthenticateXboxAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result<string>.Success("fake_xbox_token"));
    }

    public Task<Result<string>> AuthenticatePlayStationAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result<string>.Success("fake_playstation_token"));
    }

    public Task<Result<string>> AuthenticateSteamAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result<string>.Success("fake_steam_token"));
    }

    public Task<Result<string>> AuthenticateEpicAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result<string>.Success("fake_epic_token"));
    }

    public Task<Result<string>> AuthenticateGogAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result<string>.Success("fake_gog_token"));
    }

    public Task<Result<string>> AuthenticateGeForceNowAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result<string>.Success("fake_geforce_now_token"));
    }

    public Task<Result<string>> AuthenticateXboxCloudAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result<string>.Success("fake_xbox_cloud_token"));
    }

    public Task<Result<string>> AuthenticateAmazonLunaAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result<string>.Success("fake_amazon_luna_token"));
    }

    public Task<Result<string>> StartOAuthFlowAsync(
        string providerName,
        string authorizationEndpoint,
        string tokenEndpoint,
        string clientId,
        string redirectUri,
        string[] scopes,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<string>.Success($"fake_{providerName}_token"));
    }

    public Task<Result<string>> RefreshTokenAsync(
        string providerName,
        string refreshToken,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<string>.Success($"fake_{providerName}_refreshed_token"));
    }

    public Task<Result> RevokeTokenAsync(
        string providerName,
        string accessToken,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Simulates an OAuth callback.
    /// </summary>
    public void SimulateOAuthCallback(OAuthCallback callback)
    {
        OnOAuthCallback?.Invoke(this, callback);
    }
}
