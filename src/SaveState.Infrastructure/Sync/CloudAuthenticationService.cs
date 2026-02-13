using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Sync;
using System.Diagnostics;
using System.Text;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Infrastructure.Sync;

/// <summary>
/// Infrastructure implementation of ICloudAuthenticationService using local HTTP listener and system browser.
/// </summary>
public class CloudAuthenticationService : ICloudAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CloudAuthenticationService> _logger;

    public CloudAuthenticationService(HttpClient httpClient, ILogger<CloudAuthenticationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<OAuth2TokenResponse>> AuthenticateAsync(
        string providerName,
        string clientId,
        string[] scopes,
        string authUrl,
        string tokenUrl,
        CancellationToken ct = default)
    {
        var redirectUri = "http://localhost:5000/callback/";
        using var listener = new HttpListener();
        listener.Prefixes.Add(redirectUri);
        listener.Start();

        try
        {
            var state = Guid.NewGuid().ToString("N");
            var scopeString = string.Join(" ", scopes);
            var encodedScopes = WebUtility.UrlEncode(scopeString);

            var fullAuthUrl = $"{authUrl}?client_id={clientId}&scope={encodedScopes}&response_type=code&redirect_uri={WebUtility.UrlEncode(redirectUri)}&state={state}";

            _logger.LogInformation("Opening browser for {Provider} authentication...", providerName);
            Process.Start(new ProcessStartInfo(fullAuthUrl) { UseShellExecute = true });

            _logger.LogInformation("Waiting for authorization code on {RedirectUri}...", redirectUri);

            // Wait for the callback
            var context = await listener.GetContextAsync().WaitAsync(ct).ConfigureAwait(false);
            var request = context.Request;
            var response = context.Response;

            var code = request.QueryString["code"];
            var receivedState = request.QueryString["state"];

            if (string.IsNullOrEmpty(code) || receivedState != state)
            {
                _logger.LogWarning("Invalid authentication response received (state mismatch or missing code)");
                var errorBytes = Encoding.UTF8.GetBytes("Authentication failed. You can close this window.");
                response.OutputStream.Write(errorBytes, 0, errorBytes.Length);
                response.Close();
                return Result.Failure<OAuth2TokenResponse>("Authentication failed: state mismatch or missing authorization code", ErrorType.Unauthorized);
            }

            var successBytes = Encoding.UTF8.GetBytes("Authentication successful! You can return to SaveState Reborn and close this window.");
            response.OutputStream.Write(successBytes, 0, successBytes.Length);
            response.Close();

            _logger.LogInformation("Exchanging authorization code for tokens...");
            return await ExchangeCodeForTokenAsync(clientId, code, redirectUri, tokenUrl, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OAuth2 authentication was cancelled");
            return Result.Failure<OAuth2TokenResponse>("Authentication was cancelled", ErrorType.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth2 authentication flow failed");
            return Result.Failure<OAuth2TokenResponse>($"Authentication failed: {ex.Message}", ErrorType.ExternalService);
        }
        finally
        {
            listener.Stop();
        }
    }

    public async Task<Result<OAuth2TokenResponse>> RefreshTokenAsync(
        string clientId,
        string refreshToken,
        string tokenUrl,
        CancellationToken ct = default)
    {
        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken)
            });

            var response = await _httpClient.PostAsync(tokenUrl, content, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Token refresh failed: {Status} {Error}", response.StatusCode, errorBody);
                return Result.Failure<OAuth2TokenResponse>($"Token refresh failed: {response.StatusCode}", ErrorType.Unauthorized);
            }

            var data = await response.Content.ReadFromJsonAsync<TokenResponseData>(cancellationToken: ct).ConfigureAwait(false);
            if (data == null)
            {
                return Result.Failure<OAuth2TokenResponse>("Failed to parse token response", ErrorType.ExternalService);
            }

            return Result.Success(new OAuth2TokenResponse(
                data.access_token,
                data.refresh_token ?? refreshToken,
                DateTime.UtcNow.AddSeconds(data.expires_in),
                data.scope?.Split(' ') ?? Array.Empty<string>()));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Token refresh was cancelled");
            return Result.Failure<OAuth2TokenResponse>("Token refresh was cancelled", ErrorType.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return Result.Failure<OAuth2TokenResponse>($"Token refresh failed: {ex.Message}", ErrorType.ExternalService);
        }
    }

    private async Task<Result<OAuth2TokenResponse>> ExchangeCodeForTokenAsync(
        string clientId,
        string code,
        string redirectUri,
        string tokenUrl,
        CancellationToken ct)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", redirectUri)
        });

        var response = await _httpClient.PostAsync(tokenUrl, content, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogError("Token exchange failed: {Status} {Error}", response.StatusCode, errorBody);
            return Result.Failure<OAuth2TokenResponse>($"Token exchange failed: {response.StatusCode} - {errorBody}", ErrorType.Unauthorized);
        }

        var data = await response.Content.ReadFromJsonAsync<TokenResponseData>(cancellationToken: ct).ConfigureAwait(false);
        if (data == null)
        {
            return Result.Failure<OAuth2TokenResponse>("Failed to parse token response", ErrorType.ExternalService);
        }

        return Result.Success(new OAuth2TokenResponse(
            data.access_token,
            data.refresh_token,
            DateTime.UtcNow.AddSeconds(data.expires_in),
            data.scope?.Split(' ') ?? Array.Empty<string>()));
    }

    private class TokenResponseData
    {
        public string access_token { get; set; } = string.Empty;
        public string? refresh_token { get; set; }
        public int expires_in { get; set; }
        public string? scope { get; set; }
    }
}
