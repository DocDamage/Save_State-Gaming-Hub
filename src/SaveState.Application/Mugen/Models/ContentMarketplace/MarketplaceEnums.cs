namespace SaveState.Application.Mugen.Models.ContentMarketplace;

/// <summary>
/// Content category enumeration.
/// </summary>
public enum ContentCategory
{
    Characters,
    Stages,
    Screenpacks,
    Music,
    Effects,
    Tools,
    Tutorials
}

/// <summary>
/// Content status enumeration.
/// </summary>
public enum ContentStatus
{
    PendingReview,
    Approved,
    Rejected,
    Suspended
}

/// <summary>
/// License type enumeration.
/// </summary>
public enum LicenseType
{
    Permanent,
    Subscription,
    Rental
}

/// <summary>
/// Purchase status enumeration.
/// </summary>
public enum PurchaseStatus
{
    Pending,
    Completed,
    Refunded,
    Failed,
    Disputed
}

/// <summary>
/// Content type enumeration.
/// </summary>
public enum ContentType
{
    Character,
    Stage,
    Screenpack,
    MusicPack,
    EffectPack,
    Tool,
    Tutorial,
    Bundle
}

/// <summary>
/// Listing status for marketplace items.
/// </summary>
public enum ListingStatus
{
    Draft,
    UnderReview,
    Published,
    Unlisted,
    Archived,
    Removed
}
