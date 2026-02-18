namespace SaveState.Application.Mugen.Services.ContentMarketplace.Engines;

using SaveState.Application.Mugen.Models.ContentMarketplace;

/// <summary>
/// Service for handling payments in the marketplace.
/// </summary>
public interface IPaymentService
{
    Task<PaymentResult> ProcessPaymentAsync(string buyerId, decimal amount, string contentId, CancellationToken ct = default);
    Task<bool> RefundPaymentAsync(string purchaseId, CancellationToken ct = default);
}

/// <summary>
/// Result of a payment operation.
/// </summary>
public class PaymentResult
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Service for managing content access.
/// </summary>
public interface IContentAccessService
{
    Task<string> GenerateDownloadUrlAsync(string contentId, string userId, CancellationToken ct = default);
    Task<bool> GrantAccessAsync(string contentId, string userId, CancellationToken ct = default);
    Task<bool> RevokeAccessAsync(string contentId, string userId, CancellationToken ct = default);
}

/// <summary>
/// Service for managing content licenses.
/// </summary>
public interface ILicenseManager
{
    Task<bool> ValidateLicenseAsync(string contentId, string userId, CancellationToken ct = default);
    Task<LicenseInfo?> GetLicenseAsync(string contentId, string userId, CancellationToken ct = default);
}

/// <summary>
/// License information.
/// </summary>
public class LicenseInfo
{
    public string LicenseId { get; set; } = string.Empty;
    public string ContentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public LicenseType Type { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsValid { get; set; }
}
