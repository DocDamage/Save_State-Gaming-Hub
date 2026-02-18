// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameDeals;

namespace SaveState.Application.GameDeals;

/// <summary>
/// Service for managing game deals and price tracking.
/// </summary>
public sealed class GameDealsService : IGameDealsService
{
    private readonly ILogger<GameDealsService> _logger;
    private readonly IEnumerable<IDealSourceClient> _dealClients;
    private readonly IGameDealsRepository _repository;
    private readonly ICacheService _cache;
    private readonly ITimeProvider _timeProvider;

    public GameDealsService(
        ILogger<GameDealsService> logger,
        IEnumerable<IDealSourceClient> dealClients,
        IGameDealsRepository repository,
        ICacheService cache,
        ITimeProvider timeProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dealClients = dealClients ?? throw new ArgumentNullException(nameof(dealClients));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<GameDeal>>> GetDealsAsync(DealFilterOptions? filter = null, CancellationToken ct = default)
    {
        try
        {
            // Check cache first
            var cacheKey = $"deals_{filter?.GetHashCode() ?? 0}";
            if (_cache.TryGetValue<IReadOnlyList<GameDeal>>(cacheKey, out var cached))
            {
                _logger.LogDebug("Returning cached deals");
                return Result.Success(cached!);
            }

            // Get deals from database
            var deals = await _repository.GetDealsAsync(filter, ct);

            // If no deals or cache expired, fetch from sources
            if (!deals.Any() || ShouldRefreshCache())
            {
                _logger.LogInformation("Fetching fresh deals from sources");
                var sourceDeals = await FetchDealsFromSourcesAsync(filter, ct);
                if (sourceDeals.Any())
                {
                    deals = sourceDeals;
                    await _repository.SaveDealsAsync(deals, ct);
                }
            }

            // Cache for 30 minutes
            _cache.Set(cacheKey, deals, TimeSpan.FromMinutes(30));

            return Result.Success(deals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get deals");
            return Result.Failure<IReadOnlyList<GameDeal>>("Failed to retrieve deals");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<GameDeal>>> GetDealsForGameAsync(string gameTitle, CancellationToken ct = default)
    {
        try
        {
            var cacheKey = $"deals_game_{gameTitle.ToLowerInvariant().Replace(" ", "_")}";
            if (_cache.TryGetValue<IReadOnlyList<GameDeal>>(cacheKey, out var cached))
            {
                return Result.Success(cached!);
            }

            // Check database first
            var deals = await _repository.GetDealsForGameAsync(gameTitle, ct);

            // If no deals, fetch from sources
            if (!deals.Any())
            {
                foreach (var client in _dealClients)
                {
                    var result = await client.GetDealsForGameAsync(gameTitle, ct);
                    if (result.IsSuccess && result.Value.Any())
                    {
                        var clientDeals = result.Value.ToList();
                        await _repository.SaveDealsAsync(clientDeals, ct);
                        deals = deals.Concat(clientDeals).ToList();
                    }
                }
            }

            // Sort by price
            deals = deals.OrderBy(d => d.CurrentPrice).ToList();

            // Cache for 1 hour
            _cache.Set(cacheKey, deals, TimeSpan.FromHours(1));

            return Result.Success(deals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get deals for game: {Title}", gameTitle);
            return Result.Failure<IReadOnlyList<GameDeal>>("Failed to retrieve game deals");
        }
    }

    /// <inheritdoc />
    public async Task<Result<GameDeal?>> GetBestDealAsync(string gameTitle, CancellationToken ct = default)
    {
        try
        {
            var dealsResult = await GetDealsForGameAsync(gameTitle, ct);
            if (!dealsResult.IsSuccess)
            {
                return Result.Failure<GameDeal?>(dealsResult.Error!);
            }

            var bestDeal = dealsResult.Value.OrderBy(d => d.CurrentPrice).FirstOrDefault();
            return Result.Success(bestDeal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get best deal for: {Title}", gameTitle);
            return Result.Failure<GameDeal?>("Failed to retrieve best deal");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PriceHistoryEntry>>> GetPriceHistoryAsync(string gameTitle, string storeId, CancellationToken ct = default)
    {
        try
        {
            var history = await _repository.GetPriceHistoryAsync(gameTitle, storeId, ct);
            return Result.Success(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get price history");
            return Result.Failure<IReadOnlyList<PriceHistoryEntry>>("Failed to retrieve price history");
        }
    }

    /// <inheritdoc />
    public async Task<Result<DealStatistics>> GetDealStatisticsAsync(string gameTitle, CancellationToken ct = default)
    {
        try
        {
            var history = new List<PriceHistoryEntry>();

            // Get history from all stores
            var storeIds = new[] { "steam", "gog", "epic" };
            foreach (var storeId in storeIds)
            {
                var storeHistory = await _repository.GetPriceHistoryAsync(gameTitle, storeId, ct);
                history.AddRange(storeHistory);
            }

            if (!history.Any())
            {
                return Result.Success(new DealStatistics
                {
                    GameTitle = gameTitle,
                    Trend = PriceTrend.Unknown
                });
            }

            var currentPrice = history.OrderByDescending(h => h.Date).First().Price;
            var historicalLow = history.Min(h => h.Price);
            var averagePrice = history.Average(h => h.Price);
            var saleEntries = history.Where(h => h.WasOnSale).ToList();

            // Calculate trend (compare last 30 days to previous 30 days)
            var recent30Days = history.Where(h => h.Date >= _timeProvider.UtcNow.AddDays(-30)).ToList();
            var previous30Days = history.Where(h =>
                h.Date >= _timeProvider.UtcNow.AddDays(-60) &&
                h.Date < _timeProvider.UtcNow.AddDays(-30)).ToList();

            var trend = PriceTrend.Stable;
            if (recent30Days.Any() && previous30Days.Any())
            {
                var recentAvg = recent30Days.Average(h => h.Price);
                var previousAvg = previous30Days.Average(h => h.Price);

                var change = (recentAvg - previousAvg) / previousAvg;
                trend = change switch
                {
                    > 0.1m => PriceTrend.Rising,
                    < -0.1m => PriceTrend.Falling,
                    _ => PriceTrend.Stable
                };
            }

            // Calculate days since last sale
            var lastSale = saleEntries.OrderByDescending(h => h.Date).FirstOrDefault();
            var daysSinceLastSale = lastSale != null
                ? (_timeProvider.UtcNow - lastSale.Date).Days
                : (int?)null;

            // Generate recommendation
            string? recommendation = null;
            if (currentPrice <= historicalLow * 1.05m)
            {
                recommendation = "🔥 Historical low! Great time to buy.";
            }
            else if (trend == PriceTrend.Falling)
            {
                recommendation = "📉 Prices are falling. Consider waiting for a better deal.";
            }
            else if (trend == PriceTrend.Rising)
            {
                recommendation = "📈 Prices are rising. Buy now if you're interested.";
            }

            var stats = new DealStatistics
            {
                GameTitle = gameTitle,
                CurrentLowestPrice = currentPrice,
                HistoricalLowestPrice = historicalLow,
                AveragePrice = averagePrice,
                SaleCount = saleEntries.Count,
                Trend = trend,
                DaysSinceLastSale = daysSinceLastSale,
                BestTimeToBuyRecommendation = recommendation
            };

            return Result.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get deal statistics");
            return Result.Failure<DealStatistics>("Failed to calculate statistics");
        }
    }

    /// <inheritdoc />
    public async Task<Result<PriceAlert>> CreatePriceAlertAsync(
        Guid userId,
        string gameTitle,
        decimal? targetPrice,
        decimal? targetDiscountPercent,
        CancellationToken ct = default)
    {
        try
        {
            var alert = new PriceAlert
            {
                UserId = userId,
                GameTitle = gameTitle,
                TargetPrice = targetPrice,
                TargetDiscountPercent = targetDiscountPercent,
                IsActive = true,
                CreatedAt = _timeProvider.UtcNow
            };

            await _repository.CreatePriceAlertAsync(alert, ct);

            _logger.LogInformation("Created price alert for user {UserId} on game {GameTitle}", userId, gameTitle);
            return Result.Success(alert);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create price alert");
            return Result.Failure<PriceAlert>("Failed to create alert");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PriceAlert>>> GetUserPriceAlertsAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var alerts = await _repository.GetPriceAlertsForUserAsync(userId, ct);
            return Result.Success(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user price alerts");
            return Result.Failure<IReadOnlyList<PriceAlert>>("Failed to retrieve alerts");
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeletePriceAlertAsync(Guid alertId, CancellationToken ct = default)
    {
        try
        {
            await _repository.DeletePriceAlertAsync(alertId, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete price alert");
            return Result.Failure("Failed to delete alert");
        }
    }

    /// <inheritdoc />
    public async Task<Result> RefreshDealsAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Refreshing all deals from sources");

            foreach (var client in _dealClients)
            {
                try
                {
                    var result = await client.GetDealsAsync(null, ct);
                    if (result.IsSuccess)
                    {
                        await _repository.SaveDealsAsync(result.Value, ct);
                        _logger.LogInformation("Saved {Count} deals from {Source}",
                            result.Value.Count, client.SourceName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching deals from {Source}", client.SourceName);
                }
            }

            // Clear cache
            _cache.Remove("deals");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh deals");
            return Result.Failure("Failed to refresh deals");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<string>>> SearchGamesAsync(string query, CancellationToken ct = default)
    {
        try
        {
            // Get unique game titles from our deal database
            var deals = await _repository.GetDealsAsync(null, ct);
            var games = deals
                .Where(d => d.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(d => d.Title)
                .Distinct()
                .Take(10)
                .ToList();

            return Result.Success<IReadOnlyList<string>>(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search games");
            return Result.Failure<IReadOnlyList<string>>("Failed to search games");
        }
    }

    private async Task<IReadOnlyList<GameDeal>> FetchDealsFromSourcesAsync(DealFilterOptions? filter, CancellationToken ct)
    {
        var allDeals = new List<GameDeal>();

        foreach (var client in _dealClients)
        {
            try
            {
                var result = await client.GetDealsAsync(filter, ct);
                if (result.IsSuccess)
                {
                    allDeals.AddRange(result.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching from source: {Source}", client.SourceName);
            }
        }

        // Remove duplicates (same game from different sources, keep cheapest)
        var uniqueDeals = allDeals
            .GroupBy(d => new { d.TitlePlain, d.Store.Id })
            .Select(g => g.OrderBy(d => d.CurrentPrice).First())
            .ToList();

        return uniqueDeals;
    }

    private bool ShouldRefreshCache()
    {
        // Cache is considered stale after 30 minutes
        // This would be implemented with cache metadata in a real system
        return false;
    }
}
