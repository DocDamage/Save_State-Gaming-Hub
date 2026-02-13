namespace SaveState.Application.Mugen.Models.ContentMarketplace;

/// <summary>
/// Search query for content.
/// </summary>
public class SearchQuery
{
    public string? SearchTerm { get; set; }
    public ContentCategory? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public IReadOnlyList<string>? Tags { get; set; }
    public float? MinRating { get; set; }
    public string? CreatorId { get; set; }
    public string? CompatibleVersion { get; set; }
    public SearchSortOption SortBy { get; set; } = SearchSortOption.Relevance;
    public bool SortDescending { get; set; } = true;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Search sort options.
/// </summary>
public enum SearchSortOption
{
    Relevance,
    Rating,
    Price,
    DownloadCount,
    UploadDate,
    Name
}

/// <summary>
/// Search result container.
/// </summary>
public class SearchResult
{
    public IReadOnlyList<MarketplaceItem> Items { get; set; } = default!;
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}

/// <summary>
/// Filter options for content browsing.
/// </summary>
public class FilterOptions
{
    public IReadOnlyList<ContentCategory> Categories { get; set; } = default!;
    public IReadOnlyList<string> AvailableTags { get; set; } = default!;
    public IReadOnlyList<string> CompatibleVersions { get; set; } = default!;
    public PriceRange PriceRange { get; set; } = default!;
    public RatingRange RatingRange { get; set; } = default!;
}

/// <summary>
/// Price range for filtering.
/// </summary>
public class PriceRange
{
    public decimal Min { get; set; }
    public decimal Max { get; set; }
}

/// <summary>
/// Rating range for filtering.
/// </summary>
public class RatingRange
{
    public float Min { get; set; }
    public float Max { get; set; }
}

/// <summary>
/// Featured content filter.
/// </summary>
public class FeaturedContentFilter
{
    public ContentCategory? Category { get; set; }
    public int? Limit { get; set; }
    public FeaturedSortOption SortBy { get; set; } = FeaturedSortOption.DownloadCount;
}

/// <summary>
/// Featured content sort options.
/// </summary>
public enum FeaturedSortOption
{
    DownloadCount,
    Rating,
    UploadDate,
    Revenue
}
