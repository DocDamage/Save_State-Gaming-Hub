using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaveState.Core.Monitoring;

namespace SaveState.Infrastructure.Monitoring;

/// <summary>
/// Background service that hosts the performance monitor.
/// Ensures proper lifecycle management of performance monitoring.
/// </summary>
public class PerformanceMonitorBackgroundService : BackgroundService
{
    private readonly PerformanceMonitorService _performanceMonitor;
    private readonly ILogger<PerformanceMonitorBackgroundService> _logger;

    public PerformanceMonitorBackgroundService(
        PerformanceMonitorService performanceMonitor,
        ILogger<PerformanceMonitorBackgroundService> logger)
    {
        _performanceMonitor = performanceMonitor;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Performance monitor background service started");

        // The PerformanceMonitorService runs on its own timer,
        // so this background service just needs to stay alive
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Performance monitor background service stopping");
        _performanceMonitor.Dispose();
        return base.StopAsync(cancellationToken);
    }
}
