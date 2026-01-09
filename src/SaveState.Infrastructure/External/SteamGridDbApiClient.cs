using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Common;
using SaveState.Core.Common.Configuration;

namespace SaveState.Infrastructure.External;

public class SteamGridDbApiClient : ISteamGridDbApiClient
{
    private readonly HttpClient _httpClient;
    private readonly SteamGridDbOptions _options;
    private readonly ILogger<SteamGridDbApiClient> _logger;
    private readonly SemaphoreSlim _rateLimiter;

    public SteamGridDbApiClient(
        HttpClient httpClient,
        IOptions<SteamGridDbOptions> options,
        ILogger<SteamGridDbApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _rateLimiter = new SemaphoreSlim(_options.MaxConcurrentRequests);

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = _options.RequestTimeout;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SaveState", "1.0"));
    }

    public async Task<IReadOnlyList<SteamGridDbGrid>> SearchGridsAsync(string query, CancellationToken ct = default)
    {
        await _rateLimiter.WaitAsync(ct);

        try
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var response = await _httpClient.GetAsync($"search/autocomplete/{encodedQuery}", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("SteamGridDB search failed with status {StatusCode} for query '{Query}'",
                    response.StatusCode, query);
                return Array.Empty<SteamGridDbGrid>();
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            var searchResponse = JsonSerializer.Deserialize<SteamGridDbSearchResponse>(content);

            if (searchResponse?.Data == null)
                return Array.Empty<SteamGridDbGrid>();

            // Get grids for the first game result
            var firstGame = searchResponse.Data.FirstOrDefault();
            if (firstGame != null)
            {
                return await GetGridsBySteamIdAsync(firstGame.Id, ct);
            }

            return Array.Empty<SteamGridDbGrid>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search SteamGridDB for '{Query}'", query);
            throw new SteamGridDbApiException($"Search failed for '{query}': {ex.Message}", ex);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    public async Task<IReadOnlyList<SteamGridDbGrid>> GetGridsBySteamIdAsync(int steamId, CancellationToken ct = default)
    {
        await _rateLimiter.WaitAsync(ct);

        try
        {
            var typesParam = string.Join(',', _options.PreferredImageTypes);
            var response = await _httpClient.GetAsync($"grids/steam/{steamId}?types={typesParam}", ct);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("No grids found for Steam ID {SteamId}", steamId);
                    return Array.Empty<SteamGridDbGrid>();
                }

                _logger.LogWarning("SteamGridDB grids request failed with status {StatusCode} for Steam ID {SteamId}",
                    response.StatusCode, steamId);
                return Array.Empty<SteamGridDbGrid>();
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            var gridsResponse = JsonSerializer.Deserialize<SteamGridDbGridsResponse>(content);

            return gridsResponse?.Data ?? Array.Empty<SteamGridDbGrid>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get grids for Steam ID {SteamId}", steamId);
            throw new SteamGridDbApiException($"Failed to get grids for Steam ID {steamId}: {ex.Message}", ex);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    public async Task<Result<byte[]>> DownloadImageAsync(string imageUrl, CancellationToken ct = default)
    {
        await _rateLimiter.WaitAsync(ct);

        try
        {
            var response = await _httpClient.GetAsync(imageUrl, ct);

            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<byte[]>($"Failed to download image: HTTP {response.StatusCode}", ErrorType.ExternalService);
            }

            var imageData = await response.Content.ReadAsByteArrayAsync(ct);
            return Result.Success<byte[]>(imageData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download image from {Url}", imageUrl);
            return Result.Failure<byte[]>($"Failed to download image: {ex.Message}", ErrorType.ExternalService);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private class SteamGridDbSearchResponse
    {
        public SteamGridDbGame[] Data { get; set; } = Array.Empty<SteamGridDbGame>();
    }

    private class SteamGridDbGame
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class SteamGridDbGridsResponse
    {
        public SteamGridDbGrid[] Data { get; set; } = Array.Empty<SteamGridDbGrid>();
    }
}
