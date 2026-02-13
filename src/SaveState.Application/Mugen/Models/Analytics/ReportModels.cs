namespace SaveState.Application.Mugen.Models.Analytics;

/// <summary>
/// Business analytics report data.
/// </summary>
public class BusinessAnalyticsReport
{
    public string ReportId { get; set; } = default!;
    public ReportType ReportType { get; set; } = default!;
    public DateTime StartDate { get; set; } = default!;
    public DateTime EndDate { get; set; } = default!;
    public int DataPoints { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
    public ReportSummary Summary { get; set; } = default!;
    public IReadOnlyDictionary<string, object> DetailedMetrics { get; set; } = default!;
    public IReadOnlyList<string> Recommendations { get; set; } = default!;
}

/// <summary>
/// Report summary data.
/// </summary>
public class ReportSummary
{
    public int TotalUsers { get; set; } = default!;
    public int ActiveUsers { get; set; } = default!;
    public decimal TotalRevenue { get; set; } = default!;
    public TimeSpan AverageSessionTime { get; set; } = default!;
    public IReadOnlyDictionary<string, double> TopMetrics { get; set; } = default!;
}

/// <summary>
/// Analytics report request.
/// </summary>
public class AnalyticsReportRequest
{
    public ReportType ReportType { get; set; } = default!;
    public DateTime StartDate { get; set; } = default!;
    public DateTime EndDate { get; set; } = default!;
    public IReadOnlyList<string> Metrics { get; set; } = default!;
    public IReadOnlyList<string> Dimensions { get; set; } = default!;
}
