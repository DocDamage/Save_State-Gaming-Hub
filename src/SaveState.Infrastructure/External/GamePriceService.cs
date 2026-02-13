using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using System.Text.Json;

namespace SaveState.Infrastructure.External;

/// <summary>
/// Price data for a game from various stores.
/// </summary>
public sealed record GamePriceData(
    string GameId,
    string Title,
    decimal CurrentPrice,
    decimal RegularPrice,
    decimal LowestPrice,
    DateTime? LowestPriceDate,
    string StoreName,
    string StoreUrl,
    decimal DiscountPercent,
    bool IsOnSale);

/// <summary>
/// Historical price point for tracking.
/// </summary>
public sealed record PriceHistoryPoint(
    DateTime Date,
    decimal Price,
    string StoreName);

/// <summary>
/// Service for tracking game prices using IsThereAnyDeal API.
/// Requires API key from https://isthereanydeal.com/dev/app/
/// </summary>
public sealed partial class GamePriceService : IGamePriceService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GamePriceService> _logger;
    private readonly string? _apiKey;
    private readonly Dictionary<string, (GamePriceData Data, DateTime Cached)> _priceCache = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);

    private const string BaseUrl = "https://api.isthereanydeal.com";

    public GamePriceService(
        HttpClient httpClient,
        ILogger<GamePriceService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Get API key from environment variable
        _apiKey = Environment.GetEnvironmentVariable("ITAD_API_KEY");

        if (string.IsNullOrEmpty(_apiKey))
        {
            LogNoApiKey(_logger);
        }
    }

    /// <inheritdoc />
    public async Task<Result<GamePriceData>> GetCurrentPriceAsync(string gameTitle, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return Result.Failure<GamePriceData>("IsThereAnyDeal API key not configured. Set ITAD_API_KEY environment variable.");
        }

        try
        {
            // Check cache
            var cacheKey = gameTitle.ToLowerInvariant().Trim();
            if (_priceCache.TryGetValue(cacheKey, out var cached) &&
                DateTime.UtcNow - cached.Cached < CacheDuration)
            {
                LogCacheHit(_logger, gameTitle);
                return Result.Success(cached.Data);
            }

            LogSearching(_logger, gameTitle);

            // Step 1: Search for the game to get its ITAD ID
            var searchResult = await SearchGameIdAsync(gameTitle, ct).ConfigureAwait(false);
            if (!searchResult.IsSuccess || string.IsNullOrEmpty(searchResult.Value))
            {
                LogGameNotFound(_logger, gameTitle);
                return Result.Failure<GamePriceData>($"Game '{gameTitle}' not found in price database");
            }

            var gameId = searchResult.Value;

            // Step 2: Get current prices
            var pricesUrl = $"{BaseUrl}/games/prices/v2?key={_apiKey}&country=US&nondeals=true";
            var requestBody = JsonSerializer.Serialize(new[] { gameId });
            using var content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync(pricesUrl, content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                LogPriceFetchFailed(_logger, gameTitle, (int)response.StatusCode);
                return Result.Failure<GamePriceData>($"Failed to fetch price data: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var priceData = ParsePriceResponse(json, gameId, gameTitle);

            if (priceData == null)
            {
                LogNoPriceData(_logger, gameTitle);
                return Result.Failure<GamePriceData>($"No price data available for '{gameTitle}'");
            }

            // Cache the result
            _priceCache[cacheKey] = (priceData, DateTime.UtcNow);

            LogPriceFound(_logger, gameTitle, priceData.CurrentPrice, priceData.StoreName);
            return Result.Success(priceData);
        }
        catch (HttpRequestException ex)
        {
            LogNetworkError(_logger, gameTitle, ex);
            return Result.Failure<GamePriceData>($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            LogUnexpectedError(_logger, gameTitle, ex);
            return Result.Failure<GamePriceData>($"Unexpected error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PriceHistoryPoint>>> GetPriceHistoryAsync(
        string gameTitle,
        int days = 365,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return Result.Failure<IReadOnlyList<PriceHistoryPoint>>("IsThereAnyDeal API key not configured");
        }

        try
        {
            LogFetchingHistory(_logger, gameTitle, days);

            // First get the game ID
            var searchResult = await SearchGameIdAsync(gameTitle, ct).ConfigureAwait(false);
            if (!searchResult.IsSuccess || string.IsNullOrEmpty(searchResult.Value))
            {
                return Result.Failure<IReadOnlyList<PriceHistoryPoint>>($"Game '{gameTitle}' not found");
            }

            var gameId = searchResult.Value;
            var since = DateTimeOffset.UtcNow.AddDays(-days).ToUnixTimeSeconds();
            var historyUrl = $"{BaseUrl}/games/history/v2?key={_apiKey}&id={gameId}&country=US&since={since}";

            using var response = await _httpClient.GetAsync(historyUrl, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                LogHistoryFetchFailed(_logger, gameTitle, (int)response.StatusCode);
                return Result.Failure<IReadOnlyList<PriceHistoryPoint>>($"Failed to fetch history: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var history = ParseHistoryResponse(json);

            LogHistoryFetched(_logger, gameTitle, history.Count);
            return Result.Success<IReadOnlyList<PriceHistoryPoint>>(history);
        }
        catch (Exception ex)
        {
            LogUnexpectedError(_logger, gameTitle, ex);
            return Result.Failure<IReadOnlyList<PriceHistoryPoint>>($"Error: {ex.Message}");
        }
    }

    private async Task<Result<string>> SearchGameIdAsync(string gameTitle, CancellationToken ct)
    {
        var searchUrl = $"{BaseUrl}/games/search/v1?key={_apiKey}&title={Uri.EscapeDataString(gameTitle)}";

        using var response = await _httpClient.GetAsync(searchUrl, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<string>($"Search failed: {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);

        // The response is an array of game matches
        if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
        {
            var firstMatch = doc.RootElement[0];
            if (firstMatch.TryGetProperty("id", out var idProp))
            {
                return Result.Success(idProp.GetString() ?? "");
            }
        }

        return Result.Failure<string>("Game not found");
    }

    private static GamePriceData? ParsePriceResponse(string json, string gameId, string gameTitle)
    {
        using var doc = JsonDocument.Parse(json);

        // Response format: array of price objects per game
        if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
            return null;

        var gameData = doc.RootElement[0];
        if (!gameData.TryGetProperty("deals", out var deals) || deals.GetArrayLength() == 0)
            return null;

        // Find the best current deal
        var bestDeal = deals.EnumerateArray()
            .OrderBy(d => d.TryGetProperty("price", out var p) && p.TryGetProperty("amount", out var a)
                ? a.GetDecimal() : decimal.MaxValue)
            .FirstOrDefault();

        if (bestDeal.ValueKind == JsonValueKind.Undefined)
            return null;

        var price = bestDeal.TryGetProperty("price", out var priceProp)
            ? priceProp.TryGetProperty("amount", out var amountProp) ? amountProp.GetDecimal() : 0
            : 0;

        var regular = bestDeal.TryGetProperty("regular", out var regProp)
            ? regProp.TryGetProperty("amount", out var regAmountProp) ? regAmountProp.GetDecimal() : price
            : price;

        var storeName = bestDeal.TryGetProperty("shop", out var shopProp)
            ? shopProp.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "Unknown" : "Unknown"
            : "Unknown";

        var storeUrl = bestDeal.TryGetProperty("url", out var urlProp)
            ? urlProp.GetString() ?? ""
            : "";

        var discount = regular > 0 ? Math.Round((1 - (price / regular)) * 100, 0) : 0;

        // Try to get historical low from the response
        var historicalLow = gameData.TryGetProperty("historyLow", out var histLowProp)
            ? histLowProp.TryGetProperty("amount", out var histAmountProp) ? histAmountProp.GetDecimal() : price
            : price;

        var historyLowDate = gameData.TryGetProperty("historyLowAt", out var histDateProp)
            ? DateTime.TryParse(histDateProp.GetString(), out var dt) ? dt : (DateTime?)null
            : null;

        return new GamePriceData(
            gameId,
            gameTitle,
            price,
            regular,
            historicalLow,
            historyLowDate,
            storeName,
            storeUrl,
            discount,
            price < regular);
    }

    private static List<PriceHistoryPoint> ParseHistoryResponse(string json)
    {
        var history = new List<PriceHistoryPoint>();

        using var doc = JsonDocument.Parse(json);

        // Parse the history array
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return history;

        foreach (var point in doc.RootElement.EnumerateArray())
        {
            if (point.TryGetProperty("timestamp", out var tsProp) &&
                point.TryGetProperty("deal", out var dealProp))
            {
                var timestamp = DateTimeOffset.FromUnixTimeSeconds(tsProp.GetInt64()).DateTime;
                var price = dealProp.TryGetProperty("price", out var priceProp)
                    ? priceProp.TryGetProperty("amount", out var amountProp) ? amountProp.GetDecimal() : 0
                    : 0;
                var store = dealProp.TryGetProperty("shop", out var shopProp)
                    ? shopProp.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : ""
                    : "";

                history.Add(new PriceHistoryPoint(timestamp, price, store));
            }
        }

        return history.OrderBy(p => p.Date).ToList();
    }

    /// <summary>
    /// Formats a price as a currency string.
    /// </summary>
    public static string FormatPrice(decimal? price, string currency = "USD")
    {
        if (price == null) return "--";
        return currency switch
        {
            "USD" => $"${price:F2}",
            "EUR" => $"€{price:F2}",
            "GBP" => $"£{price:F2}",
            _ => $"{price:F2} {currency}"
        };
    }

    #region LoggerMessage Definitions

    [LoggerMessage(Level = LogLevel.Warning, Message = "IsThereAnyDeal API key not configured. Price tracking disabled.")]
    private static partial void LogNoApiKey(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Price cache hit for '{GameTitle}'")]
    private static partial void LogCacheHit(ILogger logger, string gameTitle);

    [LoggerMessage(Level = LogLevel.Information, Message = "Searching prices for '{GameTitle}'")]
    private static partial void LogSearching(ILogger logger, string gameTitle);

    [LoggerMessage(Level = LogLevel.Information, Message = "Game '{GameTitle}' not found in price database")]
    private static partial void LogGameNotFound(ILogger logger, string gameTitle);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Price fetch failed for '{GameTitle}' with status {StatusCode}")]
    private static partial void LogPriceFetchFailed(ILogger logger, string gameTitle, int statusCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "No price data available for '{GameTitle}'")]
    private static partial void LogNoPriceData(ILogger logger, string gameTitle);

    [LoggerMessage(Level = LogLevel.Information, Message = "Price found for '{GameTitle}': ${Price} at {StoreName}")]
    private static partial void LogPriceFound(ILogger logger, string gameTitle, decimal price, string storeName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetching {Days} days price history for '{GameTitle}'")]
    private static partial void LogFetchingHistory(ILogger logger, string gameTitle, int days);

    [LoggerMessage(Level = LogLevel.Warning, Message = "History fetch failed for '{GameTitle}' with status {StatusCode}")]
    private static partial void LogHistoryFetchFailed(ILogger logger, string gameTitle, int statusCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetched {Count} price history points for '{GameTitle}'")]
    private static partial void LogHistoryFetched(ILogger logger, string gameTitle, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Network error for '{GameTitle}'")]
    private static partial void LogNetworkError(ILogger logger, string gameTitle, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error for '{GameTitle}'")]
    private static partial void LogUnexpectedError(ILogger logger, string gameTitle, Exception ex);

    #endregion
}

/// <summary>
/// Interface for game price tracking service.
/// </summary>
public interface IGamePriceService
{
    /// <summary>
    /// Gets the current best price for a game across all tracked stores.
    /// </summary>
    Task<Result<GamePriceData>> GetCurrentPriceAsync(string gameTitle, CancellationToken ct = default);

    /// <summary>
    /// Gets the price history for a game over the specified number of days.
    /// </summary>
    Task<Result<IReadOnlyList<PriceHistoryPoint>>> GetPriceHistoryAsync(string gameTitle, int days = 365, CancellationToken ct = default);
}
