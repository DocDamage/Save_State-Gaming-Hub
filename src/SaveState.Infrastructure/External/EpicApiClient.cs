using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Configuration;
using SaveState.Core.GameLibrary.DTOs;
using System.Diagnostics;

namespace SaveState.Infrastructure.External;

public class EpicApiClient : IEpicApiClient
{
    private readonly HttpClient _httpClient;
    private readonly EpicOptions _options;
    private readonly ILogger<EpicApiClient> _logger;

    public EpicApiClient(
        HttpClient httpClient,
        IOptions<EpicOptions> options,
        ILogger<EpicApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EpicGame>> GetOwnedGamesAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_options.AuthToken))
        {
            _logger.LogWarning("Epic Auth Token is missing. Returning empty list.");
            return Array.Empty<EpicGame>();
        }

        try
        {
            // Note: Epic API endpoints are complex. This is a simplified implementation
            // showing where the logic would go for a real OAuth-based client.
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://library-service.live.epicgames.com/library/api/public/items?accountId={_options.AccountId}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.AuthToken);

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return Array.Empty<EpicGame>();

            var content = await response.Content.ReadFromJsonAsync<EpicLibraryResponse>(ct).ConfigureAwait(false);

            return content?.Records?.Select(r => new EpicGame
            {
                Id = r.CatalogItemId,
                Title = r.Namespace // Namespace often holds the app name in some contexts, or we'd need another mapping
            }).ToList() ?? new List<EpicGame>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch owned games from Epic");
            return Array.Empty<EpicGame>();
        }
    }

    public async Task<GameMetadata> GetGameDetailsAsync(string gameId, CancellationToken ct = default)
    {
        try
        {
            // Epic Store API is often accessed via catalog-service
            var url = $"https://catalog-public-service-prod06.ol.epicgames.com/catalog/api/shared/bulk/items?id={gameId}";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode) return GameMetadata.Empty;

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty(gameId, out var gameElement)) return GameMetadata.Empty;

            return new GameMetadata
            {
                Title = gameElement.GetProperty("title").GetString() ?? string.Empty,
                Description = gameElement.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                Developer = gameElement.TryGetProperty("developer", out var dev) ? dev.GetString() : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch game details for Epic ID {GameId}", gameId);
            return GameMetadata.Empty;
        }
    }

    public Task<bool> LaunchGameAsync(string gameId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Launching Epic game with ID {GameId}", gameId);

            var psi = new ProcessStartInfo
            {
                FileName = $"com.epicgames.launcher://apps/{gameId}?action=launch",
                UseShellExecute = true
            };

            Process.Start(psi);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch Epic game {GameId}", gameId);
            return Task.FromResult(false);
        }
    }

    private class EpicLibraryResponse
    {
        public List<EpicLibraryRecord>? Records { get; set; }
    }

    private class EpicLibraryRecord
    {
        public string CatalogItemId { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
    }
}
