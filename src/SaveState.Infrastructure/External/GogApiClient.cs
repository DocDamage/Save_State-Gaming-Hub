using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Configuration;
using SaveState.Core.GameLibrary.DTOs;
using System.Diagnostics;

namespace SaveState.Infrastructure.External;

public class GogApiClient : IGogApiClient
{
    private readonly HttpClient _httpClient;
    private readonly GogOptions _options;
    private readonly ILogger<GogApiClient> _logger;

    public GogApiClient(
        HttpClient httpClient,
        IOptions<GogOptions> options,
        ILogger<GogApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GogGame>> GetOwnedGamesAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_options.Username))
        {
            _logger.LogWarning("GOG Username is missing. Returning empty list.");
            return Array.Empty<GogGame>();
        }

        try
        {
            var url = $"https://embed.gog.com/user/data/games";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<GogGame>();
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("owned", out var ownedProperty))
            {
                return Array.Empty<GogGame>();
            }

            var games = new List<GogGame>();
            foreach (var gameId in ownedProperty.EnumerateArray())
            {
                games.Add(new GogGame
                {
                    Id = gameId.GetInt32(),
                    Title = "GOG Game " + gameId.GetInt32()
                });
            }

            return games;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch owned games from GOG");
            return Array.Empty<GogGame>();
        }
    }

    public async Task<GameMetadata> GetGameDetailsAsync(string gameId, CancellationToken ct = default)
    {
        try
        {
            var url = $"https://api.gog.com/products/{gameId}";
            var response = await _httpClient.GetFromJsonAsync<GogProductResponse>(url, ct).ConfigureAwait(false);

            if (response == null) return GameMetadata.Empty;

            return new GameMetadata
            {
                Title = response.Title,
                Description = response.Description,
                ReleaseDate = response.ReleaseDate,
                Developer = response.Developer,
                Publisher = response.Publisher
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch game details for GOG ID {GameId}", gameId);
            return GameMetadata.Empty;
        }
    }

    public async Task<bool> LaunchGameAsync(string gameId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Launching GOG game with ID {GameId}", gameId);

            var psi = new ProcessStartInfo
            {
                FileName = $"goggalaxy://openGameView/{gameId}",
                UseShellExecute = true
            };

            Process.Start(psi);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch GOG game {GameId}", gameId);
            return false;
        }
    }

    private class GogProductResponse
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset? ReleaseDate { get; set; }
        public string? Developer { get; set; }
        public string? Publisher { get; set; }
    }
}
