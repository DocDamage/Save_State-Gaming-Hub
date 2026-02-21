// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using SaveState.Core.Common.Services;

namespace SaveState.Core.GameDeals;

/// <summary>
/// Represents a game deal from a store.
/// </summary>
public class GameDeal
{
    /// <summary>
    /// Unique identifier for the deal.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The game title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Plain title for matching.
    /// </summary>
    public string? TitlePlain { get; set; }

    /// <summary>
    /// Game image URL.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Current price.
    /// </summary>
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// Original/regular price.
    /// </summary>
    public decimal? RegularPrice { get; set; }

    /// <summary>
    /// Discount percentage.
    /// </summary>
    public decimal? DiscountPercent => RegularPrice.HasValue && RegularPrice.Value > 0
        ? Math.Round((1 - CurrentPrice / RegularPrice.Value) * 100, 0)
        : null;

    /// <summary>
    /// The store offering the deal.
    /// </summary>
    public GameStore? Store { get; set; }

    /// <summary>
    /// Deal start date.
    /// </summary>
    public DateTime? DealStart { get; set; }

    /// <summary>
    /// Deal end date (if known).
    /// </summary>
    public DateTime? DealEnd { get; set; }

    /// <summary>
    /// Whether the deal is active.
    /// </summary>
    public bool IsActive => DealEnd == null || DealEnd.Value > SystemTimeProvider.Instance.UtcNow;

    /// <summary>
    /// Whether this is a historical low price.
    /// </summary>
    public bool IsHistoricalLow { get; set; }

    /// <summary>
    /// Link to the store page.
    /// </summary>
    public string? StoreUrl { get; set; }

    /// <summary>
    /// DRM type (Steam, GOG, Epic, etc.).
    /// </summary>
    public string? Drm { get; set; }

    /// <summary>
    /// When the deal was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = SystemTimeProvider.Instance.UtcNow;

    /// <summary>
    /// Metacritic score if available.
    /// </summary>
    public int? MetacriticScore { get; set; }

    /// <summary>
    /// Steam rating if available.
    /// </summary>
    public string? SteamRating { get; set; }

    /// <summary>
    /// Formatted current price.
    /// </summary>
    public string FormattedPrice => $"${CurrentPrice:F2}";

    /// <summary>
    /// Formatted regular price.
    /// </summary>
    public string? FormattedRegularPrice => RegularPrice.HasValue ? $"${RegularPrice.Value:F2}" : null;

    /// <summary>
    /// Formatted discount string.
    /// </summary>
    public string? FormattedDiscount => DiscountPercent.HasValue ? $"-{DiscountPercent.Value:F0}%" : null;

    /// <summary>
    /// How much money is saved.
    /// </summary>
    public decimal? Savings => RegularPrice.HasValue ? RegularPrice.Value - CurrentPrice : null;
}

/// <summary>
/// Game store information.
/// </summary>
public class GameStore
{
    /// <summary>
    /// Store ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Store name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Store color for UI.
    /// </summary>
    public string Color { get; set; } = "#808080";

    /// <summary>
    /// Whether this is an official store.
    /// </summary>
    public bool IsOfficial { get; set; } = true;

    /// <summary>
    /// Common stores.
    /// </summary>
    public static readonly GameStore Steam = new() { Id = "steam", Name = "Steam", Color = "#1b2838" };
    public static readonly GameStore GOG = new() { Id = "gog", Name = "GOG", Color = "#86328a" };
    public static readonly GameStore Epic = new() { Id = "epic", Name = "Epic Games", Color = "#ffffff" };
    public static readonly GameStore Humble = new() { Id = "humble", Name = "Humble Store", Color = "#cb272c" };
    public static readonly GameStore Fanatical = new() { Id = "fanatical", Name = "Fanatical", Color = "#f94725" };
    public static readonly GameStore GreenManGaming = new() { Id = "gmg", Name = "Green Man Gaming", Color = "#00a8ff" };
    public static readonly GameStore Amazon = new() { Id = "amazon", Name = "Amazon", Color = "#ff9900" };
    public static readonly GameStore GameBillet = new() { Id = "gamebillet", Name = "GameBillet", Color = "#00b0f0" };
    public static readonly GameStore Voidu = new() { Id = "voidu", Name = "Voidu", Color = "#ff6b00" };
    public static readonly GameStore GamersGate = new() { Id = "gamersgate", Name = "GamersGate", Color = "#0078d7" };
}

/// <summary>
/// Price history entry for a game.
/// </summary>
public class PriceHistoryEntry
{
    /// <summary>
    /// Entry ID.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Game title.
    /// </summary>
    public string GameTitle { get; set; } = string.Empty;

    /// <summary>
    /// Store ID.
    /// </summary>
    public string StoreId { get; set; } = string.Empty;

    /// <summary>
    /// Price at this point in time.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Date of this price point.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Whether this was a sale price.
    /// </summary>
    public bool WasOnSale { get; set; }
}

/// <summary>
/// User's price alert for a game.
/// </summary>
public class PriceAlert
{
    /// <summary>
    /// Alert ID.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Game title to watch.
    /// </summary>
    public string GameTitle { get; set; } = string.Empty;

    /// <summary>
    /// Target price (alert when price drops to or below this).
    /// </summary>
    public decimal? TargetPrice { get; set; }

    /// <summary>
    /// Target discount percentage.
    /// </summary>
    public decimal? TargetDiscountPercent { get; set; }

    /// <summary>
    /// Specific stores to watch (empty = all stores).
    /// </summary>
    public List<string> StoreIds { get; set; } = new();

    /// <summary>
    /// Whether to alert on historical low.
    /// </summary>
    public bool AlertOnHistoricalLow { get; set; } = true;

    /// <summary>
    /// Whether the alert is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the alert was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = SystemTimeProvider.Instance.UtcNow;

    /// <summary>
    /// When the alert was last triggered.
    /// </summary>
    public DateTime? LastTriggeredAt { get; set; }

    /// <summary>
    /// Minimum hours between alerts (to avoid spam).
    /// </summary>
    public int MinHoursBetweenAlerts { get; set; } = 24;

    /// <summary>
    /// Checks if enough time has passed since last alert.
    /// </summary>
    public bool CanTrigger => !LastTriggeredAt.HasValue ||
        SystemTimeProvider.Instance.UtcNow >= LastTriggeredAt.Value.AddHours(MinHoursBetweenAlerts);
}

/// <summary>
/// Filter options for deal search.
/// </summary>
public class DealFilterOptions
{
    /// <summary>
    /// Search query.
    /// </summary>
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Store IDs to include.
    /// </summary>
    public List<string>? StoreIds { get; set; }

    /// <summary>
    /// Minimum discount percentage.
    /// </summary>
    public decimal? MinDiscountPercent { get; set; }

    /// <summary>
    /// Maximum price.
    /// </summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// DRM types to include.
    /// </summary>
    public List<string>? DrmTypes { get; set; }

    /// <summary>
    /// Whether to show only historical lows.
    /// </summary>
    public bool? OnlyHistoricalLows { get; set; }

    /// <summary>
    /// Minimum Metacritic score.
    /// </summary>
    public int? MinMetacriticScore { get; set; }

    /// <summary>
    /// Sort order.
    /// </summary>
    public DealSortOrder SortOrder { get; set; } = DealSortOrder.DiscountPercent;
}

/// <summary>
/// Sort options for deals.
/// </summary>
public enum DealSortOrder
{
    DiscountPercent,
    Price,
    Title,
    DealEnd,
    MetacriticScore,
    Newest
}

/// <summary>
/// Deal statistics for a game.
/// </summary>
public class DealStatistics
{
    /// <summary>
    /// Game title.
    /// </summary>
    public string GameTitle { get; set; } = string.Empty;

    /// <summary>
    /// Current lowest price.
    /// </summary>
    public decimal CurrentLowestPrice { get; set; }

    /// <summary>
    /// Historical lowest price.
    /// </summary>
    public decimal HistoricalLowestPrice { get; set; }

    /// <summary>
    /// Average price over time.
    /// </summary>
    public decimal AveragePrice { get; set; }

    /// <summary>
    /// Number of times the game has been on sale.
    /// </summary>
    public int SaleCount { get; set; }

    /// <summary>
    /// Average sale frequency (days between sales).
    /// </summary>
    public double? AverageSaleFrequencyDays { get; set; }

    /// <summary>
    /// Best time to buy recommendation.
    /// </summary>
    public string? BestTimeToBuyRecommendation { get; set; }

    /// <summary>
    /// Price trend (rising, falling, stable).
    /// </summary>
    public PriceTrend Trend { get; set; }

    /// <summary>
    /// Days since last sale.
    /// </summary>
    public int? DaysSinceLastSale { get; set; }
}

/// <summary>
/// Price trend direction.
/// </summary>
public enum PriceTrend
{
    Rising,
    Falling,
    Stable,
    Unknown
}
