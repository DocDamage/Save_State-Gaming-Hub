namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Mobile dashboard data.
/// </summary>
public class MobileCompanionServiceMobileDashboard
{
    public string SessionId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
    public IReadOnlyList<MobileCompanionServiceQuickAction> QuickActions { get; set; } = default!;
    public MobileCompanionServiceLiveGameStats? LiveStats { get; set; } = default!;
    public IReadOnlyList<MobileCompanionServiceActivityItem> RecentActivity { get; set; } = default!;
    public IReadOnlyList<MobileCompanionServiceMobileNotification> Notifications { get; set; } = default!;
    public IReadOnlyList<MobileCompanionServiceSocialActivity> SocialFeed { get; set; } = default!;
    public IReadOnlyList<MobileCompanionServiceContentItem> ContentQueue { get; set; } = default!;
}

/// <summary>
/// Activity item.
/// </summary>
public class MobileCompanionServiceActivityItem
{
    public string ActivityId { get; set; } = default!;
    public MobileCompanionServiceActivityType Type { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Metadata { get; set; } = default!;
}

/// <summary>
/// Social activity.
/// </summary>
public class MobileCompanionServiceSocialActivity
{
    public string ActivityId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public MobileCompanionServiceSocialActivityType Type { get; set; } = default!;
    public string Content { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public int Likes { get; set; } = default!;
    public int Comments { get; set; } = default!;
}

/// <summary>
/// Content item.
/// </summary>
public class MobileCompanionServiceContentItem
{
    public string ContentId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public MobileCompanionServiceContentType Type { get; set; } = default!;
    public MobileCompanionServiceDownloadStatus Status { get; set; } = default!;
    public int Progress { get; set; } = default!;
    public long Size { get; set; } = default!;
}

/// <summary>
/// Mobile analytics data.
/// </summary>
public class MobileCompanionServiceMobileAnalytics
{
    public string UserId { get; set; } = default!;
    public TimeSpan Period { get; set; } = default!;
    public int SessionCount { get; set; } = default!;
    public TimeSpan AverageSessionLength { get; set; } = default!;
    public int CommandsSent { get; set; } = default!;
    public int NotificationsReceived { get; set; } = default!;
    public int ContentDownloaded { get; set; } = default!;
    public int RemoteMatchesStarted { get; set; } = default!;
    public IReadOnlyList<int> PeakUsageHours { get; set; } = default!;
    public IReadOnlyDictionary<MobileCompanionServiceMobilePlatform, int> DeviceBreakdown { get; set; } = default!;
    public IReadOnlyDictionary<string, int> FeatureUsage { get; set; } = default!;
}

/// <summary>
/// Achievement data.
/// </summary>
public class MobileCompanionServiceAchievement
{
    public string AchievementId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime UnlockedAt { get; set; } = default!;
}
