using SaveState.Application.Mugen.Models.ContentMarketplace;
using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services.ContentMarketplace;

/// <summary>
/// MUGEN Content Marketplace service interface.
/// Provides distribution, monetization, licensing, and community-driven content ecosystem.
/// </summary>
public interface IMugenContentMarketplaceService
{
    // Listing operations
    Task<Result<IReadOnlyList<MarketplaceItem>>> GetFeaturedContentAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<MarketplaceItem>>> GetContentByCategoryAsync(ContentCategory category, CancellationToken ct = default);
    Task<Result<MarketplaceItem>> GetContentDetailsAsync(string contentId, CancellationToken ct = default);
    Task<Result<string>> UploadContentAsync(ContentUploadRequest request, CancellationToken ct = default);

    // Search operations
    Task<Result<IReadOnlyList<MarketplaceItem>>> SearchContentAsync(string query, CancellationToken ct = default);
    Task<Result<SearchResult>> AdvancedSearchAsync(SearchQuery query, CancellationToken ct = default);

    // Purchase operations
    Task<Result<ContentPurchase>> PurchaseContentAsync(string contentId, string buyerId, CancellationToken ct = default);
    Task<Result> DownloadContentAsync(string contentId, string userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MarketplaceItem>>> GetUserLibraryAsync(string userId, CancellationToken ct = default);
    Task<bool> VerifyContentAccessAsync(string contentId, string userId, CancellationToken ct = default);

    // Review operations
    Task<Result> RateContentAsync(string contentId, string userId, int rating, string? review, CancellationToken ct = default);
    Task<Result<ReviewSummary>> GetContentReviewsAsync(string contentId, CancellationToken ct = default);
    Task<Result> SubmitReviewAsync(ReviewRequest request, CancellationToken ct = default);

    // Analytics operations
    Task<Result<CreatorDashboard>> GetCreatorDashboardAsync(string creatorId, CancellationToken ct = default);
    Task<Result<MarketplaceStats>> GetMarketplaceStatsAsync(CancellationToken ct = default);
    Task<Result<SalesMetrics>> GetSalesMetricsAsync(string creatorId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TrendingContent>>> GetTrendingContentAsync(int limit = 10, CancellationToken ct = default);
}
