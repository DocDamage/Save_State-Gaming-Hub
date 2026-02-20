using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SaveState.Infrastructure.External;

/// <summary>
/// Data retrieved from HowLongToBeat for a game.
/// </summary>
public sealed record HowLongToBeatData(
    string GameId,
    string Title,
    string? ImageUrl,
    TimeSpan? MainStory,
    TimeSpan? MainPlusExtras,
    TimeSpan? Completionist,
    TimeSpan? AllStyles);

/// <summary>
/// Service for retrieving game completion time data from HowLongToBeat.
/// Uses web scraping since there is no official public API.
/// </summary>
public sealed partial class HowLongToBeatService : IHowLongToBeatService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HowLongToBeatService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, HowLongToBeatData> _cache = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);
    private readonly Dictionary<string, DateTime> _cacheTimestamps = new();

    private const string SearchUrl = "https://howlongtobeat.com/api/search";
    private const string BaseUrl = "https://howlongtobeat.com";

    public HowLongToBeatService(
        HttpClient httpClient,
        ILogger<HowLongToBeatService> logger,
        ITimeProvider timeProvider)
    {
        _httpClient = httpClient;
        _logger = logger;
        _timeProvider = timeProvider;

        // Set up headers to mimic browser request
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <inheritdoc />
    public async Task<Result<HowLongToBeatData>> SearchGameAsync(string gameTitle, CancellationToken ct = default)
    {
        try
        {
            // Check cache first
            var cacheKey = gameTitle.ToLowerInvariant().Trim();
            if (_cache.TryGetValue(cacheKey, out var cached) &&
                _cacheTimestamps.TryGetValue(cacheKey, out var timestamp) &&
                _timeProvider.UtcNow - timestamp < CacheDuration)
            {
                LogCacheHit(_logger, gameTitle);
                return Result.Success(cached);
            }

            LogSearching(_logger, gameTitle);

            // Prepare search request body matching HLTB's internal API format
            var searchPayload = new
            {
                searchType = "games",
                searchTerms = gameTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                searchPage = 1,
                size = 5,
                searchOptions = new
                {
                    games = new
                    {
                        userId = 0,
                        platform = "",
                        sortCategory = "popular",
                        rangeCategory = "main",
                        rangeTime = new { min = (int?)null, max = (int?)null },
                        gameplay = new { perspective = "", flow = "", genre = "" },
                        rangeYear = new { min = "", max = "" },
                        modifier = ""
                    },
                    users = new { sortCategory = "postcount" },
                    filter = "",
                    sort = 0,
                    randomizer = 0
                }
            };

            var jsonContent = JsonSerializer.Serialize(searchPayload);
            using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Add required headers
            content.Headers.Add("Referer", "https://howlongtobeat.com");

            using var response = await _httpClient.PostAsync(SearchUrl, content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                LogSearchFailed(_logger, gameTitle, (int)response.StatusCode);
                return Result.Failure<HowLongToBeatData>($"HLTB search failed with status {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseJson);

            if (!doc.RootElement.TryGetProperty("data", out var dataArray) ||
                dataArray.ValueKind != JsonValueKind.Array ||
                dataArray.GetArrayLength() == 0)
            {
                LogNoResults(_logger, gameTitle);
                return Result.Failure<HowLongToBeatData>($"No HLTB data found for '{gameTitle}'");
            }

            // Find best match (first result is usually most relevant)
            var bestMatch = dataArray.EnumerateArray().FirstOrDefault();
            if (bestMatch.ValueKind == JsonValueKind.Undefined)
            {
                return Result.Failure<HowLongToBeatData>($"No HLTB data found for '{gameTitle}'");
            }

            var result = ParseGameData(bestMatch);

            // Cache the result
            _cache[cacheKey] = result;
            _cacheTimestamps[cacheKey] = _timeProvider.UtcNow;

            LogSearchSuccess(_logger, gameTitle, result.Title);
            return Result.Success(result);
        }
        catch (HttpRequestException ex)
        {
            LogNetworkError(_logger, gameTitle, ex);
            return Result.Failure<HowLongToBeatData>($"Network error searching HLTB: {ex.Message}");
        }
        catch (Exception ex)
        {
            LogUnexpectedError(_logger, gameTitle, ex);
            return Result.Failure<HowLongToBeatData>($"Unexpected error: {ex.Message}");
        }
    }

    private static HowLongToBeatData ParseGameData(JsonElement game)
    {
        var gameId = game.TryGetProperty("game_id", out var idProp) ? idProp.GetInt32().ToString() : "";
        var title = game.TryGetProperty("game_name", out var nameProp) ? nameProp.GetString() ?? "" : "";
        var imageUrl = game.TryGetProperty("game_image", out var imgProp)
            ? $"{BaseUrl}/games/{imgProp.GetString()}"
            : null;

        // Times are in seconds
        var mainStory = ParseTimeFromSeconds(game, "comp_main");
        var mainPlusExtras = ParseTimeFromSeconds(game, "comp_plus");
        var completionist = ParseTimeFromSeconds(game, "comp_100");
        var allStyles = ParseTimeFromSeconds(game, "comp_all");

        return new HowLongToBeatData(
            gameId,
            title,
            imageUrl,
            mainStory,
            mainPlusExtras,
            completionist,
            allStyles);
    }

    private static TimeSpan? ParseTimeFromSeconds(JsonElement game, string property)
    {
        if (!game.TryGetProperty(property, out var prop))
            return null;

        // HLTB returns time in seconds
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var seconds) && seconds > 0)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        return null;
    }

    /// <summary>
    /// Formats a timespan as a user-friendly string (e.g., "25h", "42½h")
    /// </summary>
    public static string FormatPlaytime(TimeSpan? time)
    {
        if (time == null || time.Value.TotalHours < 1)
            return "--";

        var hours = time.Value.TotalHours;
        if (hours < 1) return "< 1h";
        if (hours % 1 >= 0.25 && hours % 1 < 0.75)
            return $"{(int)hours}½h";
        return $"{Math.Round(hours)}h";
    }

    #region LoggerMessage Definitions

    [LoggerMessage(Level = LogLevel.Debug, Message = "HLTB cache hit for '{GameTitle}'")]
    private static partial void LogCacheHit(ILogger logger, string gameTitle);

    [LoggerMessage(Level = LogLevel.Information, Message = "Searching HLTB for '{GameTitle}'")]
    private static partial void LogSearching(ILogger logger, string gameTitle);

    [LoggerMessage(Level = LogLevel.Warning, Message = "HLTB search failed for '{GameTitle}' with status {StatusCode}")]
    private static partial void LogSearchFailed(ILogger logger, string gameTitle, int statusCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "No HLTB results found for '{GameTitle}'")]
    private static partial void LogNoResults(ILogger logger, string gameTitle);

    [LoggerMessage(Level = LogLevel.Information, Message = "HLTB search success for '{GameTitle}' -> '{MatchedTitle}'")]
    private static partial void LogSearchSuccess(ILogger logger, string gameTitle, string matchedTitle);

    [LoggerMessage(Level = LogLevel.Error, Message = "Network error searching HLTB for '{GameTitle}'")]
    private static partial void LogNetworkError(ILogger logger, string gameTitle, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error searching HLTB for '{GameTitle}'")]
    private static partial void LogUnexpectedError(ILogger logger, string gameTitle, Exception ex);

    #endregion
}

/// <summary>
/// Interface for HowLongToBeat service.
/// </summary>
public interface IHowLongToBeatService
{
    /// <summary>
    /// Searches for a game on HowLongToBeat and returns completion time data.
    /// </summary>
    Task<Result<HowLongToBeatData>> SearchGameAsync(string gameTitle, CancellationToken ct = default);
}
