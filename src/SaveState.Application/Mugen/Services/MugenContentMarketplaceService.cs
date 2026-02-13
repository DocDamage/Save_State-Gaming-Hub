using SaveState.Application.Mugen.Models.ContentMarketplace;
using SaveState.Application.Mugen.Services.ContentMarketplace;
using SaveState.Application.Mugen.Services.ContentMarketplace.Engines;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Professional content marketplace for MUGEN mods, characters, stages, and content.
/// Provides distribution, monetization, licensing, and community-driven content ecosystem.
/// Acts as a coordinator delegating operations to specialized engines.
/// </summary>
public class MugenContentMarketplaceService : IMugenContentMarketplaceService
{
    private readonly ILogger<MugenContentMarketplaceService> _logger;
    private readonly ICacheService _cache;

    // Data stores
    private readonly Dictionary<string, MarketplaceItem> _marketplaceItems = new();
    private readonly Dictionary<string, UserPurchase> _userPurchases = new();
    private readonly Dictionary<string, ContentLicense> _activeLicenses = new();

    // Specialized engines
    private readonly ListingEngine _listingEngine;
    private readonly PurchaseEngine _purchaseEngine;
    private readonly ReviewEngine _reviewEngine;
    private readonly SearchEngine _searchEngine;
    private readonly AnalyticsEngine _analyticsEngine;
    private readonly RevenueManager _revenueManager;

    public MugenContentMarketplaceService(
        ILogger<MugenContentMarketplaceService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache)
    {
        _logger = logger;
        _cache = cache;

        // Initialize engines
        _listingEngine = new ListingEngine(
            loggerFactory.CreateLogger<ListingEngine>(), 
            _marketplaceItems);
        _revenueManager = new RevenueManager(
            loggerFactory.CreateLogger<RevenueManager>());
        _purchaseEngine = new PurchaseEngine(
            loggerFactory.CreateLogger<PurchaseEngine>(),
            _userPurchases,
            _activeLicenses,
            _revenueManager);
        _reviewEngine = new ReviewEngine(
            loggerFactory.CreateLogger<ReviewEngine>());
        _searchEngine = new SearchEngine(
            loggerFactory.CreateLogger<SearchEngine>());
        _analyticsEngine = new AnalyticsEngine(
            loggerFactory.CreateLogger<AnalyticsEngine>());

        InitializeSampleContent();
    }

    #region Listing Operations

    public Task<Result<IReadOnlyList<MarketplaceItem>>> GetFeaturedContentAsync(CancellationToken ct = default)
        => _listingEngine.GetFeaturedContentAsync(ct);

    public Task<Result<IReadOnlyList<MarketplaceItem>>> GetContentByCategoryAsync(ContentCategory category, CancellationToken ct = default)
        => _listingEngine.GetContentByCategoryAsync(category, ct);

    public Task<Result<MarketplaceItem>> GetContentDetailsAsync(string contentId, CancellationToken ct = default)
        => _listingEngine.GetContentDetailsAsync(contentId, ct);

    public Task<Result<string>> UploadContentAsync(ContentUploadRequest request, CancellationToken ct = default)
        => _listingEngine.UploadContentAsync(request, ct);

    #endregion

    #region Search Operations

    public Task<Result<IReadOnlyList<MarketplaceItem>>> SearchContentAsync(string query, CancellationToken ct = default)
        => _searchEngine.SearchContentAsync(query, _marketplaceItems.Values, ct);

    public Task<Result<SearchResult>> AdvancedSearchAsync(SearchQuery query, CancellationToken ct = default)
        => _searchEngine.AdvancedSearchAsync(query, _marketplaceItems.Values, ct);

    #endregion

    #region Purchase Operations

    public async Task<Result<ContentPurchase>> PurchaseContentAsync(string contentId, string buyerId, CancellationToken ct = default)
    {
        var item = _listingEngine.GetItem(contentId);
        if (item == null)
        {
            return Result.Failure<ContentPurchase>("Content not found");
        }

        return await _purchaseEngine.PurchaseContentAsync(item, buyerId, ct);
    }

    public Task<Result> DownloadContentAsync(string contentId, string userId, CancellationToken ct = default)
        => _purchaseEngine.DownloadContentAsync(contentId, userId, 
            () => _listingEngine.IncrementDownloadCount(contentId), ct);

    public Task<Result<IReadOnlyList<MarketplaceItem>>> GetUserLibraryAsync(string userId, CancellationToken ct = default)
        => _purchaseEngine.GetUserLibraryAsync(userId, _listingEngine.GetItem, ct);

    public Task<bool> VerifyContentAccessAsync(string contentId, string userId, CancellationToken ct = default)
        => Task.FromResult(_purchaseEngine.VerifyContentAccess(contentId, userId));

    #endregion

    #region Review Operations

    public Task<Result> RateContentAsync(string contentId, string userId, int rating, string? review, CancellationToken ct = default)
    {
        var isVerifiedPurchase = _purchaseEngine.HasPurchased(contentId, userId);
        return _reviewEngine.RateContentAsync(contentId, userId, rating, review, isVerifiedPurchase,
            (id, rate) => _listingEngine.UpdateRating(id, rate), ct);
    }

    public Task<Result<ReviewSummary>> GetContentReviewsAsync(string contentId, CancellationToken ct = default)
        => _reviewEngine.GetContentReviewsAsync(contentId, ct);

    public Task<Result> SubmitReviewAsync(ReviewRequest request, CancellationToken ct = default)
    {
        var isVerifiedPurchase = _purchaseEngine.HasPurchased(request.ContentId, request.UserId);
        return _reviewEngine.SubmitReviewAsync(request, isVerifiedPurchase, ct);
    }

    #endregion

    #region Analytics Operations

    public async Task<Result<CreatorDashboard>> GetCreatorDashboardAsync(string creatorId, CancellationToken ct = default)
    {
        var creatorItems = _marketplaceItems.Values.Where(item => item.CreatorId == creatorId);
        return await _analyticsEngine.GetCreatorDashboardAsync(creatorId, creatorItems,
            async () => {
                var result = await GetSalesMetricsAsync(creatorId, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, ct);
                return result.IsSuccess ? result.Value : new SalesMetrics();
            }, ct);
    }

    public Task<Result<MarketplaceStats>> GetMarketplaceStatsAsync(CancellationToken ct = default)
        => _analyticsEngine.GetMarketplaceStatsAsync(_marketplaceItems.Values, _userPurchases.Values, ct);

    public Task<Result<SalesMetrics>> GetSalesMetricsAsync(string creatorId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        var creatorItems = _marketplaceItems.Values.Where(item => item.CreatorId == creatorId);
        return _analyticsEngine.GetSalesMetricsAsync(creatorId, creatorItems, startDate, endDate, ct);
    }

    public Task<Result<IReadOnlyList<TrendingContent>>> GetTrendingContentAsync(int limit = 10, CancellationToken ct = default)
        => _analyticsEngine.GetTrendingContentAsync(_marketplaceItems.Values, limit, ct);

    #endregion

    private void InitializeSampleContent()
        => _listingEngine.InitializeSampleContent();
}
