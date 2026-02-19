using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using SaveState.Core.Monitoring;

namespace SaveState.Infrastructure.Health;

/// <summary>
/// Health check that exposes application metrics for monitoring and alerting.
/// Provides comprehensive application performance and health data.
/// </summary>
public class MetricsHealthCheck : IHealthCheck
{
    private readonly IApplicationMetrics _metrics;
    private readonly ILogger<MetricsHealthCheck> _logger;

    public MetricsHealthCheck(IApplicationMetrics metrics, ILogger<MetricsHealthCheck> logger)
    {
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshotResult = await _metrics.GetMetricsSnapshotAsync(cancellationToken).ConfigureAwait(false);
            if (snapshotResult.IsFailure || snapshotResult.Value is null)
            {
                return HealthCheckResult.Degraded(
                    $"Failed to retrieve metrics snapshot: {snapshotResult.Error ?? "Unknown error"}");
            }

            var snapshot = snapshotResult.Value;

            // Define health thresholds
            var healthStatus = DetermineOverallHealth(snapshot);

            var description = healthStatus switch
            {
                HealthStatus.Healthy => "Application metrics are within normal ranges",
                HealthStatus.Degraded => "Application metrics indicate performance issues",
                _ => "Application metrics indicate critical issues"
            };

            // Return health check results with comprehensive metrics data
            var data = new Dictionary<string, object>
            {
                ["TotalRequests"] = snapshot.TotalRequests,
                ["AverageResponseTime"] = snapshot.AverageResponseTime.TotalMilliseconds,
                ["CacheHitRatio"] = snapshot.CacheHitRatio,
                ["TotalCacheRequests"] = snapshot.TotalCacheRequests,
                ["UnhandledExceptions"] = snapshot.UnhandledExceptions,
                ["DatabaseErrors"] = snapshot.DatabaseErrors,
                ["SuccessfulApiCalls"] = snapshot.SuccessfulApiCalls,
                ["FailedApiCalls"] = snapshot.FailedApiCalls,
                ["TotalApiCalls"] = snapshot.TotalApiCalls,
                ["AverageDatabaseQueryTime"] = snapshot.AverageDatabaseQueryTime.TotalMilliseconds,
                ["CurrentDatabaseConnections"] = snapshot.CurrentDatabaseConnections,
                ["CurrentMemoryUsage"] = snapshot.CurrentMemoryUsage,
                ["CurrentCpuUsage"] = snapshot.CurrentCpuUsage,
                ["TotalExceptions"] = snapshot.TotalExceptions,
                ["TotalAiRequests"] = snapshot.TotalAiRequests,
                ["SuccessfulAiRequests"] = snapshot.SuccessfulAiRequests,
                ["TotalTokensUsed"] = snapshot.TotalTokensUsed
            };

            return new HealthCheckResult(healthStatus, description, data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve application metrics for health check");

            return HealthCheckResult.Unhealthy(
                "Failed to retrieve application metrics",
                ex,
                new Dictionary<string, object>
                {
                    ["Error"] = ex.Message,
                    ["ErrorType"] = ex.GetType().Name
                });
        }
    }

    private static HealthStatus DetermineOverallHealth(MetricsSnapshot snapshot)
    {
        var issues = 0;
        var criticalIssues = 0;

        // Critical issues (immediate attention required)
        if (snapshot.UnhandledExceptions > 5)
            criticalIssues++;
        if (snapshot.DatabaseErrors > 50)
            criticalIssues++;
        if (snapshot.FailedApiCalls > snapshot.SuccessfulApiCalls && snapshot.TotalApiCalls > 20)
            criticalIssues++;

        // Performance issues (monitoring required)
        if (snapshot.AverageResponseTime > TimeSpan.FromSeconds(10))
            issues++;
        if (snapshot.CacheHitRatio < 0.3 && snapshot.TotalCacheRequests > 50)
            issues++;
        if (snapshot.AverageDatabaseQueryTime > TimeSpan.FromSeconds(2))
            issues++;

        // Determine overall health
        if (criticalIssues > 0)
            return HealthStatus.Unhealthy;
        if (issues > 2)
            return HealthStatus.Degraded;

        return HealthStatus.Healthy;
    }
}
