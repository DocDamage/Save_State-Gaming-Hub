namespace SaveState.Core.Monitoring;

using SaveState.Core.Performance.Services;
using SaveState.Core.Common;

/// <summary>
/// Interface for collecting and tracking application performance metrics.
/// Provides methods to record various performance indicators across the application.
/// </summary>
public interface IApplicationMetrics
{
    // Performance Counters
    void RecordResponseTime(string operation, TimeSpan duration);
    void RecordThroughput(string operation, int count = 1);
    void RecordMemoryUsage(long bytes);
    void RecordCpuUsage(double percentage);

    // Database Metrics
    void RecordDatabaseQuery(string operation, TimeSpan duration);
    void RecordDatabaseConnectionCount(int count);
    void RecordDatabaseError(string operation, string errorType);

    // Cache Metrics
    void RecordCacheHit(string cacheName);
    void RecordCacheMiss(string cacheName);
    void RecordCacheEviction(string cacheName);

    // API Client Metrics
    void RecordApiCall(string service, string endpoint, TimeSpan duration, bool success);
    void RecordApiRateLimit(string service, TimeSpan retryAfter);
    void RecordApiError(string service, string errorType);

    // AI Service Metrics
    void RecordAiRequest(string provider, string operation, TimeSpan duration, bool success);
    void RecordAiTokenUsage(string provider, int inputTokens, int outputTokens);

    // Error Tracking
    void RecordException(string source, string exceptionType, string message);
    void RecordUnhandledException(string source, Exception exception);

    // Custom Metrics
    void RecordCustomMetric(string name, double value, Dictionary<string, string>? tags = null);
    void IncrementCounter(string name, Dictionary<string, string>? tags = null);

    // PERFORMANCE OPTIMIZATION: Advanced performance monitoring
    void RecordSlowQuery(string operation, TimeSpan duration, int recordCount);
    void RecordBatchOperation(string operation, int itemCount, TimeSpan duration);
    void RecordPerformanceWarning(string operation, string message);
    void RecordPerformanceSnapshot(PerformanceSnapshot snapshot);

    // Health and Status
    Task<Result<MetricsSnapshot>> GetMetricsSnapshotAsync(CancellationToken ct = default);
}

/// <summary>
/// Snapshot of current metrics for health checks and monitoring.
/// </summary>
public class MetricsSnapshot
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Performance Metrics
    public TimeSpan AverageResponseTime { get; set; }
    public int TotalRequests { get; set; }
    public long CurrentMemoryUsage { get; set; }
    public double CurrentCpuUsage { get; set; }

    // Database Metrics
    public TimeSpan AverageDatabaseQueryTime { get; set; }
    public int TotalDatabaseQueries { get; set; }
    public int CurrentDatabaseConnections { get; set; }
    public int DatabaseErrors { get; set; }

    // Cache Metrics
    public double CacheHitRatio { get; set; }
    public int TotalCacheRequests { get; set; }
    public int CacheEvictions { get; set; }

    // API Metrics
    public int TotalApiCalls { get; set; }
    public int SuccessfulApiCalls { get; set; }
    public int FailedApiCalls { get; set; }

    // AI Metrics
    public int TotalAiRequests { get; set; }
    public int SuccessfulAiRequests { get; set; }
    public long TotalTokensUsed { get; set; }

    // Error Metrics
    public int TotalExceptions { get; set; }
    public int UnhandledExceptions { get; set; }
    public Dictionary<string, int> ExceptionsByType { get; set; } = new();

    // Custom Metrics
    public Dictionary<string, double> CustomMetrics { get; set; } = new();
    public Dictionary<string, long> Counters { get; set; } = new();
}
