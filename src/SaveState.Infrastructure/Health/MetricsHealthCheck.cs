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
            var snapshot = await _metrics.GetMetricsSnapshotAsync(cancellationToken).ConfigureAwait(false);

            // Define health thresholds
            var healthStatus = DetermineOverallHealth(snapshot);

            var description = healthStatus switch
            {
                HealthStatus.Healthy => "Application metrics are within normal ranges",
                HealthStatus.Degraded => "Application metrics indicate performance issues",
                _ => "Application metrics indicate critical issues"
            };

            // For now, return simple health check results without data
            // TODO: Add data parameter support when HealthCheckResult API is clarified
            return new HealthCheckResult(healthStatus, description);
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
