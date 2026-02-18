// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Common;
using SaveState.Core.GameDeals;

namespace SaveState.Infrastructure.GameDeals.Clients;

/// <summary>
/// Client for IsThereAnyDeal API (https://isthereanydeal.com/).
/// </summary>
public sealed class IsThereAnyDealClient : IDealSourceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IsThereAnyDealClient> _logger;
    private readonly IsThereAnyDealOptions _options;

    public string SourceName => "IsThereAnyDeal";

    public IsThereAnyDealClient(
        HttpClient httpClient,
        ILogger<IsThereAnyDealClient> logger,
        IOptions<IsThereAnyDealOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new IsThereAnyDealOptions();
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<GameDeal>>> GetDealsAsync(DealFilterOptions? filter = null, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_options.ApiKey))
            {
                _logger.LogWarning("IsThereAnyDeal API key not configured");
                return Result.Success<IReadOnlyList<GameDeal>>(new List<GameDeal>());
            }

            // Build query parameters
            var queryParams = new List<string>
            {
                $"key={Uri.EscapeDataString(_options.ApiKey)}",
                "country=US",
                "currency=USD"
            };

            if (filter?.StoreIds?.Any() == true)
            {
                queryParams.Add($"shops={string.Join(",", filter.StoreIds)}");
            }

            var url = $"{GetBaseUrl()}/deals?{string.Join("&", queryParams)}";

            _logger.LogDebug("Fetching deals from ITAD: {Url}", url);

            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("ITAD API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return Result.Failure<IReadOnlyList<GameDeal>>($"API error: {response.StatusCode}");
            }

            var deals = await response.Content.ReadFromJsonAsync<List<ITADDeal>>(ct);

            if (deals == null)
            {
                return Result.Success<IReadOnlyList<GameDeal>>(new List<GameDeal>());
            }

            var mappedDeals = deals.Select(MapToGameDeal).ToList();

            // Apply client-side filtering
            if (filter != null)
            {
                mappedDeals = ApplyFilters(mappedDeals, filter).ToList();
            }

            _logger.LogInformation("Retrieved {Count} deals from ITAD", mappedDeals.Count);
            return Result.Success<IReadOnlyList<GameDeal>>(mappedDeals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching deals from ITAD");
            return Result.Failure<IReadOnlyList<GameDeal>>("Failed to fetch deals");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<GameDeal>>> GetDealsForGameAsync(string gameTitle, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_options.ApiKey))
            {
                return Result.Success<IReadOnlyList<GameDeal>>(new List<GameDeal>());
            }

            // First, search for the game to get its plain title
            var searchResult = await SearchGameAsync(gameTitle, ct);
            if (searchResult == null)
            {
                return Result.Success<IReadOnlyList<GameDeal>>(new List<GameDeal>());
            }

            var url = $"{GetBaseUrl()}/deals?key={Uri.EscapeDataString(_options.ApiKey)}" +
                      $"&plains={Uri.EscapeDataString(searchResult)}" +
                      "&country=US&currency=USD";

            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                return Result.Success<IReadOnlyList<GameDeal>>(new List<GameDeal>());
            }

            var deals = await response.Content.ReadFromJsonAsync<List<ITADDeal>>(ct);
            var mappedDeals = deals?.Select(MapToGameDeal).ToList() ?? new List<GameDeal>();

            return Result.Success<IReadOnlyList<GameDeal>>(mappedDeals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching deals for game: {Title}", gameTitle);
            return Result.Failure<IReadOnlyList<GameDeal>>("Failed to fetch game deals");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PriceHistoryEntry>>> GetPriceHistoryAsync(string gameTitle, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_options.ApiKey))
            {
                return Result.Success<IReadOnlyList<PriceHistoryEntry>>(new List<PriceHistoryEntry>());
            }

            var searchResult = await SearchGameAsync(gameTitle, ct);
            if (searchResult == null)
            {
                return Result.Success<IReadOnlyList<PriceHistoryEntry>>(new List<PriceHistoryEntry>());
            }

            var url = $"{GetBaseUrl()}/history?key={Uri.EscapeDataString(_options.ApiKey)}" +
                      $"&plains={Uri.EscapeDataString(searchResult)}" +
                      "&country=US&currency=USD";

            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                return Result.Success<IReadOnlyList<PriceHistoryEntry>>(new List<PriceHistoryEntry>());
            }

            // Parse and map price history
            return Result.Success<IReadOnlyList<PriceHistoryEntry>>(new List<PriceHistoryEntry>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching price history for: {Title}", gameTitle);
            return Result.Failure<IReadOnlyList<PriceHistoryEntry>>("Failed to fetch price history");
        }
    }

    /// <summary>
    /// Searches for a game and returns its plain title for ITAD API.
    /// </summary>
    private async Task<string?> SearchGameAsync(string gameTitle, CancellationToken ct)
    {
        try
        {
            var url = $"{GetBaseUrl()}/search?key={Uri.EscapeDataString(_options.ApiKey)}" +
                      $"&q={Uri.EscapeDataString(gameTitle)}" +
                      "&limit=1";

            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<ITADSearchResult>(ct);
            return result?.Data?.FirstOrDefault()?.Plain;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for game: {Title}", gameTitle);
            return null;
        }
    }

    private GameDeal MapToGameDeal(ITADDeal deal)
    {
        var store = MapStore(deal.Shop?.Id ?? "unknown");

        return new GameDeal
        {
            Id = $"{deal.Plain}_{deal.Shop?.Id}_{deal.Price?.Cut}",
            Title = deal.Title ?? "Unknown",
            TitlePlain = deal.Plain,
            CurrentPrice = (decimal)(deal.Price?.New ?? 0) / 100, // Convert cents to dollars
            RegularPrice = (decimal?)(deal.Price?.Old ?? 0) / 100,
            Store = store,
            DealStart = deal.Added != null ? DateTimeOffset.FromUnixTimeSeconds(deal.Added.Value).DateTime : null,
            IsHistoricalLow = deal.Price?.IsLow ?? false,
            StoreUrl = deal.Url,
            Drm = deal.Drm?.FirstOrDefault(),
            ImageUrl = deal.Image
        };
    }

    private GameStore MapStore(string storeId)
    {
        return storeId.ToLowerInvariant() switch
        {
            "steam" => GameStore.Steam,
            "gog" => GameStore.GOG,
            "epic" => GameStore.Epic,
            "humblestore" => GameStore.Humble,
            "fanatical" => GameStore.Fanatical,
            "greenmangaming" => GameStore.GreenManGaming,
            "amazonus" => GameStore.Amazon,
            "gamebillet" => GameStore.GameBillet,
            "voidu" => GameStore.Voidu,
            "gamersgate" => GameStore.GamersGate,
            _ => new GameStore { Id = storeId, Name = storeId }
        };
    }

    private IEnumerable<GameDeal> ApplyFilters(List<GameDeal> deals, DealFilterOptions filter)
    {
        var query = deals.AsEnumerable();

        if (filter.MinDiscountPercent.HasValue)
        {
            query = query.Where(d => d.DiscountPercent >= filter.MinDiscountPercent.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(d => d.CurrentPrice <= filter.MaxPrice.Value);
        }

        if (filter.OnlyHistoricalLows == true)
        {
            query = query.Where(d => d.IsHistoricalLow);
        }

        if (filter.MinMetacriticScore.HasValue)
        {
            query = query.Where(d => d.MetacriticScore >= filter.MinMetacriticScore.Value);
        }

        query = filter.SortOrder switch
        {
            DealSortOrder.DiscountPercent => query.OrderByDescending(d => d.DiscountPercent),
            DealSortOrder.Price => query.OrderBy(d => d.CurrentPrice),
            DealSortOrder.Title => query.OrderBy(d => d.Title),
            DealSortOrder.DealEnd => query.OrderBy(d => d.DealEnd ?? DateTime.MaxValue),
            DealSortOrder.MetacriticScore => query.OrderByDescending(d => d.MetacriticScore),
            DealSortOrder.Newest => query.OrderByDescending(d => d.DealStart ?? DateTime.MinValue),
            _ => query
        };

        return query;
    }

    private string GetBaseUrl() => _options.BaseUrl.TrimEnd('/');
}

/// <summary>
/// ITAD API response models.
/// </summary>
public class ITADDeal
{
    public string? Plain { get; set; }
    public string? Title { get; set; }
    public string? Image { get; set; }
    public ITADShop? Shop { get; set; }
    public ITADPrice? Price { get; set; }
    public List<string>? Drm { get; set; }
    public string? Url { get; set; }
    public long? Added { get; set; }
}

public class ITADShop
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

public class ITADPrice
{
    public int? New { get; set; }
    public int? Old { get; set; }
    public int? Cut { get; set; }
    public bool? IsLow { get; set; }
}

public class ITADSearchResult
{
    public List<ITADSearchData>? Data { get; set; }
}

public class ITADSearchData
{
    public string? Plain { get; set; }
    public string? Title { get; set; }
}

/// <summary>
/// Configuration options for IsThereAnyDeal API.
/// </summary>
public class IsThereAnyDealOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.isthereanydeal.com/v01";
    public int CacheMinutes { get; set; } = 30;
}
