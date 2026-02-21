using SaveState.Core.Common;

namespace SaveState.Core.GameLibrary.Services;

public interface IPerformanceProfiler
{
    Task<Result> StartProfilingAsync(Guid gameId, CancellationToken ct = default);
    Task<Result> StopProfilingAsync(CancellationToken ct = default);
    Task<Result<PerformanceMetrics>> GetCurrentMetricsAsync(CancellationToken ct = default);
    Task<Result<PerformanceReport>> GenerateReportAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<BottleneckAnalysis>>> AnalyzeBottlenecksAsync(CancellationToken ct = default);
    bool IsProfiling { get; }
    event EventHandler<PerformanceMetricsUpdatedEventArgs>? MetricsUpdated;
}

public sealed record PerformanceMetrics(
    DateTime Timestamp,
    double Fps,
    double FrameTimeMs,
    double CpuUsagePercent,
    double GpuUsagePercent,
    long MemoryUsageBytes,
    long GpuMemoryBytes,
    double NetworkLatencyMs,
    IReadOnlyList<SubsystemMetrics> Subsystems);

public sealed record SubsystemMetrics(
    string SubsystemName,
    double UsagePercent,
    double TemperatureCelsius,
    string Status);

public sealed record PerformanceReport(
    Guid GameId,
    DateTime StartTime,
    DateTime EndTime,
    TimeSpan Duration,
    PerformanceMetrics AverageMetrics,
    PerformanceMetrics PeakMetrics,
    PerformanceMetrics MinMetrics,
    IReadOnlyList<PerformanceIssue> Issues,
    IReadOnlyList<Recommendation> Recommendations);

public sealed record BottleneckAnalysis(
    string Component,
    BottleneckSeverity Severity,
    string Description,
    double ImpactPercent,
    IReadOnlyList<string> Solutions);

public sealed record PerformanceIssue(
    string IssueType,
    string Description,
    PerformanceSeverity Severity,
    double ImpactPercent,
    IReadOnlyList<string> Causes);

public sealed record Recommendation(
    string Title,
    string Description,
    RecommendationPriority Priority,
    IReadOnlyList<string> Actions);

public sealed class PerformanceMetricsUpdatedEventArgs : EventArgs
{
    public required PerformanceMetrics Metrics { get; init; }
}

public enum BottleneckSeverity
{
    None,
    Minor,
    Moderate,
    Severe,
    Critical
}

public enum PerformanceSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum RecommendationPriority
{
    Low,
    Medium,
    High,
    Critical
}