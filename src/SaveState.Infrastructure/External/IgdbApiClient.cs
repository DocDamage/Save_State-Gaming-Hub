using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Configuration;
using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Infrastructure.External;

public class IgdbApiClient : IIgdbApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IgdbOptions _options;
    private readonly ILogger<IgdbApiClient> _logger;
    private readonly ITimeProvider _timeProvider;
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public IgdbApiClient(
        HttpClient httpClient,
        IHttpClientFactory httpClientFactory,
        IOptions<IgdbOptions> options,
        ILogger<IgdbApiClient> logger,
        ITimeProvider timeProvider)
    {
        _httpClient = httpClient;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    private async Task EnsureAccessTokenAsync(CancellationToken ct)
    {
        if (_accessToken != null && _timeProvider.UtcNow < _tokenExpiry)
        {
            return;
        }

        try
        {
            var authUrl = $"https://id.twitch.tv/oauth2/token?client_id={_options.ClientId}&client_secret={_options.ClientSecret}&grant_type=client_credentials";

            // Use IHttpClientFactory to create a client for Twitch authentication
            var authClient = _httpClientFactory.CreateClient("TwitchAuth");
            var response = await authClient.PostAsync(authUrl, null, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new IgdbAuthenticationException($"Failed to get IGDB access token from Twitch. Status: {response.StatusCode}");
            }

            var json = await response.Content.ReadFromJsonAsync<TwitchAuthResponse>(ct).ConfigureAwait(false);
            _accessToken = json?.AccessToken;
            _tokenExpiry = _timeProvider.UtcNow.AddSeconds(json?.ExpiresIn ?? 0).AddMinutes(-5); // Buffer

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Client-ID", _options.ClientId);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
        }
        catch (IgdbAuthenticationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate with Twitch for IGDB API");
            throw new IgdbAuthenticationException("Authentication with Twitch failed", ex);
        }
    }

    public async Task<IReadOnlyList<IgdbGame>> SearchGamesAsync(string title, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_options.ClientId))
        {
            _logger.LogWarning("IGDB Client ID is missing. Returning empty list.");
            return Array.Empty<IgdbGame>();
        }

        try
        {
            await EnsureAccessTokenAsync(ct).ConfigureAwait(false);

            var query = $"search \"{title}\"; fields name, summary, first_release_date, genres.name, cover.url; limit 10;";
            var request = new HttpRequestMessage(HttpMethod.Post, "games")
            {
                Content = new StringContent(query)
            };

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return Array.Empty<IgdbGame>();

            var games = await response.Content.ReadFromJsonAsync<List<IgdbGame>>(ct).ConfigureAwait(false);
            return games ?? new List<IgdbGame>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search games on IGDB");
            return Array.Empty<IgdbGame>();
        }
    }

    public async Task<Result<GameMetadata>> GetGameDetailsAsync(string gameId, CancellationToken ct = default)
    {
        try
        {
            await EnsureAccessTokenAsync(ct).ConfigureAwait(false);

            var query = $"fields name, summary, first_release_date, genres.name, cover.url; where id = {gameId};";
            var request = new HttpRequestMessage(HttpMethod.Post, "games")
            {
                Content = new StringContent(query)
            };

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<GameMetadata>($"IGDB API returned status {(int)response.StatusCode}", ErrorType.External);
            }

            var games = await response.Content.ReadFromJsonAsync<List<IgdbGame>>(ct).ConfigureAwait(false);
            var game = games?.FirstOrDefault();

            if (game == null)
            {
                return Result.Failure<GameMetadata>($"Game '{gameId}' was not found in IGDB response", ErrorType.NotFound);
            }

            var metadata = new GameMetadata
            {
                Title = game.Name,
                Description = game.Summary,
                ReleaseDate = game.FirstReleaseDate,
                Genres = game.Genres?.Select(g => g.Name).ToArray(),
                CoverImageUrl = game.Cover?.Url
            };

            return Result.Success(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get game details for IGDB ID {GameId}", gameId);
            return Result.Failure<GameMetadata>($"Failed to get IGDB game details: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result<byte[]>> DownloadImageAsync(string imageUrl, CancellationToken ct = default)
    {
        try
        {
            // Normalize URL if it starts with //
            if (imageUrl.StartsWith("//"))
            {
                imageUrl = "https:" + imageUrl;
            }

            var response = await _httpClient.GetAsync(imageUrl, ct).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var imageBytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
                return Result.Success<byte[]>(imageBytes);
            }
            return Result.Failure<byte[]>($"HTTP {response.StatusCode}: {response.ReasonPhrase}", ErrorType.Internal);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download image from {ImageUrl}", imageUrl);
            return Result.Failure<byte[]>($"Image download failed: {ex.Message}", ErrorType.Internal);
        }
    }

    private class TwitchAuthResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}

