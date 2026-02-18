namespace SaveState.Application.Mugen.Services.ContentMarketplace.Engines;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.ContentMarketplace;

/// <summary>
/// Engine for searching content in the marketplace.
/// </summary>
public class SearchEngine
{
    private readonly ILogger<SearchEngine> _logger;
    private readonly ConcurrentDictionary<string, ContentListing> _listings;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public SearchEngine(ILogger<SearchEngine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _listings = new ConcurrentDictionary<string, ContentListing>();
    }

    /// <summary>
    /// Searches content with a filter.
    /// </summary>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="filter">Optional search filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching content listings.</returns>
    public Task<IReadOnlyList<ContentListing>> SearchContentAsync(string? searchTerm, SearchFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _listings.Values.Where(l => l.Status == ContentStatus.Approved).AsEnumerable();

        // Apply search term
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(l =>
                l.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                l.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                l.Tags.Any(t => t.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        // Apply filter
        if (filter != null)
        {
            if (filter.Category.HasValue)
                query = query.Where(l => l.Category == filter.Category.Value);
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

        // Apply sorting
        query = ApplySorting(query, filter?.SortBy, filter?.SortDescending ?? true);

        var results = query.ToList();
        _logger.LogDebug("Search found {Count} results for term '{SearchTerm}'", results.Count, searchTerm);
        return Task.FromResult<IReadOnlyList<ContentListing>>(results);
    }

    /// <summary>
    /// Performs an advanced search with detailed criteria.
    /// </summary>
    /// <param name="criteria">The search criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching content listings.</returns>
    public Task<IReadOnlyList<ContentListing>> AdvancedSearchAsync(AdvancedSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var query = _listings.Values.Where(l => l.Status == ContentStatus.Approved).AsEnumerable();

        // Apply search term
        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            var term = criteria.SearchTerm.Trim().ToLowerInvariant();
            query = query.Where(l =>
                l.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                l.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                l.Tags.Any(t => t.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                l.CreatorName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        // Apply categories
        if (criteria.Categories?.Any() == true)
        {
            query = query.Where(l => criteria.Categories.Contains(l.Category));
        }

        // Apply price range
        if (criteria.MinPrice.HasValue)
            query = query.Where(l => l.Price >= criteria.MinPrice.Value);
        if (criteria.MaxPrice.HasValue)
            query = query.Where(l => l.Price <= criteria.MaxPrice.Value);

        // Apply rating range
        if (criteria.MinRating.HasValue)
            query = query.Where(l => l.Rating >= criteria.MinRating.Value);
        if (criteria.MaxRating.HasValue)
            query = query.Where(l => l.Rating <= criteria.MaxRating.Value);

        // Apply date range
        if (criteria.UploadedAfter.HasValue)
            query = query.Where(l => l.UploadDate >= criteria.UploadedAfter.Value);
        if (criteria.UploadedBefore.HasValue)
            query = query.Where(l => l.UploadDate <= criteria.UploadedBefore.Value);

        // Apply creator filter
        if (!string.IsNullOrEmpty(criteria.CreatorId))
            query = query.Where(l => l.CreatorId == criteria.CreatorId);

        // Apply featured filter
        if (criteria.IsFeatured.HasValue)
            query = query.Where(l => l.IsFeatured == criteria.IsFeatured.Value);

        // Apply tag filters
        if (criteria.IncludeTags?.Any() == true)
        {
            query = query.Where(l => criteria.IncludeTags.Any(tag => 
                l.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)));
        }

        if (criteria.ExcludeTags?.Any() == true)
        {
            query = query.Where(l => !criteria.ExcludeTags.Any(tag => 
                l.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)));
        }

        // Apply sorting
        query = ApplySorting(query, criteria.SortBy, criteria.SortDescending);

        // Apply pagination
        var pageNumber = criteria.PageNumber ?? 1;
        var pageSize = criteria.PageSize ?? 20;
        query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);

        var results = query.ToList();
        _logger.LogDebug("Advanced search found {Count} results", results.Count);
        return Task.FromResult<IReadOnlyList<ContentListing>>(results);
    }

    /// <summary>
    /// Indexes a content listing for search.
    /// </summary>
    /// <param name="listing">The content listing to index.</param>
    internal void IndexListing(ContentListing listing)
    {
        _listings[listing.ContentId] = listing;
    }

    /// <summary>
    /// Removes a listing from the search index.
    /// </summary>
    /// <param name="contentId">The content ID.</param>
    /// <returns>True if removed.</returns>
    internal bool RemoveListing(string contentId)
    {
        return _listings.TryRemove(contentId, out _);
    }

    /// <summary>
    /// Clears all indexed listings.
    /// </summary>
    internal void ClearIndex()
    {
        _listings.Clear();
    }

    /// <summary>
    /// Gets the total count of indexed listings.
    /// </summary>
    internal int GetIndexCount() => _listings.Count;

    private static IEnumerable<ContentListing> ApplySorting(IEnumerable<ContentListing> query, string? sortBy, bool descending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "price" => descending ? query.OrderByDescending(l => l.Price) : query.OrderBy(l => l.Price),
            "rating" => descending ? query.OrderByDescending(l => l.Rating) : query.OrderBy(l => l.Rating),
            "name" => descending ? query.OrderByDescending(l => l.Name) : query.OrderBy(l => l.Name),
            "date" or "uploaddate" => descending ? query.OrderByDescending(l => l.UploadDate) : query.OrderBy(l => l.UploadDate),
            "downloads" or "downloadcount" => descending ? query.OrderByDescending(l => l.DownloadCount) : query.OrderBy(l => l.DownloadCount),
            _ => descending ? query.OrderByDescending(l => l.Rating) : query.OrderBy(l => l.Rating)
        };
    }
}
