namespace SaveState.Application.Mugen.Models.Educational;

/// <summary>
/// Content analytics data.
/// </summary>
public class ContentAnalytics
{
    public TimeSpan Period { get; set; } = default!;
    public int TotalTutorials { get; set; } = default!;
    public int TotalStrategyGuides { get; set; } = default!;
    public int TotalMechanicsGuides { get; set; } = default!;
    public int TotalLearningPaths { get; set; } = default!;
    public IReadOnlyDictionary<string, int> PopularCategories { get; set; } = default!;
    public IReadOnlyDictionary<string, double> CompletionRates { get; set; } = default!;
    public UserEngagement UserEngagement { get; set; } = default!;
    public ContentQuality ContentQuality { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// User engagement metrics.
/// </summary>
public class UserEngagement
{
    public TimeSpan AverageSessionLength { get; set; } = default!;
    public int TotalSessions { get; set; } = default!;
    public int UniqueUsers { get; set; } = default!;
    public double ReturnRate { get; set; } = default!;
}

/// <summary>
/// Content quality metrics.
/// </summary>
public class ContentQuality
{
    public double AverageRating { get; set; } = default!;
    public int TotalRatings { get; set; } = default!;
    public int HighlyRatedContent { get; set; } = default!;
    public double ContentFreshness { get; set; } = default!;
}

/// <summary>
/// User dashboard data.
/// </summary>
public class UserDashboard
{
    public string UserId { get; set; } = default!;
    public LearningProgress LearningProgress { get; set; } = default!;
    public IReadOnlyList<RecommendedContent> RecommendedContent { get; set; } = default!;
    public DateTime LastLogin { get; set; } = default!;
    public TimeSpan TotalTimeSpent { get; set; } = default!;
}

/// <summary>
/// Recommended content item.
/// </summary>
public class RecommendedContent
{
    public string ContentId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string RecommendationReason { get; set; } = default!;
}
