namespace SaveState.Core.Subscriptions;

/// <summary>
/// Gaming subscription service types.
/// </summary>
public enum SubscriptionType
{
    GamePass,
    PlayStationPlus,
    EaPlay,
    UbisoftPlus,
    GeForceNow,
    HumbleChoice,
    AmazonLuna,
    NintendoSwitchOnline,
    AppleArcade,
    Other
}

/// <summary>
/// Gaming subscription service types.
/// </summary>
public enum SubscriptionServiceType
{
    XboxGamePass,
    XboxGamePassUltimate,
    PlayStationPlus,
    PlayStationPlusExtra,
    PlayStationPlusPremium,
    EAPlay,
    EAPlayPro,
    UbisoftPlus,
    NintendoSwitchOnline,
    NintendoSwitchOnlineExpansion,
    AmazonLuna,
    GeForceNow,
    HumbleChoice,
    AppleArcade,
    GooglePlayPass
}

/// <summary>
/// Subscription service information.
/// </summary>
public class SubscriptionServiceInfo
{
    public string Id { get; set; } = string.Empty;
    public SubscriptionServiceType Type { get; set; }
    public SubscriptionType SubscriptionType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public string? WebsiteUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime? SubscriptionExpiryDate { get; set; }
    public int GameCount { get; set; }
    public bool SupportsCloudGaming { get; set; }
    public bool SupportsEaPlay { get; set; }
    public List<SubscriptionFeature> Features { get; set; } = new();
}

/// <summary>
/// Subscription service features.
/// </summary>
public class SubscriptionFeature
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsIncluded { get; set; }
}

/// <summary>
/// Game available on a subscription service.
/// </summary>
public class SubscriptionGame
{
    public string GameId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public List<SubscriptionServiceType> AvailableOn { get; set; } = new();
    public DateTime? AddedDate { get; set; }
    public DateTime? LeavingSoonDate { get; set; }
    public bool IsLeavingSoon => IsLeavingSoonAt(DateTime.UtcNow);
    public bool IsNewArrival => IsNewArrivalAt(DateTime.UtcNow);
    
    public bool IsLeavingSoonAt(DateTime currentTime) => LeavingSoonDate.HasValue && LeavingSoonDate.Value <= currentTime.AddDays(14);
    public bool IsNewArrivalAt(DateTime currentTime) => AddedDate.HasValue && AddedDate.Value >= currentTime.AddDays(-30);
    public List<string> Genres { get; set; } = new();
    public int? MetacriticScore { get; set; }
    public TimeSpan? AveragePlaytime { get; set; }
}

/// <summary>
/// User's subscription library.
/// </summary>
public class UserSubscriptionLibrary
{
    public List<SubscriptionServiceType> ActiveSubscriptions { get; set; } = new();
    public List<SubscriptionGame> Games { get; set; } = new();
    public int TotalGames => Games.Count;
    public int LeavingSoonCount => Games.Count(g => g.IsLeavingSoon);
    public int NewArrivalsCount => Games.Count(g => g.IsNewArrival);
    public DateTime LastSyncDate { get; set; }
}

/// <summary>
/// Recommendation for subscription games.
/// </summary>
public class SubscriptionRecommendation
{
    public SubscriptionGame Game { get; set; } = null!;
    public double MatchScore { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<string> BasedOnGames { get; set; } = new();
}

/// <summary>
/// Subscription comparison data.
/// </summary>
public class SubscriptionComparison
{
    public List<SubscriptionServiceInfo> Services { get; set; } = new();
    public List<string> UniqueGamesByService { get; set; } = new();
    public decimal TotalMonthlyCost { get; set; }
    public int TotalUniqueGames { get; set; }
    public string BestValueRecommendation { get; set; } = string.Empty;
}

/// <summary>
/// Game leaving soon alert.
/// </summary>
public class LeavingSoonAlert
{
    public SubscriptionGame Game { get; set; } = null!;
    public DateTime LeavingDate { get; set; }
    public int DaysRemaining => (LeavingDate - DateTime.UtcNow).Days;
    public int GetDaysRemaining(DateTime currentTime) => (LeavingDate - currentTime).Days;
    public bool IsUrgent => DaysRemaining <= 7;
}

/// <summary>
/// Entity representing a user's subscription to a gaming service.
/// </summary>
public class UserSubscriptionEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string SubscriptionType { get; set; } = string.Empty;
    public string? Tier { get; set; }
    public decimal MonthlyPrice { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AutoRenew { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Entity representing a game the user wants to track for subscription availability.
/// </summary>
public class TrackedGameEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string GameTitle { get; set; } = string.Empty;
    public string? PreferredServiceId { get; set; }
    public string? Notes { get; set; }
    public DateTime TrackedAt { get; set; }
    public bool NotifyOnAvailable { get; set; } = true;
    public bool NotifyOnLeaving { get; set; } = true;
}

/// <summary>
/// Entity for caching subscription service catalog data.
/// </summary>
public class SubscriptionCatalogCacheEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ServiceId { get; set; } = string.Empty;
    public string GameTitle { get; set; } = string.Empty;
    public string? Developer { get; set; }
    public string? Publisher { get; set; }
    public DateTime? DateAdded { get; set; }
    public DateTime? DateLeaving { get; set; }
    public string? Genres { get; set; }
    public int? MetacriticScore { get; set; }
    public DateTime CachedAt { get; set; }
}

/// <summary>
/// Legacy subscription game model for backward compatibility.
/// </summary>
public class GameSubscription
{
    public string GameTitle { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public DateTime? DateAdded { get; set; }
    public DateTime? DateLeaving { get; set; }
}

/// <summary>
/// Legacy subscription alert model for backward compatibility.
/// </summary>
public class SubscriptionAlert
{
    public string ServiceName { get; set; } = string.Empty;
    public string GameTitle { get; set; } = string.Empty;
    public DateTime LeavingDate { get; set; }
    public AlertType Type { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Alert types for subscription notifications.
/// </summary>
public enum AlertType
{
    LeavingSoon,
    NowAvailable,
    PriceChange,
    ServiceChange
}
