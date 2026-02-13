namespace SaveState.Application.Mugen.Models.Analytics;

/// <summary>
/// Business intelligence report data.
/// </summary>
public class BusinessIntelligenceReport
{
    public string ReportId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string FocusArea { get; set; } = default!;
    public TimeSpan TimePeriod { get; set; } = default!;
    public IReadOnlyList<BusinessInsight> KeyInsights { get; set; } = default!;
    public IReadOnlyList<string> StrategicRecommendations { get; set; } = default!;
    public IReadOnlyList<AnalyticsRiskAssessment> RiskAssessments { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Business insight data.
/// </summary>
public class BusinessInsight
{
    public string InsightId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public InsightImpact Impact { get; set; } = default!;
    public double Confidence { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Data { get; set; } = default!;
    public IReadOnlyList<string> Recommendations { get; set; } = default!;
}

/// <summary>
/// Risk assessment data.
/// </summary>
public class AnalyticsRiskAssessment
{
    public string RiskId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public RiskSeverity Severity { get; set; } = default!;
    public double Probability { get; set; } = default!;
    public IReadOnlyList<string> MitigationStrategies { get; set; } = default!;
}

/// <summary>
/// BI report request.
/// </summary>
public class BIReportRequest
{
    public string FocusArea { get; set; } = default!;
    public TimeSpan TimePeriod { get; set; } = default!;
    public IReadOnlyList<string> KeyMetrics { get; set; } = default!;
    public IReadOnlyList<string> AnalysisDimensions { get; set; } = default!;
}
