using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Configuration;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.Monitoring;
using SaveState.Infrastructure.Monitoring;
using System.Diagnostics;

namespace SaveState.Infrastructure.External;

public class SteamApiClient : ISteamApiClient
{
    private readonly HttpClient _httpClient;
    private readonly SteamOptions _options;
    private readonly ILogger<SteamApiClient> _logger;
    private readonly IApplicationMetrics _metrics;
    private readonly ErrorTrackingService _errorTracker;

    public SteamApiClient(
        HttpClient httpClient,
        IOptions<SteamOptions> options,
        ILogger<SteamApiClient> logger,
        IApplicationMetrics metrics,
        ErrorTrackingService errorTracker)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _metrics = metrics;
        _errorTracker = errorTracker;
    }

    public async Task<IReadOnlyList<SteamGame>> GetOwnedGamesAsync(CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;

        if (string.IsNullOrEmpty(_options.ApiKey) || string.IsNullOrEmpty(_options.SteamId))
        {
            _logger.LogWarning("Steam API Key or Steam ID is missing. Returning empty list.");
            _metrics.RecordApiCall("Steam", "GetOwnedGames", DateTime.UtcNow - startTime, false);
            _metrics.RecordApiError("Steam", "ConfigurationMissing");
            return Array.Empty<SteamGame>();
        }

        try
        {
            var url = $"IPlayerService/GetOwnedGames/v1/?key={_options.ApiKey}&steamid={_options.SteamId}&format=json&include_appinfo=1";
            var response = await _httpClient.GetFromJsonAsync<SteamOwnedGamesResponse>(url, ct).ConfigureAwait(false);

            if (response?.Response?.Games == null)
            {
                var emptyResultDuration = DateTime.UtcNow - startTime;
                _metrics.RecordApiCall("Steam", "GetOwnedGames", emptyResultDuration, true); // Success but no games
                return Array.Empty<SteamGame>();
            }

            var games = response.Response.Games.Select(g => new SteamGame
            {
                AppId = g.AppId,
                Name = g.Name,
                PlayTimeMinutes = g.PlaytimeForever
            }).ToList();

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordApiCall("Steam", "GetOwnedGames", duration, true);

            return games;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Failed to fetch owned games from Steam");

            // Record error metrics and track exception
            _metrics.RecordApiCall("Steam", "GetOwnedGames", duration, false);
            _metrics.RecordApiError("Steam", ex.GetType().Name);
            _errorTracker.RecordException("SteamApiClient.GetOwnedGames", ex.GetType().Name, ex.Message, ex);

            throw new SteamApiException("Failed to fetch owned games from Steam", ex);
        }
    }

    public async Task<GameMetadata> GetGameDetailsAsync(string appId, CancellationToken ct = default)
    {
        try
        {
            // Store API is at store.steampowered.com, but we can call it if base address allows or use absolute URL
            var url = $"https://store.steampowered.com/api/appdetails?appids={appId}";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return GameMetadata.Empty;
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty(appId, out var appElement) ||
                !appElement.GetProperty("success").GetBoolean())
            {
                return GameMetadata.Empty;
            }

            var data = appElement.GetProperty("data");

            return new GameMetadata
            {
                Title = data.GetProperty("name").GetString() ?? string.Empty,
                Description = data.GetProperty("short_description").GetString(),
                ReleaseDate = DateTimeOffset.TryParse(data.GetProperty("release_date").GetProperty("date").GetString(), out var date) ? date : null,
                Developer = data.TryGetProperty("developers", out var devs) ? devs[0].GetString() : null,
                Publisher = data.TryGetProperty("publishers", out var pubs) ? pubs[0].GetString() : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch game details for Steam AppId {AppId}", appId);
            return GameMetadata.Empty;
        }
    }

    public Task<bool> LaunchGameAsync(string appId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Launching Steam game with AppId {AppId}", appId);

            var psi = new ProcessStartInfo
            {
                FileName = $"steam://run/{appId}",
                UseShellExecute = true
            };

            Process.Start(psi);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch Steam game {AppId}", appId);
            return Task.FromResult(false);
        }
    }

    private class SteamOwnedGamesResponse
    {
        public SteamOwnedGamesContent? Response { get; set; }
    }

    private class SteamOwnedGamesContent
    {
        public int GameCount { get; set; }
        public List<SteamGameInternal>? Games { get; set; }
    }

    private class SteamGameInternal
    {
        public int AppId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int PlaytimeForever { get; set; }
    }
}
