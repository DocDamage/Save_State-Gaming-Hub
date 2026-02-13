namespace SaveState.Application.Mugen.Models.Analytics;

/// <summary>
/// Trend analysis data.
/// </summary>
public class TrendAnalysis
{
    public string Metric { get; set; } = default!;
    public TimeSpan TimePeriod { get; set; } = default!;
    public TrendDirection Direction { get; set; } = default!;
    public double Magnitude { get; set; } = default!;
    public double Confidence { get; set; } = default!;
    public int DataPoints { get; set; } = default!;
    public IReadOnlyList<string> KeyFindings { get; set; } = default!;
    public TrendForecast Forecast { get; set; } = default!;
}

/// <summary>
/// Trend forecast data.
/// </summary>
public class TrendForecast
{
    public double PredictedValue { get; set; } = default!;
    public TimeSpan ForecastPeriod { get; set; } = default!;
    public double UpperBound { get; set; } = default!;
    public double LowerBound { get; set; } = default!;
}

/// <summary>
/// Trend analysis request.
/// </summary>
public class TrendAnalysisRequest
{
    public string Metric { get; set; } = default!;
    public TimeSpan TimePeriod { get; set; } = default!;
    public IReadOnlyList<string> Filters { get; set; } = default!;
}

/// <summary>
/// Segment analysis data.
/// </summary>
public class SegmentAnalysis
{
    public string SegmentationCriteria { get; set; } = default!;
    public IReadOnlyList<UserSegment> Segments { get; set; } = default!;
    public int TotalUsers { get; set; } = default!;
    public DateTime AnalysisDate { get; set; } = default!;
    public IReadOnlyList<string> Insights { get; set; } = default!;
}

/// <summary>
/// User segment data.
/// </summary>
public class UserSegment
{
    public string SegmentId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int Size { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Characteristics { get; set; } = default!;
    public IReadOnlyDictionary<string, double> KeyMetrics { get; set; } = default!;
}

/// <summary>
/// Segment analysis request.
/// </summary>
public class SegmentAnalysisRequest
{
    public string SegmentationCriteria { get; set; } = default!;
    public IReadOnlyList<string> Metrics { get; set; } = default!;
    public int MinSegmentSize { get; set; } = default!;
}
