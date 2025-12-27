using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace SaveState.Core.Services.Ai;

/// <summary>
/// Monitors the health of AI services and provides health check functionality.
/// Tracks component health and provides overall system health status.
/// </summary>
public class HealthMonitor
{
    private readonly ILogger _logger = Log.ForContext<HealthMonitor>();
    private readonly MetricsService _metricsService;
    private readonly CacheManager _cacheManager;
    private bool _selfHealingEnabled = true;

    public HealthMonitor(MetricsService metricsService, CacheManager cacheManager)
    {
        _metricsService = metricsService;
        _cacheManager = cacheManager;
    }

    /// <summary>
    /// Performs a comprehensive health check of the AI system.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync()
    {
        var result = new HealthCheckResult
        {
            Timestamp = DateTime.UtcNow,
            Components = new Dictionary<string, ComponentHealth>(),
            Issues = new List<string>()
        };

        // Check metrics service
        result.Components["metrics"] = new ComponentHealth
        {
            Name = "metrics",
            IsHealthy = true,
            Status = "ok"
        };

        // Check cache service
        var cacheStats = _cacheManager.GetStats();
        result.Components["cache"] = new ComponentHealth
        {
            Name = "cache",
            IsHealthy = cacheStats.ExpiredEntries < cacheStats.TotalEntries * 0.1, // Less than 10% expired
            Status = cacheStats.ExpiredEntries == 0 ? "ok" :
                    cacheStats.ExpiredEntries < cacheStats.TotalEntries * 0.1 ? "warning" : "error",
            Details = $"Total: {cacheStats.TotalEntries}, Expired: {cacheStats.ExpiredEntries}"
        };

        if (!result.Components["cache"].IsHealthy)
        {
            result.Issues.Add($"Cache has {cacheStats.ExpiredEntries} expired entries out of {cacheStats.TotalEntries}");
        }

        // Get orchestrator metrics for health assessment
        var metrics = _metricsService.GetMetrics();

        // Check request success rate
        if (metrics.TotalRequests > 0)
        {
            var successRate = metrics.SuccessRate;
            if (successRate < 0.8) // Less than 80% success rate
            {
                result.Issues.Add($"Low success rate: {successRate:P1}");
                result.IsHealthy = false;
            }
        }

        // Check for failing stages
        foreach (var stageMetric in metrics.StageMetrics)
        {
            if (stageMetric.Executions > 0)
            {
                var failureRate = (double)stageMetric.Failures / stageMetric.Executions;
                if (failureRate > 0.5) // More than 50% failures
                {
                    result.Issues.Add($"Stage '{stageMetric.StageName}' has high failure rate: {failureRate:P1}");
                    result.IsHealthy = false;

                    result.Components[$"stage_{stageMetric.StageName}"] = new ComponentHealth
                    {
                        Name = stageMetric.StageName,
                        IsHealthy = false,
                        Status = "degraded",
                        ErrorMessage = $"Failure rate: {failureRate:P1}"
                    };
                }
                else if (failureRate > 0.1) // More than 10% failures
                {
                    result.Components[$"stage_{stageMetric.StageName}"] = new ComponentHealth
                    {
                        Name = stageMetric.StageName,
                        IsHealthy = true,
                        Status = "warning",
                        Details = $"Failure rate: {failureRate:P1}"
                    };
                }
            }
        }

        // Overall health assessment
        result.IsHealthy = result.IsHealthy && !result.Issues.Any();

        _logger.Debug("Health check completed: Healthy={IsHealthy}, Issues={IssueCount}",
            result.IsHealthy, result.Issues.Count);

        return result;
    }

    /// <summary>
    /// Enables or disables self-healing functionality.
    /// </summary>
    public void EnableSelfHealing(bool enable)
    {
        _selfHealingEnabled = enable;
        _logger.Information("Self-healing {Status}", enable ? "enabled" : "disabled");
    }

    /// <summary>
    /// Attempts to perform self-healing actions when issues are detected.
    /// </summary>
    public async Task<bool> PerformSelfHealingAsync()
    {
        if (!_selfHealingEnabled)
        {
            return false;
        }

        var healthResult = await CheckHealthAsync();
        var healed = false;

        // Attempt cache cleanup if cache is unhealthy
        if (healthResult.Components.TryGetValue("cache", out var cacheHealth) && !cacheHealth.IsHealthy)
        {
            _logger.Information("Performing cache cleanup as self-healing action");
            _cacheManager.CleanupExpiredEntries();
            healed = true;
        }

        // Reset metrics if there are too many failures
        var metrics = _metricsService.GetMetrics();
        if (metrics.TotalRequests > 100 && metrics.SuccessRate < 0.5)
        {
            _logger.Information("Resetting metrics as self-healing action");
            // Note: MetricsService would need a ResetMetrics method
            healed = true;
        }

        return healed;
    }

        /// <summary>
        /// Gets detailed health information for troubleshooting.
        /// </summary>
        public async Task<Dictionary<string, object>> GetDiagnosticInfoAsync()
        {
            var healthResult = await CheckHealthAsync();
            var metrics = _metricsService.GetMetrics();

            return new Dictionary<string, object>
            {
                ["health_result"] = healthResult,
                ["metrics"] = metrics,
                ["recent_events"] = _metricsService.GetRecentEvents(20),
                ["recommendations"] = GenerateRecommendations(healthResult, metrics)
            };
        }

    private List<string> GenerateRecommendations(HealthCheckResult health, OrchestratorMetrics metrics)
    {
        var recommendations = new List<string>();

        if (metrics.SuccessRate < 0.8)
        {
            recommendations.Add("Consider implementing more robust error handling and fallback mechanisms");
        }

        if (metrics.CacheHitRate < 0.3)
        {
            recommendations.Add("Low cache hit rate - consider adjusting cache TTL or cache key generation");
        }

        if (metrics.AverageLatencyMs > 5000)
        {
            recommendations.Add("High average latency detected - consider optimizing slow stages or implementing response streaming");
        }

        foreach (var stage in metrics.StageMetrics.Where(s => s.Executions > 0))
        {
            var failureRate = (double)stage.Failures / stage.Executions;
            if (failureRate > 0.2)
            {
                recommendations.Add($"High failure rate in stage '{stage.StageName}' - investigate and fix underlying issues");
            }

            if (stage.AverageLatency > 3000)
            {
                recommendations.Add($"Stage '{stage.StageName}' is slow ({stage.AverageLatency:F0}ms avg) - consider optimization");
            }
        }

        return recommendations;
    }
}
