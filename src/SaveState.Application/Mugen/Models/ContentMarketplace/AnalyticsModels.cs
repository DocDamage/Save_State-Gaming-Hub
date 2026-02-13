namespace SaveState.Application.Mugen.Models.ContentMarketplace;

/// <summary>
/// Sales metrics for analytics.
/// </summary>
public class SalesMetrics
{
    public DateTime PeriodStart { get; set; } = default!;
    public DateTime PeriodEnd { get; set; } = default!;
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal PlatformRevenue { get; set; }
    public decimal CreatorRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int UniqueBuyers { get; set; }
    public int RepeatBuyers { get; set; }
    public IReadOnlyDictionary<ContentCategory, CategoryMetrics> CategoryBreakdown { get; set; } = default!;
}

/// <summary>
/// Category-specific metrics.
/// </summary>
public class CategoryMetrics
{
    public ContentCategory Category { get; set; } = default!;
    public int SalesCount { get; set; }
    public decimal Revenue { get; set; }
    public int ItemCount { get; set; }
    public float AverageRating { get; set; }
}

/// <summary>
/// Marketplace trend direction for content popularity.
/// </summary>
public enum MarketplaceTrendDirection
{
    Rising = 0,
    Stable = 1,
    Declining = 2
}

/// <summary>
/// Trending content item.
/// </summary>
public class TrendingContent
{
    public string ContentId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public ContentCategory Category { get; set; } = default!;
    public string CreatorId { get; set; } = default!;
    public string CreatorName { get; set; } = default!;
    public int RecentDownloads { get; set; }
    public int RecentSales { get; set; }
    public decimal RecentRevenue { get; set; }
    public float TrendingScore { get; set; }
    public MarketplaceTrendDirection Direction { get; set; }
}

/// <summary>
/// Creator dashboard data.
/// </summary>
public class CreatorDashboard
{
    public string CreatorId { get; set; } = default!;
    public int TotalItems { get; set; } = default!;
    public decimal TotalRevenue { get; set; } = default!;
    public int TotalDownloads { get; set; } = default!;
    public double AverageRating { get; set; } = default!;
    public IReadOnlyList<MarketplaceItem> Items { get; set; } = default!;
    public IReadOnlyList<CreatorActivity> RecentActivity { get; set; } = default!;
    public SalesMetrics RecentMetrics { get; set; } = default!;
}

/// <summary>
/// Creator activity record.
/// </summary>
public class CreatorActivity
{
    public DateTime Timestamp { get; set; } = default!;
    public string ActivityType { get; set; } = default!;
    public string Description { get; set; } = default!;
}

/// <summary>
/// Marketplace statistics.
/// </summary>
public class MarketplaceStats
{
    public int TotalItems { get; set; } = default!;
    public int TotalDownloads { get; set; } = default!;
    public decimal TotalRevenue { get; set; } = default!;
    public int ActiveUsers { get; set; } = default!;
    public IReadOnlyDictionary<ContentCategory, int> CategoryBreakdown { get; set; } = default!;
    public IReadOnlyList<CreatorRanking> TopCreators { get; set; } = default!;
    public IReadOnlyList<MarketplaceItem> RecentUploads { get; set; } = default!;
}

/// <summary>
/// Creator ranking data.
/// </summary>
public class CreatorRanking
{
    public string CreatorId { get; set; } = default!;
    public string CreatorName { get; set; } = default!;
    public int TotalItems { get; set; } = default!;
    public int TotalDownloads { get; set; } = default!;
    public decimal TotalRevenue { get; set; } = default!;
    public double AverageRating { get; set; } = default!;
}

/// <summary>
/// Creator balance information.
/// </summary>
public class CreatorBalance
{
    public string CreatorId { get; set; } = default!;
    public decimal AvailableBalance { get; set; } = default!;
    public decimal PendingBalance { get; set; } = default!;
    public decimal TotalEarned { get; set; } = default!;
    public DateTime? LastPayout { get; set; } = default!;
}
