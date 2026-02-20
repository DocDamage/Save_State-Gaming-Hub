namespace SaveState.Core.Metrics;

/// <summary>
/// Interface for recording business, performance, and system metrics.
/// Provides a unified API for metrics collection with Prometheus/Grafana integration.
/// </summary>
public interface IMetricsService
{
    // Business Metrics
    void RecordGameLaunched(string gameName, string platform);
    void RecordSaveStateCreated(string gameName);
    void RecordSaveStateLoaded(string gameName);
    void RecordMemoryPatternDetected(string gameName, string patternType);
    void RecordAutoDiscoverySuccess(string gameName, int patternsFound);
    void RecordCheatTableImported(string gameName, int entries);

    // Performance Metrics
    void RecordMemoryScanDuration(TimeSpan duration, string gameName);
    void RecordPatternDetectionDuration(TimeSpan duration, string gameName);
    void RecordGameLaunchDuration(TimeSpan duration, string gameName);

    // System Metrics
    void RecordCpuUsage(double percentage);
    void RecordMemoryUsage(long bytes);
    void RecordDiskUsage(long bytes);

    // Error Metrics
    void RecordError(string errorType, string component);
    void RecordWarning(string warningType, string component);

    // Active Sessions
    void IncrementActiveSessions();
    void DecrementActiveSessions();
    void IncrementAttachedProcesses();
    void DecrementAttachedProcesses();
}

/// <summary>
/// Interface for exporting metrics in various formats.
/// </summary>
public interface IMetricsReporter
{
    /// <summary>
    /// Exports metrics in Prometheus text format.
    /// </summary>
    string ExportPrometheusFormat();

    /// <summary>
    /// Gets a snapshot of current metrics.
    /// </summary>
    ServiceMetricsSnapshot GetSnapshot();
}

/// <summary>
/// Snapshot of service metrics for reporting and monitoring.
/// </summary>
public class ServiceMetricsSnapshot
{
    public DateTime Timestamp { get; set; }
    public Dictionary<string, long> Counters { get; set; } = new();
    public Dictionary<string, double> Gauges { get; set; } = new();
    public Dictionary<string, MetricHistogram> Histograms { get; set; } = new();
}

/// <summary>
/// Histogram metric statistics.
/// </summary>
public class MetricHistogram
{
    public long Count { get; set; }
    public double Sum { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double Average => Count > 0 ? Sum / Count : 0;
}
