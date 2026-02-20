using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using System.Collections.Generic;

namespace SaveState.Infrastructure.Community;

/// <summary>
/// Community marketplace for sharing mods, themes, and content.
/// PHASE 7: REQUIRED - Community Marketplace (Session 5)
/// </summary>
public class CommunityMarketplaceService
{
    private readonly ILogger<CommunityMarketplaceService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, MarketplaceItem> _items = new();
    private readonly Dictionary<string, UserReview> _reviews = new();

    public CommunityMarketplaceService(ILogger<CommunityMarketplaceService> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Lists items in the marketplace.
    /// </summary>
    public async Task<Result<List<MarketplaceItem>>> ListItemsAsync(
        MarketplaceCategory? category = null,
        string? searchQuery = null,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Listing marketplace items");

            var items = _items.Values.AsEnumerable();

            if (category.HasValue)
            {
                items = items.Where(i => i.Category == category);
            }

            if (!string.IsNullOrEmpty(searchQuery))
            {
                items = items.Where(i => i.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            var result = items
                .OrderByDescending(i => i.Rating)
                .Take(pageSize)
                .ToList();

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list marketplace items");
            return Result.Failure<List<MarketplaceItem>>(
                $"List failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <summary>
    /// Uploads an item to the marketplace.
    /// </summary>
    public async Task<Result<MarketplaceItem>> UploadItemAsync(
        string creatorId,
        string name,
        string description,
        MarketplaceCategory category,
        string fileUrl,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Uploading marketplace item: {Name}", name);

            var item = new MarketplaceItem(
                id: Guid.NewGuid().ToString(),
                creatorId: creatorId,
                name: name,
                description: description,
                category: category,
                fileUrl: fileUrl,
                uploadedAt: _timeProvider.UtcNow,
                downloads: 0,
                rating: 0,
                reviewCount: 0);

            _items[item.Id] = item;

            _logger.LogInformation("Marketplace item uploaded: {ItemId}", item.Id);
            return Result.Success(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload marketplace item: {Name}", name);
            return Result.Failure<MarketplaceItem>(
                $"Upload failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <summary>
    /// Downloads an item from the marketplace.
    /// </summary>
    public async Task<Result<string>> DownloadItemAsync(
        string itemId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_items.TryGetValue(itemId, out var item))
            {
                return Result.Failure<string>("Item not found", ErrorType.Validation);
            }

            _logger.LogInformation("Downloading marketplace item: {ItemId}", itemId);

            item.Downloads++;

            return Result.Success(item.FileUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download marketplace item: {ItemId}", itemId);
            return Result.Failure<string>(
                $"Download failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <summary>
    /// Submits a review for an item.
    /// </summary>
    public async Task<Result> SubmitReviewAsync(
        string itemId,
        string userId,
        int rating,
        string comment,
        CancellationToken ct = default)
    {
        try
        {
            if (!_items.TryGetValue(itemId, out var item))
            {
                return Result.Failure("Item not found", ErrorType.Validation);
            }

            if (rating < 1 || rating > 5)
            {
                return Result.Failure("Rating must be between 1 and 5", ErrorType.Validation);
            }

            _logger.LogInformation("Submitting review for item: {ItemId}", itemId);

            var review = new UserReview(
                Id: Guid.NewGuid().ToString(),
                ItemId: itemId,
                UserId: userId,
                Rating: rating,
                Comment: comment,
                SubmittedAt: _timeProvider.UtcNow);

            _reviews[review.Id] = review;

            // Update item rating
            item.ReviewCount++;
            item.Rating = (item.Rating * (item.ReviewCount - 1) + rating) / item.ReviewCount;

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit review");
            return Result.Failure($"Review submission failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets reviews for an item.
    /// </summary>
    public async Task<Result<List<UserReview>>> GetItemReviewsAsync(
        string itemId,
        CancellationToken ct = default)
    {
        try
        {
            var reviews = _reviews.Values
                .Where(r => r.ItemId == itemId)
                .OrderByDescending(r => r.SubmittedAt)
                .ToList();

            return Result.Success(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get reviews");
            return Result.Failure<List<UserReview>>(
                $"Fetch failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <summary>
    /// Searches for items in marketplace.
    /// </summary>
    public async Task<Result<List<MarketplaceItem>>> SearchItemsAsync(
        string searchQuery,
        CancellationToken ct = default)
    {
        try
        {
            var results = _items.Values
                .Where(i => i.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                           i.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => i.Rating)
                .ToList();

            return Result.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search items");
            return Result.Failure<List<MarketplaceItem>>(
                $"Search failed: {ex.Message}",
                ErrorType.Internal);
        }
    }
}

/// <summary>
/// Marketplace item.
/// </summary>
public class MarketplaceItem
{
    public string Id { get; set; }
    public string CreatorId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public MarketplaceCategory Category { get; set; }
    public string FileUrl { get; set; }
    public DateTime UploadedAt { get; set; }
    public int Downloads { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }

    public MarketplaceItem(
        string id,
        string creatorId,
        string name,
        string description,
        MarketplaceCategory category,
        string fileUrl,
        DateTime uploadedAt,
        int downloads,
        double rating,
        int reviewCount)
    {
        Id = id;
        CreatorId = creatorId;
        Name = name;
        Description = description;
        Category = category;
        FileUrl = fileUrl;
        UploadedAt = uploadedAt;
        Downloads = downloads;
        Rating = rating;
        ReviewCount = reviewCount;
    }
}

/// <summary>
/// Marketplace category.
/// </summary>
public enum MarketplaceCategory
{
    Mod,
    Theme,
    Shader,
    SaveState,
    Cheat,
    Tutorial,
    Gameplay,
    Other
}

/// <summary>
/// User review.
/// </summary>
public record UserReview(
    string Id,
    string ItemId,
    string UserId,
    int Rating,
    string Comment,
    DateTime SubmittedAt);
