namespace SaveState.Application.Mugen.Services.WebPortal;

/// <summary>
/// Page view analytics data.
/// </summary>
public class PageView
{
    public string ViewId { get; set; } = default!;
    public string PagePath { get; set; } = default!;
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; } = default!;
    public string UserAgent { get; set; } = default!;
    public string? Referrer { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// User activity tracking.
/// </summary>
public class UserActivity
{
    public string ActivityId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string ActivityType { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public string? Metadata { get; set; }
    public string? IpAddress { get; set; }
}

/// <summary>
/// Engagement metrics.
/// </summary>
public class WebPortalServiceEngagementMetrics
{
    public TimeSpan AverageSessionLength { get; set; } = default!;
    public int DailyActiveUsers { get; set; } = default!;
    public int WeeklyActiveUsers { get; set; } = default!;
    public int MonthlyActiveUsers { get; set; } = default!;
    public double ContentConsumptionRate { get; set; } = default!;
    public double SocialInteractionRate { get; set; } = default!;
    public double ForumParticipationRate { get; set; } = default!;
}

/// <summary>
/// Community stats data.
/// </summary>
public class WebPortalServiceCommunityStats
{
    public int TotalUsers { get; set; } = default!;
    public int ActiveUsers { get; set; } = default!;
    public int TotalForumPosts { get; set; } = default!;
    public int TotalForumThreads { get; set; } = default!;
    public int TotalContentSubmissions { get; set; } = default!;
    public int ApprovedContent { get; set; } = default!;
    public DateTime PeriodStart { get; set; } = default!;
    public DateTime PeriodEnd { get; set; } = default!;
    public IReadOnlyList<WebPortalServiceTopContributor> TopContributors { get; set; } = default!;
    public IReadOnlyList<WebPortalServicePopularTag> PopularTags { get; set; } = default!;
    public WebPortalServiceEngagementMetrics WebPortalServiceEngagementMetrics { get; set; } = default!;
}

/// <summary>
/// Analytics query parameters.
/// </summary>
public class AnalyticsQuery
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? MetricType { get; set; }
    public string? Dimension { get; set; }
    public IReadOnlyList<string> Filters { get; set; } = default!;
}

/// <summary>
/// Analytics report data.
/// </summary>
public class AnalyticsReport
{
    public string ReportId { get; set; } = default!;
    public string ReportType { get; set; } = default!;
    public DateTime GeneratedAt { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public IReadOnlyDictionary<string, object> Data { get; set; } = default!;
}
