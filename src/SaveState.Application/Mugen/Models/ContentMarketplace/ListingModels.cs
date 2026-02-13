namespace SaveState.Application.Mugen.Models.ContentMarketplace;

/// <summary>
/// Marketplace item representing content for sale.
/// </summary>
public class MarketplaceItem
{
    public string ContentId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ContentCategory Category { get; set; } = default!;
    public string CreatorId { get; set; } = default!;
    public string CreatorName { get; set; } = default!;
    public decimal Price { get; set; } = default!;
    public LicenseType LicenseType { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
    public IReadOnlyList<string> Images { get; set; } = default!;
    public IReadOnlyList<string> ContentFiles { get; set; } = default!;
    public ContentStatus Status { get; set; } = default!;
    public DateTime UploadDate { get; set; } = default!;
    public DateTime LastUpdated { get; set; } = default!;
    public float Rating { get; set; } = default!;
    public int RatingCount { get; set; } = default!;
    public int DownloadCount { get; set; } = default!;
    public bool IsFeatured { get; set; } = default!;
    public IReadOnlyList<string> CompatibleVersions { get; set; } = default!;
}

/// <summary>
/// Detailed listing information.
/// </summary>
public class ListingDetails
{
    public string ContentId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ContentCategory Category { get; set; } = default!;
    public string CreatorId { get; set; } = default!;
    public string CreatorName { get; set; } = default!;
    public decimal Price { get; set; } = default!;
    public LicenseType LicenseType { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
    public IReadOnlyList<string> Images { get; set; } = default!;
    public IReadOnlyList<string> ContentFiles { get; set; } = default!;
    public ContentStatus Status { get; set; } = default!;
    public DateTime UploadDate { get; set; } = default!;
    public DateTime LastUpdated { get; set; } = default!;
    public float Rating { get; set; } = default!;
    public int RatingCount { get; set; } = default!;
    public int DownloadCount { get; set; } = default!;
    public bool IsFeatured { get; set; } = default!;
    public IReadOnlyList<string> CompatibleVersions { get; set; } = default!;
    public IReadOnlyList<Review> Reviews { get; set; } = default!;
    public ReviewSummary ReviewSummary { get; set; } = default!;
}

/// <summary>
/// Content upload request.
/// </summary>
public class ContentUploadRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ContentCategory Category { get; set; } = default!;
    public string CreatorId { get; set; } = default!;
    public decimal Price { get; set; } = default!;
    public LicenseType LicenseType { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
    public IReadOnlyList<string> Images { get; set; } = default!;
    public IReadOnlyList<string> ContentFiles { get; set; } = default!;
    public IReadOnlyList<string> CompatibleVersions { get; set; } = default!;
}
