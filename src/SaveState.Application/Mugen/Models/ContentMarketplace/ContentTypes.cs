namespace SaveState.Application.Mugen.Models.ContentMarketplace;

/// <summary>
/// Content listing for marketplace display.
/// </summary>
public class ContentListing
{
    public string ContentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ContentCategory Category { get; set; }
    public string CreatorId { get; set; } = string.Empty;
    public string CreatorName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public LicenseType LicenseType { get; set; }
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Images { get; set; } = Array.Empty<string>();
    public ContentStatus Status { get; set; }
    public DateTime UploadDate { get; set; }
    public DateTime LastUpdated { get; set; }
    public float Rating { get; set; }
    public int RatingCount { get; set; }
    public int DownloadCount { get; set; }
    public bool IsFeatured { get; set; }
    public IReadOnlyList<string> CompatibleVersions { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Content filter for browsing.
/// </summary>
public class ContentFilter
{
    public ContentCategory? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public float? MinRating { get; set; }
    public IReadOnlyList<string>? Tags { get; set; }
    public string? CreatorId { get; set; }
    public string? SearchTerm { get; set; }
}

/// <summary>
/// Search filter for content search.
/// </summary>
public class SearchFilter
{
    public string? SearchTerm { get; set; }
    public ContentCategory? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public float? MinRating { get; set; }
    public IReadOnlyList<string>? Tags { get; set; }
    public string? CreatorId { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Advanced search criteria for detailed searches.
/// </summary>
public class AdvancedSearchCriteria
{
    public string? SearchTerm { get; set; }
    public IReadOnlyList<ContentCategory>? Categories { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public float? MinRating { get; set; }
    public float? MaxRating { get; set; }
    public IReadOnlyList<string>? IncludeTags { get; set; }
    public IReadOnlyList<string>? ExcludeTags { get; set; }
    public string? CreatorId { get; set; }
    public DateTime? UploadedAfter { get; set; }
    public DateTime? UploadedBefore { get; set; }
    public bool? IsFeatured { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
}

/// <summary>
/// Purchase result after content purchase.
/// </summary>
public class PurchaseResult
{
    public string PurchaseId { get; set; } = string.Empty;
    public string ContentId { get; set; } = string.Empty;
    public string BuyerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PurchaseStatus Status { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}

/// <summary>
/// Download result for content downloads.
/// </summary>
public class DownloadResult
{
    public string ContentId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Library item for user's purchased content.
/// </summary>
public class LibraryItem
{
    public string ContentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ContentCategory Category { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public DateTime PurchasedAt { get; set; }
    public LicenseType LicenseType { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}
