namespace SaveState.Application.Mugen.Services.MatchAnalytics;

/// <summary>
/// Complete analytics report with all sections.
/// </summary>
public record AnalyticsReport(
    Guid ReportId,
    ReportType Type,
    DateTime GeneratedAt,
    Guid SubjectId,
    string Title,
    IReadOnlyList<ReportSection> Sections,
    IReadOnlyList<ReportInsight> KeyInsights,
    AnalyticsStatus Status);

/// <summary>
/// Individual section within an analytics report.
/// </summary>
public record ReportSection(
    string Title,
    string Content,
    ReportSectionType SectionType,
    IReadOnlyDictionary<string, object> Data);

/// <summary>
/// Types of report sections.
/// </summary>
public enum ReportSectionType
{
    Summary,
    Statistics,
    Patterns,
    Trends,
    Recommendations,
    Comparison,
    Visualization
}

/// <summary>
/// Key insight extracted from analysis.
/// </summary>
public record ReportInsight(
    string Category,
    string Description,
    AnalyticsPriority Priority,
    IReadOnlyList<string> SupportingData);

/// <summary>
/// Personalized improvement recommendation.
/// </summary>
public record ImprovementRecommendation(
    string Category,
    string Recommendation,
    string Rationale,
    AnalyticsPriority Priority,
    IReadOnlyList<string> ActionSteps);

/// <summary>
/// Performance trends over a time period.
/// </summary>
public record PerformanceTrends(
    Guid PlayerId,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<TrendPoint> WinRateTrend,
    IReadOnlyList<TrendPoint> DamageTrend,
    IReadOnlyList<TrendPoint> ComboTrend,
    IReadOnlyList<string> NotableChanges);

/// <summary>
/// Comprehensive match analysis result.
/// </summary>
public record MatchAnalysis(
    Guid MatchId,
    PlayerMatchStats Player1Performance,
    PlayerMatchStats Player2Performance,
    IReadOnlyList<string> KeyMoments,
    IReadOnlyList<string> TurningPoints,
    string OverallAnalysis);

/// <summary>
/// Player statistics summary.
/// </summary>
public record PlayerStatistics(
    Guid PlayerId,
    int TotalMatches,
    int Wins,
    int Losses,
    decimal WinRate,
    IReadOnlyDictionary<string, CharacterStats> CharacterStats,
    IReadOnlyList<AchievementData> Achievements,
    RankingInfo Ranking);
