using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SaveState.Infrastructure.HealthChecks;

/// <summary>
/// Health check for memory usage.
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    private readonly ILogger<MemoryHealthCheck> _logger;
    private readonly int _maxMemoryUsagePercent;

    public MemoryHealthCheck(ILogger<MemoryHealthCheck> logger, int maxMemoryUsagePercent = 90)
    {
        _logger = logger;
        _maxMemoryUsagePercent = maxMemoryUsagePercent;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var workingSetBytes = process.WorkingSet64;
            var gcTotalMemory = GC.GetTotalMemory(false);
            
            // Get total system memory (approximation for container environments)
            var totalMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            var usedPercentage = totalMemoryBytes > 0 
                ? (double)workingSetBytes / totalMemoryBytes * 100 
                : 0;

            var data = new Dictionary<string, object>
            {
                ["WorkingSetMB"] = Math.Round(workingSetBytes / (1024.0 * 1024), 2),
                ["GCTotalMemoryMB"] = Math.Round(gcTotalMemory / (1024.0 * 1024), 2),
                ["TotalAvailableMB"] = Math.Round(totalMemoryBytes / (1024.0 * 1024), 2),
                ["UsedPercentage"] = Math.Round(usedPercentage, 2),
                ["Gen0Collections"] = GC.CollectionCount(0),
                ["Gen1Collections"] = GC.CollectionCount(1),
                ["Gen2Collections"] = GC.CollectionCount(2)
            };

            if (usedPercentage > _maxMemoryUsagePercent)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"High memory usage: {data["UsedPercentage"]}%",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Memory usage normal: {data["UsedPercentage"]}%",
                data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Memory check failed", ex));
        }
    }
}
