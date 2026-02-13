namespace SaveState.Application.Mugen.Models.ContentMarketplace;

/// <summary>
/// Content purchase record.
/// </summary>
public class ContentPurchase
{
    public string PurchaseId { get; set; } = default!;
    public string ContentId { get; set; } = default!;
    public string BuyerId { get; set; } = default!;
    public decimal PurchaseAmount { get; set; } = default!;
    public decimal PlatformFee { get; set; } = default!;
    public decimal CreatorRevenue { get; set; } = default!;
    public DateTime PurchasedAt { get; set; } = default!;
    public LicenseType LicenseType { get; set; } = default!;
    public string DownloadUrl { get; set; } = default!;
    public PurchaseStatus Status { get; set; } = default!;
}

/// <summary>
/// User purchase record.
/// </summary>
public class UserPurchase
{
    public string PurchaseId { get; set; } = default!;
    public string ContentId { get; set; } = default!;
    public string BuyerId { get; set; } = default!;
    public DateTime PurchasedAt { get; set; } = default!;
    public PurchaseStatus Status { get; set; } = default!;
}

/// <summary>
/// Content license record.
/// </summary>
public class ContentLicense
{
    public string LicenseId { get; set; } = default!;
    public string ContentId { get; set; } = default!;
    public string LicenseeId { get; set; } = default!;
    public LicenseType LicenseType { get; set; } = default!;
    public DateTime GrantedAt { get; set; } = default!;
    public DateTime? ExpiresAt { get; set; } = default!;
}

/// <summary>
/// Transaction details for marketplace purchases.
/// </summary>
public class Transaction
{
    public string TransactionId { get; set; } = default!;
    public string PurchaseId { get; set; } = default!;
    public string BuyerId { get; set; } = default!;
    public string SellerId { get; set; } = default!;
    public decimal Amount { get; set; } = default!;
    public decimal PlatformFee { get; set; } = default!;
    public decimal SellerRevenue { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public string PaymentMethod { get; set; } = default!;
    public TransactionStatus Status { get; set; } = default!;
}

/// <summary>
/// Transaction status enumeration.
/// </summary>
public enum TransactionStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Refunded,
    Cancelled
}

/// <summary>
/// Payment information for transactions.
/// </summary>
public class PaymentInfo
{
    public string PaymentMethodId { get; set; } = default!;
    public string PaymentMethodType { get; set; } = default!;
    public string LastFourDigits { get; set; } = default!;
    public DateTime? ExpiryDate { get; set; } = default!;
    public bool IsDefault { get; set; } = default!;
}
