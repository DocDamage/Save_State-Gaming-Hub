namespace SaveState.Application.Mugen.Models.Analytics;

/// <summary>
/// Performance metrics data for analytics.
/// </summary>
public class AnalyticsPerformanceMetrics
{
    public string Category { get; set; } = default!;
    public IReadOnlyDictionary<string, MetricData> Metrics { get; set; } = default!;
    public TimeSpan TimeRange { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Metric data.
/// </summary>
public class MetricData
{
    public string Name { get; set; } = default!;
    public double Value { get; set; } = default!;
    public string Unit { get; set; } = default!;
    public MetricTrend Trend { get; set; } = default!;
    public double? Target { get; set; } = default!;
}

/// <summary>
/// Performance metrics request.
/// </summary>
public class AnalyticsPerformanceMetricsRequest
{
    public string Category { get; set; } = default!;
    public IReadOnlyList<string> Metrics { get; set; } = default!;
    public TimeSpan TimeRange { get; set; } = default!;
}

/// <summary>
/// Anomaly report data.
/// </summary>
public class AnomalyReport
{
    public string DataType { get; set; } = default!;
    public TimeSpan TimePeriod { get; set; } = default!;
    public IReadOnlyList<Anomaly> Anomalies { get; set; } = default!;
}

/// <summary>
/// Anomaly data.
/// </summary>
public class Anomaly
{
    public string AnomalyId { get; set; } = default!;
    public AnomalyType Type { get; set; } = default!;
    public AnomalySeverity Severity { get; set; } = default!;
    public DateTime DetectedAt { get; set; } = default!;
    public string Description { get; set; } = default!;
    public double Deviation { get; set; } = default!;
}

/// <summary>
/// Data export report.
/// </summary>
public class DataExportReport
{
    public string ExportId { get; set; } = default!;
    public ExportFormat Format { get; set; } = default!;
    public long FileSize { get; set; } = default!;
    public string DownloadUrl { get; set; } = default!;
    public DateTime ExportedAt { get; set; } = default!;
    public DateTime ExpiresAt { get; set; } = default!;
}

/// <summary>
/// Data export request.
/// </summary>
public class DataExportRequest
{
    public ExportFormat Format { get; set; } = default!;
    public DateTime StartDate { get; set; } = default!;
    public DateTime EndDate { get; set; } = default!;
    public IReadOnlyList<string> DataTypes { get; set; } = default!;
    public IReadOnlyList<string> Filters { get; set; } = default!;
}
