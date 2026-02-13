using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Configuration;

namespace SaveState.Infrastructure.SaveStates;

/// <summary>
/// Hosted daemon that periodically syncs recent save states to cloud storage.
/// </summary>
public sealed class SaveStateCloudSyncBackgroundService : BackgroundService
{
    private readonly SaveStateCloudSyncDaemonProcessor _processor;
    private readonly SaveStateCloudSyncMonitor _monitor;
    private readonly IOptionsMonitor<CloudSyncOptions> _optionsMonitor;
    private readonly ILogger<SaveStateCloudSyncBackgroundService> _logger;

    public SaveStateCloudSyncBackgroundService(
        SaveStateCloudSyncDaemonProcessor processor,
        SaveStateCloudSyncMonitor monitor,
        IOptionsMonitor<CloudSyncOptions> optionsMonitor,
        ILogger<SaveStateCloudSyncBackgroundService> logger)
    {
        _processor = processor;
        _monitor = monitor;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _monitor.SetRunning(true, "Save-state cloud daemon started.");
        _logger.LogInformation("Save-state cloud daemon started.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _processor.ProcessCycleAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Save-state cloud daemon cycle failed.");
                    _monitor.RecordSyncFailure(null, $"Daemon cycle failed: {ex.Message}");
                }

                var interval = TimeSpan.FromSeconds(Math.Max(
                    15,
                    _optionsMonitor.CurrentValue.SaveStateDaemon.IntervalSeconds));

                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _monitor.SetRunning(false, "Save-state cloud daemon stopped.");
            _logger.LogInformation("Save-state cloud daemon stopped.");
        }
    }
}
