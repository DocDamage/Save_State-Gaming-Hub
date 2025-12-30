using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SaveState.Core.Monitoring;

namespace SaveState.Infrastructure.Monitoring;

/// <summary>
/// Service for monitoring system performance metrics.
/// Provides real-time CPU, memory, and system resource monitoring.
/// </summary>
public class PerformanceMonitorService : IDisposable
{
    private readonly IApplicationMetrics _metrics;
    private readonly ILogger<PerformanceMonitorService> _logger;
    private readonly Timer _performanceTimer;
    private readonly Process _currentProcess;
    private bool _disposed;

    public PerformanceMonitorService(
        IApplicationMetrics metrics,
        ILogger<PerformanceMonitorService> logger)
    {
        _metrics = metrics;
        _logger = logger;
        _currentProcess = Process.GetCurrentProcess();

        // Collect performance metrics every 30 seconds
        _performanceTimer = new Timer(CollectPerformanceMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

        _logger.LogInformation("Performance monitor service initialized");
    }

    private void CollectPerformanceMetrics(object? state)
    {
        try
        {
            if (_disposed)
                return;

            // Collect memory usage
            var memoryUsage = _currentProcess.WorkingSet64;
            _metrics.RecordMemoryUsage(memoryUsage);

            // Collect CPU usage (simplified - in production you'd want more accurate measurement)
            var cpuUsage = GetCpuUsage();
            if (cpuUsage >= 0)
            {
                _metrics.RecordCpuUsage(cpuUsage);
            }

            // Collect database connection count (this would be injected from a connection pool monitor)
            // For now, we'll track this separately when database operations occur

            _logger.LogDebug("Collected performance metrics: Memory={Memory}MB, CPU={Cpu}%",
                memoryUsage / 1024 / 1024, cpuUsage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect performance metrics");
        }
    }

    private double GetCpuUsage()
    {
        try
        {
            // On Windows, we can use PerformanceCounter for more accurate CPU measurement
            if (OperatingSystem.IsWindows())
            {
                return GetWindowsCpuUsage();
            }

            // On other platforms, use a simpler approach
            _currentProcess.Refresh();
            var cpuTime = _currentProcess.TotalProcessorTime.TotalMilliseconds;
            var uptime = Environment.TickCount64;

            if (uptime > 0)
            {
                // Simple CPU usage estimation
                return Math.Min(100.0, (cpuTime / uptime) * 100.0 / Environment.ProcessorCount);
            }

            return -1;
        }
        catch
        {
            return -1;
        }
    }

    private static double GetWindowsCpuUsage()
    {
        try
        {
            // Use Process.TotalProcessorTime for CPU usage estimation
            // This is a simple approximation, not as accurate as PerformanceCounter
            var process = Process.GetCurrentProcess();
            var totalProcessorTime = process.TotalProcessorTime.TotalMilliseconds;
            var uptime = Environment.TickCount64;

            if (uptime > 0)
            {
                // Calculate CPU usage as percentage
                var cpuUsage = (totalProcessorTime / uptime) * 100.0 / Environment.ProcessorCount;
                return Math.Min(100.0, Math.Max(0.0, cpuUsage));
            }

            return 0.0;
        }
        catch
        {
            // Fallback to simple measurement
            return 0.0;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _performanceTimer?.Dispose();
        _currentProcess?.Dispose();

        _logger.LogInformation("Performance monitor service disposed");
    }
}
