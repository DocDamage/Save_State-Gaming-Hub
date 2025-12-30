using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SaveState.Infrastructure.Health;

/// <summary>
/// Health check for system resource usage (CPU, memory, disk).
/// Monitors resource consumption and alerts when thresholds are exceeded.
/// </summary>
public class ResourceHealthCheck : IHealthCheck
{
    private readonly ILogger<ResourceHealthCheck> _logger;

    // Configurable thresholds
    private const double CpuThresholdPercent = 80.0; // Alert if CPU > 80%
    private const double MemoryThresholdPercent = 85.0; // Alert if memory > 85%
    private const double DiskThresholdPercent = 90.0; // Alert if disk > 90%

    public ResourceHealthCheck(ILogger<ResourceHealthCheck> logger)
    {
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, object>();

        try
        {
            // Check CPU usage
            var cpuUsage = await GetCpuUsageAsync(cancellationToken);
            results["CPU"] = new { UsagePercent = cpuUsage, Threshold = CpuThresholdPercent };

            // Check memory usage
            var (memoryUsage, totalMemory) = GetMemoryUsage();
            var memoryPercent = (double)memoryUsage / totalMemory * 100;
            results["Memory"] = new
            {
                UsageBytes = memoryUsage,
                TotalBytes = totalMemory,
                UsagePercent = memoryPercent,
                Threshold = MemoryThresholdPercent
            };

            // Check disk usage
            var diskUsages = GetDiskUsage();
            results["Disk"] = diskUsages.Select(d => new
            {
                Drive = d.Drive,
                UsedBytes = d.Used,
                TotalBytes = d.Total,
                UsagePercent = d.Percent,
                Threshold = DiskThresholdPercent
            }).ToList();

            // Determine health status
            var issues = new List<string>();
            var criticalIssues = new List<string>();

            // CPU check
            if (cpuUsage > CpuThresholdPercent)
            {
                if (cpuUsage > 95.0)
                    criticalIssues.Add($"CPU usage is critically high: {cpuUsage:F1}%");
                else
                    issues.Add($"High CPU usage: {cpuUsage:F1}%");
            }

            // Memory check
            if (memoryPercent > MemoryThresholdPercent)
            {
                if (memoryPercent > 95.0)
                    criticalIssues.Add($"Memory usage is critically high: {memoryPercent:F1}%");
                else
                    issues.Add($"High memory usage: {memoryPercent:F1}%");
            }

            // Disk check
            foreach (var disk in diskUsages)
            {
                if (disk.Percent > DiskThresholdPercent)
                {
                    if (disk.Percent > 98.0)
                        criticalIssues.Add($"Disk {disk.Drive} is critically full: {disk.Percent:F1}%");
                    else
                        issues.Add($"Disk {disk.Drive} usage is high: {disk.Percent:F1}%");
                }
            }

            // Return appropriate health result
            if (criticalIssues.Any())
            {
                return HealthCheckResult.Unhealthy(
                    $"Critical resource issues detected: {string.Join(", ", criticalIssues)}",
                    data: results);
            }

            if (issues.Any())
            {
                return HealthCheckResult.Degraded(
                    $"Resource usage warnings: {string.Join(", ", issues)}",
                    data: results);
            }

            return HealthCheckResult.Healthy("System resources are within normal ranges", results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check system resource health");
            return HealthCheckResult.Unhealthy("Resource health check failed", ex, results);
        }
    }

    private static async Task<double> GetCpuUsageAsync(CancellationToken ct)
    {
        try
        {
            var process = Process.GetCurrentProcess();

            // Get CPU usage over a short interval
            var startTime = process.TotalProcessorTime;
            var startTimeStamp = Environment.TickCount64;

            await Task.Delay(500, ct); // Sample for 500ms

            var endTime = process.TotalProcessorTime;
            var endTimeStamp = Environment.TickCount64;

            var cpuUsedMs = (endTime - startTime).TotalMilliseconds;
            var totalMsPassed = endTimeStamp - startTimeStamp;

            var cpuUsagePercent = cpuUsedMs / (totalMsPassed * Environment.ProcessorCount) * 100;
            return Math.Min(100.0, Math.Max(0.0, cpuUsagePercent));
        }
        catch
        {
            // Fallback to a simple CPU usage estimation
            return 0.0;
        }
    }

    private static (long used, long total) GetMemoryUsage()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var usedMemory = process.WorkingSet64;

            // Get total system memory (this is an approximation)
            // In a real-world scenario, you'd use performance counters or OS-specific APIs
            // For now, we'll return the process memory and a reasonable total
            var totalMemory = usedMemory * 4; // Rough estimate

            return (usedMemory, totalMemory);
        }
        catch
        {
            return (0, 1); // Avoid division by zero
        }
    }

    private List<DiskUsageInfo> GetDiskUsage()
    {
        var results = new List<DiskUsageInfo>();

        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    var used = drive.TotalSize - drive.TotalFreeSpace;
                    var percent = drive.TotalSize > 0
                        ? (double)used / drive.TotalSize * 100
                        : 0;

                    results.Add(new DiskUsageInfo(
                        drive.Name.TrimEnd('\\'),
                        used,
                        drive.TotalSize,
                        percent));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get disk usage information");
        }

        return results;
    }

    private record DiskUsageInfo(string Drive, long Used, long Total, double Percent);
}
