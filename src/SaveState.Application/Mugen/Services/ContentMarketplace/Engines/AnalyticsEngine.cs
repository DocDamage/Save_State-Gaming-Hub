namespace SaveState.Application.Mugen.Services.ContentMarketplace.Engines;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.ContentMarketplace;
using SaveState.Application.Mugen.Models.BalanceTuning;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for marketplace analytics and statistics.
/// </summary>
public class AnalyticsEngine
{
    private readonly ILogger<AnalyticsEngine> _logger;
    private readonly ConcurrentDictionary<string, ContentListing> _listings;
    private readonly ConcurrentDictionary<string, PurchaseRecord> _purchases;
    private readonly ConcurrentDictionary<string, CreatorStats> _creatorStats;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyticsEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public AnalyticsEngine(ILogger<AnalyticsEngine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _listings = new ConcurrentDictionary<string, ContentListing>();
        _purchases = new ConcurrentDictionary<string, PurchaseRecord>();
        _creatorStats = new ConcurrentDictionary<string, CreatorStats>();
        _timeProvider = new SystemTimeProvider();
    }

    /// <summary>
    /// Gets the creator dashboard data.
    /// </summary>
    /// <param name="creatorId">The creator ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The creator dashboard.</returns>
    public async Task<CreatorDashboard> GetCreatorDashboardAsync(string creatorId, CancellationToken cancellationToken = default)
    {
        var creatorItems = _listings.Values.Where(l => l.CreatorId == creatorId).ToList();
        var totalRevenue = CalculateCreatorRevenue(creatorId);
        var totalDownloads = creatorItems.Sum(i => i.DownloadCount);
        var averageRating = creatorItems.Any() ? creatorItems.Average(i => i.Rating) : 0;

        var stats = _creatorStats.GetOrAdd(creatorId, _ => new CreatorStats { CreatorId = creatorId });

        var dashboard = new CreatorDashboard
        {
            CreatorId = creatorId,
            TotalItems = creatorItems.Count,
            TotalRevenue = totalRevenue,
            TotalDownloads = totalDownloads,
            AverageRating = averageRating,
            Items = creatorItems.Select(MapToMarketplaceItem).ToList(),
            RecentActivity = GetRecentActivity(creatorId, 10),
            RecentMetrics = await GetRecentMetricsAsync(creatorId, cancellationToken).ConfigureAwait(false)
        };

        return dashboard;
    }

    /// <summary>
    /// Gets overall marketplace statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Marketplace statistics.</returns>
    public Task<MarketplaceStats> GetMarketplaceStatsAsync(CancellationToken cancellationToken = default)
    {
        var allItems = _listings.Values.Where(l => l.Status == ContentStatus.Approved).ToList();
        var allPurchases = _purchases.Values.ToList();

        var categoryBreakdown = allItems
            .GroupBy(i => i.Category)
            .ToDictionary(g => g.Key, g => g.Count()) as IReadOnlyDictionary<ContentCategory, int>;

        var topCreators = _creatorStats.Values
            .OrderByDescending(c => c.TotalRevenue)
            .Take(10)
            .Select(c => new CreatorRanking
            {
                CreatorId = c.CreatorId,
                CreatorName = c.CreatorName ?? $"Creator_{c.CreatorId[..Math.Min(8, c.CreatorId.Length)]}",
                TotalItems = c.TotalItems,
                TotalDownloads = c.TotalDownloads,
                TotalRevenue = c.TotalRevenue,
                AverageRating = c.AverageRating
            })
            .ToList();

        var stats = new MarketplaceStats
        {
            TotalItems = allItems.Count,
            TotalDownloads = allItems.Sum(i => i.DownloadCount),
            TotalRevenue = allPurchases.Sum(p => p.Amount),
            ActiveUsers = allPurchases.Select(p => p.BuyerId).Distinct().Count(),
            CategoryBreakdown = categoryBreakdown,
            TopCreators = topCreators,
            RecentUploads = allItems.OrderByDescending(i => i.UploadDate).Take(10).Select(MapToMarketplaceItem).ToList()
        };

        return Task.FromResult(stats);
    }

    /// <summary>
    /// Gets sales metrics for a creator within a date range.
    /// </summary>
    /// <param name="creatorId">The creator ID.</param>
    /// <param name="dateRange">The date range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sales metrics.</returns>
    public Task<SalesMetrics> GetSalesMetricsAsync(string creatorId, DateRange dateRange, CancellationToken cancellationToken = default)
    {
        var creatorPurchases = _purchases.Values
            .Where(p => p.CreatorId == creatorId && 
                        p.PurchaseDate >= dateRange.Start && 
                        p.PurchaseDate <= dateRange.End)
            .ToList();

        var totalSales = creatorPurchases.Count;
        var totalRevenue = creatorPurchases.Sum(p => p.Amount);
        var platformRevenue = totalRevenue * 0.3m; // 30% platform fee
        var creatorRevenue = totalRevenue - platformRevenue;

        var uniqueBuyers = creatorPurchases.Select(p => p.BuyerId).Distinct().ToList();
        var repeatBuyers = uniqueBuyers.Count(u => creatorPurchases.Count(p => p.BuyerId == u) > 1);

        // Category breakdown
        var creatorItems = _listings.Values.Where(l => l.CreatorId == creatorId).ToList();
        var categoryBreakdown = creatorItems
            .GroupBy(i => i.Category)
            .ToDictionary(
                g => g.Key,
                g => new CategoryMetrics
                {
                    Category = g.Key,
                    SalesCount = creatorPurchases.Count(p => g.Select(i => i.ContentId).Contains(p.ContentId)),
                    Revenue = creatorPurchases.Where(p => g.Select(i => i.ContentId).Contains(p.ContentId)).Sum(p => p.Amount),
                    ItemCount = g.Count(),
                    AverageRating = g.Any() ? g.Average(i => i.Rating) : 0
                }) as IReadOnlyDictionary<ContentCategory, CategoryMetrics>;

        var metrics = new SalesMetrics
        {
            PeriodStart = dateRange.Start,
            PeriodEnd = dateRange.End,
            TotalSales = totalSales,
            TotalRevenue = totalRevenue,
            PlatformRevenue = platformRevenue,
            CreatorRevenue = creatorRevenue,
            AverageOrderValue = totalSales > 0 ? totalRevenue / totalSales : 0,
            UniqueBuyers = uniqueBuyers.Count,
            RepeatBuyers = repeatBuyers,
            CategoryBreakdown = categoryBreakdown
        };

        return Task.FromResult(metrics);
    }

    /// <summary>
    /// Gets trending content based on recent activity.
    /// </summary>
    /// <param name="count">Number of items to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of trending content listings.</returns>
    public Task<IReadOnlyList<ContentListing>> GetTrendingContentAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var cutoffDate = _timeProvider.UtcNow.AddDays(-7);
        
        // Calculate trending score based on recent downloads and ratings
        var trending = _listings.Values
            .Where(l => l.Status == ContentStatus.Approved)
            .Select(l => new
            {
                Listing = l,
                Score = CalculateTrendingScore(l, cutoffDate)
            })
            .OrderByDescending(x => x.Score)
            .Take(count)
            .Select(x => x.Listing)
            .ToList();

        return Task.FromResult<IReadOnlyList<ContentListing>>(trending);
    }

    /// <summary>
    /// Records a purchase for analytics.
    /// </summary>
    /// <param name="purchaseId">The purchase ID.</param>
    /// <param name="contentId">The content ID.</param>
    /// <param name="buyerId">The buyer ID.</param>
    /// <param name="creatorId">The creator ID.</param>
    /// <param name="amount">The purchase amount.</param>
    internal void RecordPurchase(string purchaseId, string contentId, string buyerId, string creatorId, decimal amount)
    {
        var purchase = new PurchaseRecord
        {
            PurchaseId = purchaseId,
            ContentId = contentId,
            BuyerId = buyerId,
            CreatorId = creatorId,
            Amount = amount,
            PurchaseDate = _timeProvider.UtcNow
        };

        _purchases[purchaseId] = purchase;

        // Update creator stats
        var stats = _creatorStats.GetOrAdd(creatorId, _ => new CreatorStats { CreatorId = creatorId });
        stats.TotalRevenue += amount;
    }

    /// <summary>
    /// Indexes a content listing.
    /// </summary>
    /// <param name="listing">The content listing.</param>
    internal void IndexListing(ContentListing listing)
    {
        _listings[listing.ContentId] = listing;
        
        // Update creator stats
        var stats = _creatorStats.GetOrAdd(listing.CreatorId, _ => new CreatorStats 
        { 
            CreatorId = listing.CreatorId,
            CreatorName = listing.CreatorName
        });
        stats.TotalItems = _listings.Values.Count(l => l.CreatorId == listing.CreatorId);
    }

    /// <summary>
    /// Records a download for analytics.
    /// </summary>
    /// <param name="contentId">The content ID.</param>
    internal void RecordDownload(string contentId)
    {
        if (_listings.TryGetValue(contentId, out var listing))
        {
            listing.DownloadCount++;
            
            var stats = _creatorStats.GetOrAdd(listing.CreatorId, _ => new CreatorStats 
            { 
                CreatorId = listing.CreatorId,
                CreatorName = listing.CreatorName
            });
            stats.TotalDownloads++;
        }
    }

    private decimal CalculateCreatorRevenue(string creatorId)
    {
        return _purchases.Values
            .Where(p => p.CreatorId == creatorId)
            .Sum(p => p.Amount * 0.7m); // 70% to creator
    }

    private async Task<SalesMetrics> GetRecentMetricsAsync(string creatorId, CancellationToken cancellationToken)
    {
        var now = _timeProvider.UtcNow;
        return await GetSalesMetricsAsync(creatorId, new DateRange
        { 
            Start = now.AddDays(-30), 
            End = now 
        }, cancellationToken).ConfigureAwait(false);
    }

    private IReadOnlyList<CreatorActivity> GetRecentActivity(string creatorId, int limit)
    {
        var activities = new List<CreatorActivity>();
        
        // Get recent purchases
        var recentPurchases = _purchases.Values
            .Where(p => p.CreatorId == creatorId)
            .OrderByDescending(p => p.PurchaseDate)
            .Take(limit)
            .Select(p => new CreatorActivity
            {
                Timestamp = p.PurchaseDate,
                ActivityType = "Sale",
                Description = $"Content purchased for ${p.Amount:F2}"
            });

        activities.AddRange(recentPurchases);

        return activities.OrderByDescending(a => a.Timestamp).Take(limit).ToList();
    }

    private float CalculateTrendingScore(ContentListing listing, DateTime cutoffDate)
    {
        var recentFactor = listing.UploadDate > cutoffDate ? 2.0f : 1.0f;
        var ratingScore = listing.Rating * listing.RatingCount;
        var downloadScore = listing.DownloadCount / 100.0f;
        var featuredBonus = listing.IsFeatured ? 1.5f : 1.0f;

        return (ratingScore + downloadScore) * recentFactor * featuredBonus;
    }

    private static MarketplaceItem MapToMarketplaceItem(ContentListing listing)
    {
        return new MarketplaceItem
        {
            ContentId = listing.ContentId,
            Name = listing.Name,
            Description = listing.Description,
            Category = listing.Category,
            CreatorId = listing.CreatorId,
            CreatorName = listing.CreatorName,
            Price = listing.Price,
            LicenseType = listing.LicenseType,
            Tags = listing.Tags,
            Images = listing.Images,
            ContentFiles = Array.Empty<string>(),
            Status = listing.Status,
            UploadDate = listing.UploadDate,
            LastUpdated = listing.LastUpdated,
            Rating = listing.Rating,
            RatingCount = listing.RatingCount,
            DownloadCount = listing.DownloadCount,
            IsFeatured = listing.IsFeatured,
            CompatibleVersions = listing.CompatibleVersions
        };
    }

    private class PurchaseRecord
    {
        public string PurchaseId { get; set; } = string.Empty;
        public string ContentId { get; set; } = string.Empty;
        public string BuyerId { get; set; } = string.Empty;
        public string CreatorId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime PurchaseDate { get; set; }
    }

    private class CreatorStats
    {
        public string CreatorId { get; set; } = string.Empty;
        public string CreatorName { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public int TotalDownloads { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageRating { get; set; }
    }
}
