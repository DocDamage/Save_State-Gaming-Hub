using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace SaveState.Infrastructure.HealthChecks;

/// <summary>
/// Health check for available disk space.
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly ILogger<DiskSpaceHealthCheck> _logger;
    private readonly long _minimumFreeBytes;

    public DiskSpaceHealthCheck(ILogger<DiskSpaceHealthCheck> logger, long minimumFreeBytes = 1_000_000_000) // 1GB default
    {
        _logger = logger;
        _minimumFreeBytes = minimumFreeBytes;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var driveInfo = new System.IO.DriveInfo(System.IO.Directory.GetCurrentDirectory());
            var freeBytes = driveInfo.AvailableFreeSpace;
            var totalBytes = driveInfo.TotalSize;
            var usedBytes = totalBytes - freeBytes;
            var usedPercentage = (double)usedBytes / totalBytes * 100;

            var data = new Dictionary<string, object>
            {
                ["FreeSpaceBytes"] = freeBytes,
                ["FreeSpaceGB"] = Math.Round(freeBytes / (1024.0 * 1024 * 1024), 2),
                ["TotalSpaceGB"] = Math.Round(totalBytes / (1024.0 * 1024 * 1024), 2),
                ["UsedPercentage"] = Math.Round(usedPercentage, 2)
            };

            if (freeBytes < _minimumFreeBytes)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Low disk space: {data["FreeSpaceGB"]} GB remaining",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Disk space healthy: {data["FreeSpaceGB"]} GB free",
                data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disk space health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Disk space check failed", ex));
        }
    }
}
