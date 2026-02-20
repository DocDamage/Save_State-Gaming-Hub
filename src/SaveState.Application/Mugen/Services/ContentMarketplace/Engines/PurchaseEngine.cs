namespace SaveState.Application.Mugen.Services.ContentMarketplace.Engines;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.ContentMarketplace;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for handling content purchases and downloads.
/// </summary>
public class PurchaseEngine
{
    private readonly ILogger<PurchaseEngine> _logger;
    private readonly IPaymentService? _paymentService;
    private readonly IContentAccessService? _contentAccessService;
    private readonly ILicenseManager? _licenseManager;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, PurchaseRecord> _purchases;
    private readonly ConcurrentDictionary<string, LibraryItem> _library;

    /// <summary>
    /// Initializes a new instance of the <see cref="PurchaseEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="paymentService">The payment service (optional).</param>
    /// <param name="contentAccessService">The content access service (optional).</param>
    /// <param name="licenseManager">The license manager (optional).</param>
    /// <param name="timeProvider">The time provider.</param>
    public PurchaseEngine(
        ILogger<PurchaseEngine> logger,
        IPaymentService? paymentService,
        IContentAccessService? contentAccessService,
        ILicenseManager? licenseManager,
        ITimeProvider timeProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _paymentService = paymentService;
        _contentAccessService = contentAccessService;
        _licenseManager = licenseManager;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _purchases = new ConcurrentDictionary<string, PurchaseRecord>();
        _library = new ConcurrentDictionary<string, LibraryItem>();
    }

    /// <summary>
    /// Purchases content for a user.
    /// </summary>
    /// <param name="contentId">The content ID.</param>
    /// <param name="userId">The buyer user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing purchase details.</returns>
    public async Task<Result<PurchaseResult>> PurchaseContentAsync(string contentId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(contentId))
                return Result.Failure<PurchaseResult>("Content ID is required", ErrorType.Validation);
            if (string.IsNullOrWhiteSpace(userId))
                return Result.Failure<PurchaseResult>("User ID is required", ErrorType.Validation);

            // Check if already purchased
            if (HasPurchased(contentId, userId))
            {
                return Result.Failure<PurchaseResult>("Content already purchased", ErrorType.Conflict);
            }

            var purchaseId = Guid.NewGuid().ToString("N");
            var now = _timeProvider.UtcNow;

            // If payment service is available, process payment
            if (_paymentService != null)
            {
                var paymentResult = await _paymentService.ProcessPaymentAsync(userId, 0m, contentId, cancellationToken).ConfigureAwait(false);
                if (!paymentResult.Success)
                {
                    return Result.Failure<PurchaseResult>($"Payment failed: {paymentResult.ErrorMessage}", ErrorType.External);
                }
            }

            // Grant access if service available
            if (_contentAccessService != null)
            {
                await _contentAccessService.GrantAccessAsync(contentId, userId, cancellationToken).ConfigureAwait(false);
            }

            var purchaseRecord = new PurchaseRecord
            {
                PurchaseId = purchaseId,
                ContentId = contentId,
                UserId = userId,
                PurchaseDate = now,
                Status = PurchaseStatus.Completed
            };

            _purchases[purchaseId] = purchaseRecord;

            var result = new PurchaseResult
            {
                PurchaseId = purchaseId,
                ContentId = contentId,
                BuyerId = userId,
                Amount = 0m,
                Status = PurchaseStatus.Completed,
                PurchaseDate = now,
                DownloadUrl = $"/api/content/{contentId}/download"
            };

            _logger.LogInformation("Content purchased: {ContentId} by {UserId}", contentId, userId);
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to purchase content {ContentId}", contentId);
            return Result.Failure<PurchaseResult>($"Purchase failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Downloads purchased content.
    /// </summary>
    /// <param name="contentId">The content ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing download information.</returns>
    public async Task<Result<DownloadResult>> DownloadContentAsync(string contentId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!HasPurchased(contentId, userId) && !await VerifyContentAccessAsync(contentId, userId, cancellationToken).ConfigureAwait(false))
            {
                return Result.Failure<DownloadResult>("Access denied. Content not purchased.", ErrorType.Forbidden);
            }

            var downloadUrl = _contentAccessService != null
                ? await _contentAccessService.GenerateDownloadUrlAsync(contentId, userId, cancellationToken).ConfigureAwait(false)
                : $"/api/content/{contentId}/download?user={userId}";

            var result = new DownloadResult
            {
                ContentId = contentId,
                FileName = $"{contentId}.zip",
                DownloadUrl = downloadUrl,
                FileSize = 0,
                ContentType = "application/zip",
                ExpiresAt = _timeProvider.UtcNow.AddHours(24)
            };

            _logger.LogDebug("Download generated for content {ContentId} by user {UserId}", contentId, userId);
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download content {ContentId}", contentId);
            return Result.Failure<DownloadResult>($"Download failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets all items in a user's library.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of library items.</returns>
    public Task<IReadOnlyList<LibraryItem>> GetUserLibraryAsync(string userId, CancellationToken cancellationToken = default)
    {
        var items = _library.Values
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.PurchasedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<LibraryItem>>(items);
    }

    /// <summary>
    /// Verifies if a user has access to content.
    /// </summary>
    /// <param name="contentId">The content ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if access is granted.</returns>
    public async Task<bool> VerifyContentAccessAsync(string contentId, string userId, CancellationToken cancellationToken = default)
    {
        if (_licenseManager != null)
        {
            return await _licenseManager.ValidateLicenseAsync(contentId, userId, cancellationToken).ConfigureAwait(false);
        }

        return HasPurchased(contentId, userId);
    }

    /// <summary>
    /// Checks if a user has purchased specific content.
    /// </summary>
    /// <param name="contentId">The content ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if purchased.</returns>
    public bool HasPurchased(string contentId, string userId)
    {
        return _purchases.Values.Any(p => 
            p.ContentId == contentId && 
            p.UserId == userId && 
            p.Status == PurchaseStatus.Completed);
    }

    /// <summary>
    /// Adds an item to the user's library.
    /// </summary>
    /// <param name="item">The library item.</param>
    internal void AddToLibrary(LibraryItem item)
    {
        var key = $"{item.UserId}:{item.ContentId}";
        _library[key] = item;
    }

    private class PurchaseRecord
    {
        public string PurchaseId { get; set; } = string.Empty;
        public string ContentId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public PurchaseStatus Status { get; set; }
    }
}
