namespace SaveState.Application.Mugen.Services.ContentMarketplace.Engines;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.ContentMarketplace;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for managing content listings in the marketplace.
/// </summary>
public class ListingEngine
{
    private readonly ILogger<ListingEngine> _logger;
    private readonly ICacheService _cacheService;
    private readonly ConcurrentDictionary<string, ContentListing> _listings;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListingEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="cacheService">The cache service.</param>
    /// <param name="timeProvider">The time provider.</param>
    public ListingEngine(ILogger<ListingEngine> logger, ICacheService cacheService, ITimeProvider timeProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _listings = new ConcurrentDictionary<string, ContentListing>();
    }

    /// <summary>
    /// Gets featured content items.
    /// </summary>
    /// <param name="count">Number of items to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of featured content listings.</returns>
    public Task<IReadOnlyList<ContentListing>> GetFeaturedContentAsync(int count, CancellationToken cancellationToken = default)
    {
        var featured = _listings.Values
            .Where(l => l.IsFeatured && l.Status == ContentStatus.Approved)
            .OrderByDescending(l => l.Rating)
            .ThenByDescending(l => l.DownloadCount)
            .Take(count)
            .ToList();

        _logger.LogDebug("Retrieved {Count} featured content items", featured.Count);
        return Task.FromResult<IReadOnlyList<ContentListing>>(featured);
    }

    /// <summary>
    /// Gets content by category with optional filtering.
    /// </summary>
    /// <param name="category">The category name.</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of content listings matching the criteria.</returns>
    public Task<IReadOnlyList<ContentListing>> GetContentByCategoryAsync(string category, ContentFilter? filter = null, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<ContentCategory>(category, true, out var categoryEnum))
        {
            _logger.LogWarning("Invalid category: {Category}", category);
            return Task.FromResult<IReadOnlyList<ContentListing>>(Array.Empty<ContentListing>());
        }

        var query = _listings.Values.Where(l => l.Category == categoryEnum && l.Status == ContentStatus.Approved);

        if (filter != null)
        {
            if (filter.MinPrice.HasValue)
                query = query.Where(l => l.Price >= filter.MinPrice.Value);
            if (filter.MaxPrice.HasValue)
                query = query.Where(l => l.Price <= filter.MaxPrice.Value);
            if (filter.MinRating.HasValue)
                query = query.Where(l => l.Rating >= filter.MinRating.Value);
            if (!string.IsNullOrEmpty(filter.CreatorId))
                query = query.Where(l => l.CreatorId == filter.CreatorId);
            if (filter.Tags?.Any() == true)
                query = query.Where(l => filter.Tags.Any(tag => l.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)));
        }

        var results = query.OrderByDescending(l => l.UploadDate).ToList();
        return Task.FromResult<IReadOnlyList<ContentListing>>(results);
    }

    /// <summary>
    /// Gets detailed information about a specific content item.
    /// </summary>
    /// <param name="contentId">The content ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The content listing or null if not found.</returns>
    public Task<ContentListing?> GetContentDetailsAsync(string contentId, CancellationToken cancellationToken = default)
    {
        _listings.TryGetValue(contentId, out var listing);
        return Task.FromResult(listing);
    }

    /// <summary>
    /// Uploads new content to the marketplace.
    /// </summary>
    /// <param name="creatorId">The creator ID.</param>
    /// <param name="request">The upload request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the created content listing.</returns>
    public Task<Result<ContentListing>> UploadContentAsync(string creatorId, ContentUploadRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Task.FromResult(Result.Failure<ContentListing>("Content name is required", ErrorType.Validation));
            }

            var contentId = Guid.NewGuid().ToString("N");
            var now = _timeProvider.UtcNow;

            var listing = new ContentListing
            {
                ContentId = contentId,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                Category = request.Category,
                CreatorId = creatorId,
                CreatorName = request.CreatorId ?? "Unknown",
                Price = request.Price,
                LicenseType = request.LicenseType,
                Tags = request.Tags ?? Array.Empty<string>(),
                Images = request.Images ?? Array.Empty<string>(),
                Status = ContentStatus.PendingReview,
                UploadDate = now,
                LastUpdated = now,
                Rating = 0,
                RatingCount = 0,
                DownloadCount = 0,
                IsFeatured = false,
                CompatibleVersions = request.CompatibleVersions ?? Array.Empty<string>()
            };

            _listings[contentId] = listing;
            _logger.LogInformation("Content uploaded: {ContentId} by {CreatorId}", contentId, creatorId);

            return Task.FromResult(Result.Success(listing));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload content");
            return Task.FromResult(Result.Failure<ContentListing>($"Upload failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets a specific item by ID.
    /// </summary>
    /// <param name="contentId">The content ID.</param>
    /// <returns>The content listing or null if not found.</returns>
    public ContentListing? GetItem(string contentId)
    {
        _listings.TryGetValue(contentId, out var listing);
        return listing;
    }

    /// <summary>
    /// Initializes sample content for testing/demo purposes.
    /// </summary>
    public void InitializeSampleContent()
    {
        var now = _timeProvider.UtcNow;
        var sampleListings = new[]
        {
            new ContentListing
            {
                ContentId = "sample-001",
                Name = "Dragon Fighter Character",
                Description = "A powerful dragon-themed fighting character with special moves.",
                Category = ContentCategory.Characters,
                CreatorId = "creator-001",
                CreatorName = "DragonStudio",
                Price = 4.99m,
                LicenseType = LicenseType.Permanent,
                Tags = new[] { "dragon", "fantasy", "powerful" },
                Images = new[] { "img1.jpg", "img2.jpg" },
                Status = ContentStatus.Approved,
                UploadDate = now.AddDays(-30),
                LastUpdated = now.AddDays(-5),
                Rating = 4.5f,
                RatingCount = 128,
                DownloadCount = 5420,
                IsFeatured = true,
                CompatibleVersions = new[] { "1.0", "1.1" }
            },
            new ContentListing
            {
                ContentId = "sample-002",
                Name = "Neo Tokyo Stage",
                Description = "Cyberpunk themed fighting stage with animated background.",
                Category = ContentCategory.Stages,
                CreatorId = "creator-002",
                CreatorName = "CyberArts",
                Price = 2.99m,
                LicenseType = LicenseType.Permanent,
                Tags = new[] { "cyberpunk", "city", "animated" },
                Images = new[] { "stage1.jpg" },
                Status = ContentStatus.Approved,
                UploadDate = now.AddDays(-15),
                LastUpdated = now.AddDays(-15),
                Rating = 4.8f,
                RatingCount = 89,
                DownloadCount = 3200,
                IsFeatured = true,
                CompatibleVersions = new[] { "1.0" }
            },
            new ContentListing
            {
                ContentId = "sample-003",
                Name = "Special Effects Pack",
                Description = "Collection of particle effects for custom characters.",
                Category = ContentCategory.Effects,
                CreatorId = "creator-003",
                CreatorName = "FXMaster",
                Price = 1.99m,
                LicenseType = LicenseType.Permanent,
                Tags = new[] { "effects", "particles", "visuals" },
                Images = new[] { "fx1.jpg", "fx2.jpg", "fx3.jpg" },
                Status = ContentStatus.Approved,
                UploadDate = now.AddDays(-7),
                LastUpdated = now.AddDays(-7),
                Rating = 4.2f,
                RatingCount = 45,
                DownloadCount = 890,
                IsFeatured = false,
                CompatibleVersions = new[] { "1.0", "1.1", "1.2" }
            }
        };

        foreach (var listing in sampleListings)
        {
            _listings[listing.ContentId] = listing;
        }

        _logger.LogInformation("Initialized {Count} sample content listings", sampleListings.Length);
    }
}
