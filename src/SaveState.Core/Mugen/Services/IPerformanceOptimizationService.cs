using SaveState.Core.Common;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Service for performance optimization and system enhancements.
/// </summary>
public interface IPerformanceOptimizationService
{
    /// <summary>
    /// Optimizes game settings for current hardware.
/// </summary>
    Task<Result<OptimizationResult>> OptimizeSettingsAsync(SystemInfo systemInfo, CancellationToken ct = default);

    /// <summary>
    /// Monitors and reports performance metrics.
/// </summary>
    Task<Result<PerformanceMetrics>> GetPerformanceMetricsAsync(CancellationToken ct = default);

    /// <summary>
    /// Applies performance patches to characters.
/// </summary>
    Task<Result<PatchResult>> ApplyPerformancePatchesAsync(string characterName, CancellationToken ct = default);

    /// <summary>
    /// Optimizes stage loading and rendering.
/// </summary>
    Task<Result> OptimizeStageAsync(string stageName, CancellationToken ct = default);

    /// <summary>
    /// Manages memory usage during gameplay.
/// </summary>
    Task<Result<MemoryOptimizationResult>> OptimizeMemoryUsageAsync(CancellationToken ct = default);

    /// <summary>
    /// Detects and resolves performance bottlenecks.
/// </summary>
    Task<Result<IReadOnlyList<BottleneckResolution>>> ResolveBottlenecksAsync(PerformanceMetrics metrics, CancellationToken ct = default);

    /// <summary>
    /// Configures network settings for optimal online play.
/// </summary>
    Task<Result<NetworkOptimizationResult>> OptimizeNetworkSettingsAsync(NetworkInfo networkInfo, CancellationToken ct = default);

    /// <summary>
    /// Generates performance reports.
/// </summary>
    Task<Result<PerformanceReport>> GeneratePerformanceReportAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
}

/// <summary>
/// System information for optimization.
/// </summary>
public record SystemInfo(
    string CpuModel,
    int CpuCores,
    long RamBytes,
    string GpuModel,
    long GpuVramBytes,
    string OsVersion,
    bool Is64Bit);

/// <summary>
/// Result of optimization process.
/// </summary>
public record OptimizationResult(
    IReadOnlyDictionary<string, string> RecommendedSettings,
    IReadOnlyList<string> AppliedOptimizations,
    decimal ExpectedPerformanceGain,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Performance metrics.
/// </summary>
public record PerformanceMetrics(
    decimal Fps,
    decimal FrameTimeMs,
    long MemoryUsageBytes,
    decimal CpuUsage,
    decimal GpuUsage,
    long DiskReadBytesPerSecond,
    long DiskWriteBytesPerSecond,
    long NetworkLatencyMs,
    long NetworkBandwidthBytesPerSecond);

/// <summary>
/// Result of applying patches.
/// </summary>
public record PatchResult(
    string CharacterName,
    IReadOnlyList<string> AppliedPatches,
    decimal PerformanceImprovement,
    bool RequiresRestart);

/// <summary>
/// Memory optimization result.
/// </summary>
public record MemoryOptimizationResult(
    long MemoryBeforeBytes,
    long MemoryAfterBytes,
    decimal MemoryReductionPercentage,
    IReadOnlyList<string> OptimizationActions);

/// <summary>
/// Resolution for performance bottlenecks.
/// </summary>
public record BottleneckResolution(
    string BottleneckType,
    string Description,
    string Resolution,
    decimal ExpectedImprovement,
    bool RequiresRestart);

/// <summary>
/// Network information.
/// </summary>
public record NetworkInfo(
    long DownloadSpeedBytesPerSecond,
    long UploadSpeedBytesPerSecond,
    long LatencyMs,
    string ConnectionType,
    string Region);

/// <summary>
/// Network optimization result.
/// </summary>
public record NetworkOptimizationResult(
    IReadOnlyDictionary<string, string> NetworkSettings,
    decimal LatencyReductionMs,
    decimal PacketLossReduction,
    IReadOnlyList<string> Recommendations);

/// <summary>
/// Comprehensive performance report.
/// </summary>
public record PerformanceReport(
    DateTime GeneratedAt,
    TimeSpan ReportPeriod,
    SystemInfo SystemInfo,
    PerformanceMetrics AverageMetrics,
    PerformanceMetrics PeakMetrics,
    PerformanceMetrics MinimumMetrics,
    IReadOnlyList<PerformanceTrend> Trends,
    IReadOnlyList<PerformanceIssue> Issues,
    IReadOnlyList<PerformanceRecommendation> Recommendations);

/// <summary>
/// Performance trend over time.
/// </summary>
public record PerformanceTrend(
    string MetricName,
    IReadOnlyList<TrendPoint> DataPoints,
    TrendDirection Direction,
    decimal ChangePercentage);

/// <summary>
/// Data point for trend analysis.
/// </summary>
public record TrendPoint(
    DateTime Timestamp,
    decimal Value);

/// <summary>
/// Direction of performance trend.
/// </summary>
public enum TrendDirection
{
    Improving,
    Degrading,
    Stable,
    Fluctuating
}

/// <summary>
/// Identified performance issue.
/// </summary>
public record PerformanceIssue(
    string IssueType,
    string Description,
    IssueSeverity Severity,
    string Impact,
    IReadOnlyList<string> AffectedComponents);

/// <summary>
/// Severity levels for performance issues.
/// </summary>
public enum IssueSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Performance recommendation.
/// </summary>
public record PerformanceRecommendation(
    string Category,
    string Recommendation,
    string Rationale,
    PerformanceRecommendationPriority Priority,
    decimal ExpectedImprovement,
    IReadOnlyList<string> ImplementationSteps);

/// <summary>
/// Priority levels for performance recommendations.
/// </summary>
public enum PerformanceRecommendationPriority
{
    Low,
    Medium,
    High,
    Critical
}