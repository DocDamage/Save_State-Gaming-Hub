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
    private readonly ITimeProvider _timeProvider;

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

    public MugenContentMarketplaceService(
        ILogger<MugenContentMarketplaceService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _cache = cache;
        _timeProvider = timeProvider;

        // Initialize engines with new signatures
        _listingEngine = new ListingEngine(
            loggerFactory.CreateLogger<ListingEngine>(), 
            cache,
            timeProvider);
        _purchaseEngine = new PurchaseEngine(
            loggerFactory.CreateLogger<PurchaseEngine>(),
            null, // paymentService - can be injected later
            null, // contentAccessService - can be injected later
            null, // licenseManager - can be injected later
            timeProvider);
        _reviewEngine = new ReviewEngine(
            loggerFactory.CreateLogger<ReviewEngine>());
        _searchEngine = new SearchEngine(
            loggerFactory.CreateLogger<SearchEngine>());
        _analyticsEngine = new AnalyticsEngine(
            loggerFactory.CreateLogger<AnalyticsEngine>());

        InitializeSampleContent();
    }

    #region Listing Operations

    public async Task<Result<IReadOnlyList<MarketplaceItem>>> GetFeaturedContentAsync(CancellationToken ct = default)
    {
        var listings = await _listingEngine.GetFeaturedContentAsync(10, ct);
        var items = listings.Select(MapToMarketplaceItem).ToList();
        return Result.Success<IReadOnlyList<MarketplaceItem>>(items);
    }

    public async Task<Result<IReadOnlyList<MarketplaceItem>>> GetContentByCategoryAsync(ContentCategory category, CancellationToken ct = default)
    {
        var listings = await _listingEngine.GetContentByCategoryAsync(category.ToString(), null, ct);
        var items = listings.Select(MapToMarketplaceItem).ToList();
        return Result.Success<IReadOnlyList<MarketplaceItem>>(items);
    }

    public async Task<Result<MarketplaceItem>> GetContentDetailsAsync(string contentId, CancellationToken ct = default)
    {
        var listing = await _listingEngine.GetContentDetailsAsync(contentId, ct);
        if (listing == null)
            return Result.Failure<MarketplaceItem>("Content not found", ErrorType.NotFound);
        return Result.Success(MapToMarketplaceItem(listing));
    }

    public async Task<Result<string>> UploadContentAsync(ContentUploadRequest request, CancellationToken ct = default)
    {
        var result = await _listingEngine.UploadContentAsync(request.CreatorId ?? "unknown", request, ct);
        if (result.IsFailure)
            return Result.Failure<string>(result.Error ?? "Upload failed", result.ErrorType);
        return Result.Success(result.Value?.ContentId ?? "");
    }

    #endregion

    #region Search Operations

    public async Task<Result<IReadOnlyList<MarketplaceItem>>> SearchContentAsync(string query, CancellationToken ct = default)
    {
        var listings = await _searchEngine.SearchContentAsync(query, null, ct);
        var items = listings.Select(MapToMarketplaceItem).ToList();
        return Result.Success<IReadOnlyList<MarketplaceItem>>(items);
    }

    public async Task<Result<SearchResult>> AdvancedSearchAsync(SearchQuery query, CancellationToken ct = default)
    {
        var criteria = new AdvancedSearchCriteria
        {
            SearchTerm = query.SearchTerm,
            Categories = query.Category.HasValue ? new[] { query.Category.Value } : null,
            MinPrice = query.MinPrice,
            MaxPrice = query.MaxPrice,
            MinRating = query.MinRating,
            CreatorId = query.CreatorId,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
        var listings = await _searchEngine.AdvancedSearchAsync(criteria, ct);
        var items = listings.Select(MapToMarketplaceItem).ToList();
        var result = new SearchResult
        {
            Items = items,
            TotalCount = items.Count,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
        return Result.Success(result);
    }

    #endregion

    #region Purchase Operations

    public async Task<Result<ContentPurchase>> PurchaseContentAsync(string contentId, string buyerId, CancellationToken ct = default)
    {
        var result = await _purchaseEngine.PurchaseContentAsync(contentId, buyerId, ct);
        if (result.IsFailure || result.Value == null)
            return Result.Failure<ContentPurchase>(result.Error ?? "Purchase failed", result.ErrorType);
        
        var purchase = new ContentPurchase
        {
            PurchaseId = result.Value.PurchaseId,
            ContentId = result.Value.ContentId,
            BuyerId = result.Value.BuyerId,
            PurchaseAmount = result.Value.Amount,
            PurchasedAt = result.Value.PurchaseDate,
            Status = result.Value.Status,
            DownloadUrl = result.Value.DownloadUrl
        };
        return Result.Success(purchase);
    }

    public async Task<Result> DownloadContentAsync(string contentId, string userId, CancellationToken ct = default)
    {
        var result = await _purchaseEngine.DownloadContentAsync(contentId, userId, ct);
        if (result.IsFailure)
            return Result.Failure(result.Error ?? "Download failed", result.ErrorType);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<MarketplaceItem>>> GetUserLibraryAsync(string userId, CancellationToken ct = default)
    {
        var library = await _purchaseEngine.GetUserLibraryAsync(userId, ct);
        // For now return empty list as we need to map library items to marketplace items
        return Result.Success<IReadOnlyList<MarketplaceItem>>(new List<MarketplaceItem>());
    }

    public Task<bool> VerifyContentAccessAsync(string contentId, string userId, CancellationToken ct = default)
        => Task.FromResult(_purchaseEngine.VerifyContentAccess(contentId, userId));

    #endregion

    #region Review Operations

    public async Task<Result> RateContentAsync(string contentId, string userId, int rating, string? review, CancellationToken ct = default)
    {
        var result = await _reviewEngine.RateContentAsync(contentId, userId, rating, ct);
        if (result.IsFailure)
            return Result.Failure(result.Error ?? "Rating failed", result.ErrorType);
        return Result.Success();
    }

    public async Task<Result<ReviewSummary>> GetContentReviewsAsync(string contentId, CancellationToken ct = default)
    {
        var reviews = await _reviewEngine.GetContentReviewsAsync(contentId, 20, ct);
        var summary = new ReviewSummary
        {
            ContentId = contentId,
            TotalReviews = reviews.Count,
            AverageRating = reviews.Any() ? (float)reviews.Average(r => r.Rating) : 0
        };
        return Result.Success(summary);
    }

    public async Task<Result> SubmitReviewAsync(ReviewRequest request, CancellationToken ct = default)
    {
        var result = await _reviewEngine.SubmitReviewAsync(request.ContentId, request.UserId, request.Comment ?? "", ct);
        if (result.IsFailure)
            return Result.Failure(result.Error ?? "Review submission failed", result.ErrorType);
        return Result.Success();
    }

    #endregion

    #region Analytics Operations

    public async Task<Result<CreatorDashboard>> GetCreatorDashboardAsync(string creatorId, CancellationToken ct = default)
    {
        var dashboard = await _analyticsEngine.GetCreatorDashboardAsync(creatorId, ct);
        return Result.Success(dashboard);
    }

    public async Task<Result<MarketplaceStats>> GetMarketplaceStatsAsync(CancellationToken ct = default)
    {
        var stats = await _analyticsEngine.GetMarketplaceStatsAsync(ct);
        return Result.Success(stats);
    }

    public async Task<Result<SalesMetrics>> GetSalesMetricsAsync(string creatorId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        var dateRange = new Models.BalanceTuning.DateRange { Start = startDate, End = endDate };
        var metrics = await _analyticsEngine.GetSalesMetricsAsync(creatorId, dateRange, ct);
        return Result.Success(metrics);
    }

    public async Task<Result<IReadOnlyList<TrendingContent>>> GetTrendingContentAsync(int limit = 10, CancellationToken ct = default)
    {
        var listings = await _analyticsEngine.GetTrendingContentAsync(limit, ct);
        var trending = listings.Select(l => new TrendingContent
        {
            ContentId = l.ContentId,
            Name = l.Name,
            Category = l.Category,
            CreatorId = l.CreatorId,
            CreatorName = l.CreatorName,
            RecentDownloads = l.DownloadCount,
            TrendingScore = l.Rating * l.DownloadCount
        }).ToList();
        return Result.Success<IReadOnlyList<TrendingContent>>(trending);
    }

    #endregion

    private void InitializeSampleContent()
        => _listingEngine.InitializeSampleContent();

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
}
