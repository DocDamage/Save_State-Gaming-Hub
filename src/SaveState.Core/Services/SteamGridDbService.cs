using SaveState.Core.Interfaces;
using SaveState.Core.Models;
using Serilog;
using System.Text.Json;

namespace SaveState.Core.Services;

public class SteamGridDbService : IMetadataProvider
{
    public string Id => "steamgriddb";
    public string Name => "SteamGridDB";

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger = Log.ForContext<SteamGridDbService>();
    private readonly string _cacheDir;
    private string? _apiKey;

    public SteamGridDbService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://www.steamgriddb.com/api/v2/");
        
        _apiKey = Environment.GetEnvironmentVariable("STEAMGRIDDB_API_KEY");
        
        // Setup cache directory
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _cacheDir = Path.Combine(appData, "SaveState", "ImageCache");
        Directory.CreateDirectory(_cacheDir);
    }

    public async Task<GameMetadata?> GetMetadataAsync(string title, string? platformHint = null)
    {
        // SteamGridDB is primarily for images, not full metadata
        var coverUrl = await GetCoverImageAsync(title, platformHint);
        if (coverUrl != null)
        {
            return new GameMetadata { CoverUrl = coverUrl };
        }
        return null;
    }

    public async Task<string?> GetCoverImageAsync(string title, string? platformHint = null)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.Warning("SteamGridDB API key not configured. Set STEAMGRIDDB_API_KEY environment variable.");
            return null;
        }

        try
        {
            // First, search for the game
            var gameId = await SearchGameAsync(title);
            if (gameId == null)
                return null;

            // Then get grids (covers)
            var coverUrl = await GetGridAsync(gameId.Value);
            if (coverUrl == null)
                return null;

            // Download and cache
            var cachedPath = await DownloadAndCacheAsync(coverUrl, $"{gameId}_cover");
            return cachedPath;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to fetch SteamGridDB cover for: {Title}", title);
            return null;
        }
    }

    public async Task<string?> GetCoverByAppIdAsync(int steamAppId)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return null;

        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.GetAsync($"grids/steam/{steamAppId}?dimensions=600x900");
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.GetProperty("success").GetBoolean() &&
                doc.RootElement.TryGetProperty("data", out var data) &&
                data.GetArrayLength() > 0)
            {
                var url = data[0].GetProperty("url").GetString();
                if (url != null)
                {
                    return await DownloadAndCacheAsync(url, $"steam_{steamAppId}_cover");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to fetch cover for Steam app {AppId}", steamAppId);
        }
        return null;
    }

    private async Task<long?> SearchGameAsync(string title)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.GetAsync($"search/autocomplete/{Uri.EscapeDataString(title)}");
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.GetProperty("success").GetBoolean() &&
            doc.RootElement.TryGetProperty("data", out var data) &&
            data.GetArrayLength() > 0)
        {
            return data[0].GetProperty("id").GetInt64();
        }
        return null;
    }

    private async Task<string?> GetGridAsync(long gameId)
    {
        var response = await _httpClient.GetAsync($"grids/game/{gameId}?dimensions=600x900&limit=1");
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.GetProperty("success").GetBoolean() &&
            doc.RootElement.TryGetProperty("data", out var data) &&
            data.GetArrayLength() > 0)
        {
            return data[0].GetProperty("url").GetString();
        }
        return null;
    }

    private async Task<string?> DownloadAndCacheAsync(string url, string filename)
    {
        var extension = Path.GetExtension(new Uri(url).AbsolutePath);
        var cachePath = Path.Combine(_cacheDir, $"{filename}{extension}");

        if (File.Exists(cachePath))
            return cachePath;

        try
        {
            using var imageClient = new HttpClient();
            var imageData = await imageClient.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(cachePath, imageData);
            _logger.Debug("Cached image: {Path}", cachePath);
            return cachePath;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to download image: {Url}", url);
            return null;
        }
    }
}
