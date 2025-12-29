using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Common;
using SaveState.Core.Configuration;
using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Infrastructure.External;

public class IgdbApiClient : IIgdbApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IgdbOptions _options;
    private readonly ILogger<IgdbApiClient> _logger;
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public IgdbApiClient(
        HttpClient httpClient,
        IOptions<IgdbOptions> options,
        ILogger<IgdbApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    private async Task EnsureAccessTokenAsync(CancellationToken ct)
    {
        if (_accessToken != null && DateTime.UtcNow < _tokenExpiry)
        {
            return;
        }

        try
        {
            var authUrl = $"https://id.twitch.tv/oauth2/token?client_id={_options.ClientId}&client_secret={_options.ClientSecret}&grant_type=client_credentials";

            // Note: In a production app, we would use a separate HttpClient for auth or
            // ensure the base address doesn't interfere.
            var authClient = new HttpClient();
            var response = await authClient.PostAsync(authUrl, null, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to get IGDB access token from Twitch");
            }

            var json = await response.Content.ReadFromJsonAsync<TwitchAuthResponse>(ct).ConfigureAwait(false);
            _accessToken = json?.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(json?.ExpiresIn ?? 0).AddMinutes(-5); // Buffer

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Client-ID", _options.ClientId);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate with Twitch for IGDB API");
            throw;
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

    public async Task<GameMetadata> GetGameDetailsAsync(string gameId, CancellationToken ct = default)
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
            if (!response.IsSuccessStatusCode) return GameMetadata.Empty;

            var games = await response.Content.ReadFromJsonAsync<List<IgdbGame>>(ct).ConfigureAwait(false);
            var game = games?.FirstOrDefault();

            if (game == null) return GameMetadata.Empty;

            return new GameMetadata
            {
                Title = game.Name,
                Description = game.Summary,
                ReleaseDate = game.FirstReleaseDate,
                Genres = game.Genres?.Select(g => g.Name).ToArray(),
                CoverImageUrl = game.Cover?.Url
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get game details for IGDB ID {GameId}", gameId);
            return GameMetadata.Empty;
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
                return Result<byte[]>.Success(imageBytes);
            }
            return Result<byte[]>.Failure($"HTTP {response.StatusCode}: {response.ReasonPhrase}", ErrorType.Internal);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download image from {ImageUrl}", imageUrl);
            return Result<byte[]>.Failure($"Image download failed: {ex.Message}", ErrorType.Internal);
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
