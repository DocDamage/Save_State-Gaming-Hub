using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.Monitoring;

namespace SaveState.Infrastructure.Health;

/// <summary>
/// Health check for performance degradation detection.
/// Monitors response times, throughput, and other performance metrics.
/// </summary>
public class PerformanceHealthCheck : IHealthCheck
{
    private readonly IApplicationMetrics _metrics;
    private readonly ILogger<PerformanceHealthCheck> _logger;
    private readonly ITimeProvider _timeProvider;

    // Performance thresholds
    private const double MaxAverageResponseTimeSeconds = 5.0;
    private const double MinRequestThroughputPerMinute = 10.0; // Minimum requests per minute
    private const double MaxErrorRatePercent = 5.0; // Maximum 5% error rate
    private const double MinCacheHitRatio = 0.7; // Minimum 70% cache hit ratio
    private const int MinSampleSize = 50; // Minimum number of requests to evaluate

    // Track historical performance for trend analysis
    private static readonly Dictionary<string, PerformanceHistory> _performanceHistory = new();
    private static readonly object _historyLock = new();

    public PerformanceHealthCheck(IApplicationMetrics metrics, ILogger<PerformanceHealthCheck> logger, ITimeProvider timeProvider)
    {
        _metrics = metrics;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshotResult = await _metrics.GetMetricsSnapshotAsync(cancellationToken);
            if (snapshotResult.IsFailure || snapshotResult.Value is null)
            {
                var error = snapshotResult.Error ?? "Unknown metrics retrieval failure";
                return HealthCheckResult.Degraded($"Unable to retrieve performance metrics: {error}");
            }

            var snapshot = snapshotResult.Value;
            var results = new Dictionary<string, object>();
            var issues = new List<string>();
            var criticalIssues = new List<string>();

            // Evaluate current performance metrics
            EvaluateCurrentPerformance(snapshot, results, issues, criticalIssues, _timeProvider);

            // Analyze performance trends
            AnalyzePerformanceTrends(snapshot, results, issues, criticalIssues);

            // Evaluate system performance
            EvaluateSystemPerformance(snapshot, results, issues, criticalIssues, _timeProvider);

            // Store current metrics for trend analysis
            UpdatePerformanceHistory(snapshot);

            // Determine overall health
            if (criticalIssues.Any())
            {
                return HealthCheckResult.Unhealthy(
                    $"Critical performance issues detected: {string.Join(", ", criticalIssues)}",
                    data: results);
            }

            if (issues.Any())
            {
                return HealthCheckResult.Degraded(
                    $"Performance degradation detected: {string.Join(", ", issues)}",
                    data: results);
            }

            return HealthCheckResult.Healthy("Application performance is within acceptable ranges", results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check performance health");
            return HealthCheckResult.Unhealthy("Performance health check failed", ex);
        }
    }

    private static void EvaluateCurrentPerformance(
        MetricsSnapshot snapshot,
        Dictionary<string, object> results,
        List<string> issues,
        List<string> criticalIssues,
        ITimeProvider timeProvider)
    {
        // Response time check
        var avgResponseTime = snapshot.AverageResponseTime.TotalSeconds;
        results["AverageResponseTime"] = new
        {
            Value = avgResponseTime,
            Threshold = MaxAverageResponseTimeSeconds,
            Unit = "seconds"
        };

        if (avgResponseTime > MaxAverageResponseTimeSeconds * 1.5) // 150% of threshold
        {
            criticalIssues.Add($"Response time critically high: {avgResponseTime:F2}s");
        }
        else if (avgResponseTime > MaxAverageResponseTimeSeconds)
        {
            issues.Add($"Response time elevated: {avgResponseTime:F2}s");
        }

        // Error rate check
        var errorRate = snapshot.TotalApiCalls > 0
            ? (double)snapshot.FailedApiCalls / snapshot.TotalApiCalls * 100
            : 0;
        results["ErrorRate"] = new
        {
            Value = errorRate,
            Threshold = MaxErrorRatePercent,
            Unit = "percent"
        };

        if (errorRate > MaxErrorRatePercent * 2) // 200% of threshold
        {
            criticalIssues.Add($"Error rate critically high: {errorRate:F1}%");
        }
        else if (errorRate > MaxErrorRatePercent)
        {
            issues.Add($"Error rate elevated: {errorRate:F1}%");
        }

        // Request throughput check (if we have enough data)
        if (snapshot.TotalRequests >= MinSampleSize)
        {
            // Calculate approximate uptime based on timestamp (assuming metrics are collected regularly)
            var uptime = timeProvider.UtcNow - snapshot.Timestamp;
            var throughputPerMinute = uptime.TotalMinutes > 0
                ? snapshot.TotalRequests / uptime.TotalMinutes
                : snapshot.TotalRequests;

            results["RequestThroughput"] = new
            {
                Value = throughputPerMinute,
                Threshold = MinRequestThroughputPerMinute,
                Unit = "requests/minute"
            };

            if (throughputPerMinute < MinRequestThroughputPerMinute / 2) // 50% of threshold
            {
                issues.Add($"Request throughput low: {throughputPerMinute:F1} req/min");
            }
        }

        // Cache performance check (if cache is being used)
        if (snapshot.TotalCacheRequests > MinSampleSize)
        {
            // Use the cache hit ratio directly from the snapshot
            var cacheHitRatio = snapshot.CacheHitRatio;
            results["CacheHitRatio"] = new
            {
                Value = cacheHitRatio,
                Threshold = MinCacheHitRatio,
                Unit = "ratio"
            };

            if (cacheHitRatio < MinCacheHitRatio / 2) // 50% of threshold
            {
                issues.Add($"Cache hit ratio critically low: {cacheHitRatio:P1}");
            }
            else if (cacheHitRatio < MinCacheHitRatio)
            {
                issues.Add($"Cache hit ratio low: {cacheHitRatio:P1}");
            }
        }
    }

    private static void AnalyzePerformanceTrends(
        MetricsSnapshot snapshot,
        Dictionary<string, object> results,
        List<string> issues,
        List<string> criticalIssues)
    {
        lock (_historyLock)
        {
            var key = "performance";
            if (!_performanceHistory.TryGetValue(key, out var history))
            {
                history = new PerformanceHistory();
                _performanceHistory[key] = history;
            }

            // Add current snapshot to history
            history.AddSnapshot(snapshot);

            // Analyze trends if we have enough historical data
            if (history.Snapshots.Count >= 3)
            {
                var trend = history.GetTrendAnalysis();
                results["PerformanceTrend"] = trend;

                // Check for degrading trends
                if (trend.ResponseTimeIncreasing && snapshot.AverageResponseTime > TimeSpan.FromSeconds(2))
                {
                    issues.Add("Response time trending upward");
                }

                if (trend.ErrorRateIncreasing && snapshot.TotalApiCalls > 0)
                {
                    var errorRate = (double)snapshot.FailedApiCalls / snapshot.TotalApiCalls * 100;
                    if (errorRate > 2)
                    {
                        issues.Add("Error rate trending upward");
                    }
                }
            }
        }
    }

    private static void EvaluateSystemPerformance(
        MetricsSnapshot snapshot,
        Dictionary<string, object> results,
        List<string> issues,
        List<string> criticalIssues,
        ITimeProvider timeProvider)
    {
        // Database performance check
        var avgDbTime = snapshot.AverageDatabaseQueryTime.TotalMilliseconds;
        results["AverageDatabaseQueryTime"] = new
        {
            Value = avgDbTime,
            Threshold = 1000, // 1 second
            Unit = "milliseconds"
        };

        if (avgDbTime > 5000) // 5 seconds - critical
        {
            criticalIssues.Add($"Database query time critically high: {avgDbTime:F0}ms");
        }
        else if (avgDbTime > 1000) // 1 second - warning
        {
            issues.Add($"Database query time elevated: {avgDbTime:F0}ms");
        }

        // Memory usage check (if available)
        if (snapshot.CurrentMemoryUsage > 0)
        {
            var memoryUsageMB = snapshot.CurrentMemoryUsage / (1024 * 1024);
            results["MemoryUsage"] = new
            {
                Value = memoryUsageMB,
                Threshold = 500, // 500MB warning threshold
                Unit = "MB"
            };

            if (memoryUsageMB > 1000) // 1GB - critical
            {
                criticalIssues.Add($"Memory usage critically high: {memoryUsageMB:F0}MB");
            }
            else if (memoryUsageMB > 500) // 500MB - warning
            {
                issues.Add($"Memory usage elevated: {memoryUsageMB:F0}MB");
            }
        }

        // Exception rate check
        if (snapshot.TotalRequests > MinSampleSize && snapshot.UnhandledExceptions > 0)
        {
            var exceptionRate = (double)snapshot.UnhandledExceptions / snapshot.TotalRequests * 100;
            results["ExceptionRate"] = new
            {
                Value = exceptionRate,
                Threshold = 1.0, // 1% threshold
                Unit = "percent"
            };

            if (exceptionRate > 5) // 5% - critical
            {
                criticalIssues.Add($"Exception rate critically high: {exceptionRate:F2}%");
            }
            else if (exceptionRate > 1) // 1% - warning
            {
                issues.Add($"Exception rate elevated: {exceptionRate:F2}%");
            }
        }
    }

    private static void UpdatePerformanceHistory(MetricsSnapshot snapshot)
    {
        lock (_historyLock)
        {
            var key = "performance";
            if (!_performanceHistory.TryGetValue(key, out var history))
            {
                history = new PerformanceHistory();
                _performanceHistory[key] = history;
            }

            history.AddSnapshot(snapshot);

            // Keep only last 10 snapshots for trend analysis
            if (history.Snapshots.Count > 10)
            {
                history.Snapshots.RemoveAt(0);
            }
        }
    }

    private class PerformanceHistory
    {
        public List<MetricsSnapshot> Snapshots { get; } = new();

        public void AddSnapshot(MetricsSnapshot snapshot)
        {
            Snapshots.Add(snapshot);
        }

        public PerformanceTrend GetTrendAnalysis()
        {
            if (Snapshots.Count < 3)
                return new PerformanceTrend();

            var recent = Snapshots.TakeLast(3).ToList();
            var oldest = recent[0];
            var newest = recent[2];

            return new PerformanceTrend
            {
                ResponseTimeIncreasing = newest.AverageResponseTime > oldest.AverageResponseTime,
                ErrorRateIncreasing = CalculateErrorRate(newest) > CalculateErrorRate(oldest),
                ThroughputDecreasing = CalculateThroughput(newest) < CalculateThroughput(oldest)
            };
        }

        private static double CalculateErrorRate(MetricsSnapshot snapshot)
        {
            return snapshot.TotalApiCalls > 0
                ? (double)snapshot.FailedApiCalls / snapshot.TotalApiCalls
                : 0;
        }

        private static double CalculateThroughput(MetricsSnapshot snapshot)
        {
            // Calculate approximate uptime based on timestamp
            var uptime = DateTime.UtcNow - snapshot.Timestamp;
            return uptime.TotalMinutes > 0
                ? snapshot.TotalRequests / uptime.TotalMinutes
                : 0;
        }
    }

    private class PerformanceTrend
    {
        public bool ResponseTimeIncreasing { get; set; }
        public bool ErrorRateIncreasing { get; set; }
        public bool ThroughputDecreasing { get; set; }
    }
}
