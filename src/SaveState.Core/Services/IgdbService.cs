using SaveState.Core.Interfaces;
using SaveState.Core.Models;
using Serilog;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SaveState.Core.Services;

public class IgdbService : IMetadataProvider
{
    public string Id => "igdb";
    public string Name => "IGDB";

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger = Log.ForContext<IgdbService>();
    
    // Twitch OAuth credentials - should be in config
    private string? _clientId;
    private string? _clientSecret;
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public IgdbService(HttpClient httpClient, IAppConfiguration config)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(config.GetApiEndpoint("IGDB", "https://api.igdb.com/v4/"));

        // Load from environment or config
        _clientId = Environment.GetEnvironmentVariable("TWITCH_CLIENT_ID");
        _clientSecret = Environment.GetEnvironmentVariable("TWITCH_CLIENT_SECRET");
    }

    public async Task<GameMetadata?> GetMetadataAsync(string title, string? platformHint = null)
    {
        if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
        {
            _logger.Warning("IGDB credentials not configured. Set TWITCH_CLIENT_ID and TWITCH_CLIENT_SECRET environment variables.");
            return null;
        }

        try
        {
            await EnsureAuthenticatedAsync();

            var query = $@"search ""{EscapeQuery(title)}""; 
                fields name, summary, first_release_date, genres.name, 
                       involved_companies.company.name, involved_companies.developer, 
                       involved_companies.publisher, rating, cover.url;
                limit 1;";

            var request = new HttpRequestMessage(HttpMethod.Post, "games");
            request.Headers.Add("Client-ID", _clientId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            request.Content = new StringContent(query, Encoding.UTF8, "text/plain");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var games = doc.RootElement;

            if (games.GetArrayLength() == 0)
                return null;

            var game = games[0];
            var metadata = new GameMetadata
            {
                Title = game.TryGetProperty("name", out var n) ? n.GetString() : null,
                Description = game.TryGetProperty("summary", out var s) ? s.GetString() : null,
                IgdbId = game.TryGetProperty("id", out var id) ? id.GetInt64() : null
            };

            if (game.TryGetProperty("first_release_date", out var rd))
            {
                metadata.ReleaseDate = DateTimeOffset.FromUnixTimeSeconds(rd.GetInt64()).DateTime;
            }

            if (game.TryGetProperty("rating", out var r))
            {
                metadata.Rating = r.GetDouble();
            }

            if (game.TryGetProperty("cover", out var cover) && cover.TryGetProperty("url", out var url))
            {
                // Convert to high-res URL
                metadata.CoverUrl = url.GetString()?.Replace("t_thumb", "t_cover_big");
            }

            if (game.TryGetProperty("genres", out var genres))
            {
                foreach (var genre in genres.EnumerateArray())
                {
                    if (genre.TryGetProperty("name", out var gn))
                        metadata.Genres.Add(gn.GetString()!);
                }
            }

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to fetch IGDB metadata for: {Title}", title);
            return null;
        }
    }

    public async Task<string?> GetCoverImageAsync(string title, string? platformHint = null)
    {
        var metadata = await GetMetadataAsync(title, platformHint);
        return metadata?.CoverUrl;
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (_accessToken != null && DateTime.UtcNow < _tokenExpiry)
            return;

        var tokenUrl = $"https://id.twitch.tv/oauth2/token?client_id={_clientId}&client_secret={_clientSecret}&grant_type=client_credentials";
        var response = await _httpClient.PostAsync(tokenUrl, null);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        
        _accessToken = doc.RootElement.GetProperty("access_token").GetString();
        var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60);

        _logger.Information("IGDB authentication successful, token expires in {Seconds}s", expiresIn);
    }

    private string EscapeQuery(string input)
    {
        return input.Replace("\"", "\\\"");
    }
}
