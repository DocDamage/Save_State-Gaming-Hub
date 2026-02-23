using System.Linq;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Prize pool data.
/// </summary>
public class MugenPrizePoolServicePrizePool
{
    public string PrizePoolId { get; set; } = default!;
    public string TournamentId { get; set; } = default!;
    public decimal TotalPool { get; set; } = default!;
    public decimal EntryFee { get; set; } = default!;
    public int MaxParticipants { get; set; } = default!;
    public int CurrentParticipants { get; set; } = default!;
    public decimal GuaranteedPrize { get; set; } = default!;
    public decimal HousePercentage { get; set; } = default!;
    public MugenPrizePoolServicePrizeDistributionRules MugenPrizePoolServicePrizeDistribution { get; set; } = default!;
    public List<MugenPrizePoolServiceSponsorContribution> Sponsors { get; set; } = default!;
    public MugenPrizePoolServicePrizePoolStatus Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime? EntryDeadline { get; set; } = default!;
    public MugenPrizePoolServicePrizePoolFunds Funds { get; set; } = default!;
    public List<MugenPrizePoolServicePrizePoolTransaction> Transactions { get; set; } = default!;
    public DateTime? DistributedAt { get; set; } = default!;
}

/// <summary>
/// Prize pool status enumeration.
/// </summary>
public enum MugenPrizePoolServicePrizePoolStatus
{
    Open,
    Closed,
    Distributing,
    Distributed
}

/// <summary>
/// Prize pool funds.
/// </summary>
public class MugenPrizePoolServicePrizePoolFunds
{
    public decimal EntryFees { get; set; } = default!;
    public decimal Sponsorships { get; set; } = default!;
    public decimal HouseContribution { get; set; } = default!;
    public decimal TotalAvailable { get; set; } = default!;
}

/// <summary>
/// Prize distribution rules.
/// </summary>
public class MugenPrizePoolServicePrizeDistributionRules
{
    public string DistributionType { get; set; } = default!;
    public IReadOnlyDictionary<int, decimal>? CustomDistribution { get; set; } = default!;
}

/// <summary>
/// Prize pool creation request.
/// </summary>
public class MugenPrizePoolServicePrizePoolCreationRequest
{
    public string TournamentId { get; set; } = default!;
    public decimal EntryFee { get; set; } = default!;
    public int MaxParticipants { get; set; } = default!;
    public decimal GuaranteedPrize { get; set; } = default!;
    public decimal HousePercentage { get; set; } = default!;
    public MugenPrizePoolServicePrizeDistributionRules MugenPrizePoolServicePrizeDistribution { get; set; } = default!;
    public DateTime? EntryDeadline { get; set; } = default!;
}

/// <summary>
/// Entry payment request.
/// </summary>
public class MugenPrizePoolServiceEntryPaymentRequest
{
    public string PrizePoolId { get; set; } = default!;
    public string PlayerId { get; set; } = default!;
    public MugenPrizePoolServicePaymentMethod MugenPrizePoolServicePaymentMethod { get; set; } = default!;
}

/// <summary>
/// Payment method enumeration.
/// </summary>
public enum MugenPrizePoolServicePaymentMethod
{
    CreditCard,
    PayPal,
    Crypto,
    BankTransfer,
    PlatformWallet
}

/// <summary>
/// Payment result.
/// </summary>
public class MugenPrizePoolServicePaymentResult
{
    public string TransactionId { get; set; } = default!;
    public decimal Amount { get; set; } = default!;
    public MugenPrizePoolServicePaymentStatus Status { get; set; } = default!;
    public DateTime ProcessedAt { get; set; } = default!;
}

/// <summary>
/// Payment status enumeration.
/// </summary>
public enum MugenPrizePoolServicePaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}

/// <summary>
/// Sponsorship contribution request.
/// </summary>
public class MugenPrizePoolServiceSponsorshipContributionRequest
{
    public string PrizePoolId { get; set; } = default!;
    public string SponsorId { get; set; } = default!;
    public string SponsorName { get; set; } = default!;
    public decimal Amount { get; set; } = default!;
    public MugenPrizePoolServicePaymentMethod MugenPrizePoolServicePaymentMethod { get; set; } = default!;
    public string? LogoUrl { get; set; } = default!;
    public string? WebsiteUrl { get; set; } = default!;
    public MugenPrizePoolServiceSponsorshipVisibility VisibilityLevel { get; set; } = default!;
}

/// <summary>
/// Sponsorship visibility enumeration.
/// </summary>
public enum MugenPrizePoolServiceSponsorshipVisibility
{
    Banner,
    Logo,
    Named,
    Title
}

/// <summary>
/// Sponsor contribution.
/// </summary>
public class MugenPrizePoolServiceSponsorContribution
{
    public string SponsorId { get; set; } = default!;
    public string SponsorName { get; set; } = default!;
    public decimal Amount { get; set; } = default!;
    public string PaymentId { get; set; } = default!;
    public DateTime ContributionDate { get; set; } = default!;
    public string? LogoUrl { get; set; } = default!;
    public string? WebsiteUrl { get; set; } = default!;
    public MugenPrizePoolServiceSponsorshipVisibility VisibilityLevel { get; set; } = default!;
    public DateTime AgreedAt { get; set; }
}

/// <summary>
/// Tournament entry.
/// </summary>
public class MugenPrizePoolServiceTournamentEntry
{
    public string EntryId { get; set; } = default!;
    public string PrizePoolId { get; set; } = default!;
    public string PlayerId { get; set; } = default!;
    public decimal EntryFee { get; set; } = default!;
    public DateTime EntryTime { get; set; } = default!;
    public string PaymentId { get; set; } = default!;
    public MugenPrizePoolServiceEntryStatus Status { get; set; } = default!;
}

/// <summary>
/// Entry status enumeration.
/// </summary>
public enum MugenPrizePoolServiceEntryStatus
{
    Pending,
    Confirmed,
    Refunded,
    Cancelled
}

/// <summary>
/// Prize distribution.
/// </summary>
public class MugenPrizePoolServicePrizeDistribution
{
    public string PrizePoolId { get; set; } = default!;
    public decimal TotalPool { get; set; } = default!;
    public decimal HouseCut { get; set; } = default!;
    public IReadOnlyList<MugenPrizePoolServicePrizeTier> Prizes { get; set; } = default!;
    public DateTime CalculatedAt { get; set; } = default!;
    public decimal TotalPayout => Prizes.Sum(p => p.Amount);

    public decimal GetPrizeForPlacement(int placement)
    {
        return Prizes.FirstOrDefault(p => p.Placement == placement)?.Amount ?? 0;
    }
}

/// <summary>
/// Prize tier.
/// </summary>
public class MugenPrizePoolServicePrizeTier
{
    public int Placement { get; set; } = default!;
    public decimal Amount { get; set; } = default!;
    public decimal Percentage { get; set; } = default!;
}

/// <summary>
/// Tournament result.
/// </summary>
public class MugenPrizePoolServiceTournamentResult
{
    public string PlayerId { get; set; } = default!;
    public int Placement { get; set; } = default!;
    public string? TeamId { get; set; } = default!;
}

/// <summary>
/// Prize pool transaction.
/// </summary>
public class MugenPrizePoolServicePrizePoolTransaction
{
    public string TransactionId { get; set; } = default!;
    public MugenPrizePoolServiceTransactionType Type { get; set; } = default!;
    public decimal Amount { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public bool Processed { get; set; } = default!;
    public string? PlayerId { get; set; } = default!;
    public string? SponsorId { get; set; } = default!;
}

/// <summary>
/// Transaction type enumeration.
/// </summary>
public enum MugenPrizePoolServiceTransactionType
{
    EntryFee,
    Sponsorship,
    HouseContribution,
    PrizePayout,
    Refund
}

/// <summary>
/// Payout result.
/// </summary>
public class MugenPrizePoolServicePayoutResult
{
    public string RecipientId { get; set; } = default!;
    public decimal Amount { get; set; } = default!;
    public MugenPrizePoolServicePayoutStatus Status { get; set; } = default!;
    public DateTime ProcessedAt { get; set; } = default!;
    public string TransactionId { get; set; } = default!;
}

/// <summary>
/// Payout status enumeration.
/// </summary>
public enum MugenPrizePoolServicePayoutStatus
{
    Pending,
    Completed,
    Failed
}

/// <summary>
/// Prize pool analytics.
/// </summary>
public class MugenPrizePoolServicePrizePoolAnalytics
{
    public string PrizePoolId { get; set; } = default!;
    public decimal TotalPool { get; set; } = default!;
    public decimal EntryFeesCollected { get; set; } = default!;
    public decimal SponsorshipRevenue { get; set; } = default!;
    public decimal HouseContribution { get; set; } = default!;
    public int ParticipantsCount { get; set; } = default!;
    public double CapacityUtilization { get; set; } = default!;
    public decimal AverageEntryFee { get; set; } = default!;
    public decimal ProjectedPayout { get; set; } = default!;
    public int TransactionCount { get; set; } = default!;
    public DateTime LastActivity { get; set; } = default!;
}

/// <summary>
/// Payment transaction.
/// </summary>
public class MugenPrizePoolServicePaymentTransaction
{
    public string TransactionId { get; set; } = default!;
    public string PayerId { get; set; } = default!;
    public decimal Amount { get; set; } = default!;
    public MugenPrizePoolServicePaymentMethod Method { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public MugenPrizePoolServicePaymentStatus Status { get; set; } = default!;
}
