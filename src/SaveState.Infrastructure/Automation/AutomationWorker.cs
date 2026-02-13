using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaveState.Core.Automation.Services;
using SaveState.Core.Automation.Services.DTOs;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.Sync;
using Microsoft.Extensions.Options;
using SaveState.Core.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Infrastructure.Automation;

/// <summary>
/// Background worker that monitors game sessions and triggers automated backups and syncs.
/// </summary>
public class AutomationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutomationWorker> _logger;
    private readonly ISessionTrackingService _sessionTracking;
    private readonly CloudSyncOptions _options;

    public AutomationWorker(
        IServiceProvider serviceProvider,
        ILogger<AutomationWorker> logger,
        IOptions<CloudSyncOptions> options,
        ISessionTrackingService sessionTracking)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
        _sessionTracking = sessionTracking;

        _sessionTracking.SessionEnded += OnSessionEnded;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Automation worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTimeBasedSchedulesAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing time-based schedules");
            }

            // Check every 5 minutes
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken).ConfigureAwait(false);
        }
    }

    private void OnSessionEnded(object? sender, GameSessionEventArgs e)
    {
        _ = OnSessionEndedAsync(e);
    }

    private async Task OnSessionEndedAsync(GameSessionEventArgs e)
    {
        _logger.LogInformation("Game session ended for {GameId}, checking for 'AfterGameExit' schedules...", e.GameId);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<IBackupScheduler>();
            var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();

            var schedules = await scheduler.GetSchedulesForGameAsync(e.GameId).ConfigureAwait(false);
            if (!schedules.IsSuccess) return;

            foreach (var schedule in schedules.Value.Where(s => s.IsEnabled && s.Config.Frequency == BackupFrequency.AfterGameExit))
            {
                _logger.LogInformation("Triggering automated backup for schedule: {Name}", schedule.Name);
                var result = await scheduler.TriggerBackupAsync(schedule.Id).ConfigureAwait(false);

                if (result.IsSuccess && _options.AutoSyncOnExit)
                {
                    _logger.LogInformation("Automated backup successful, triggering cloud sync...");
                    await syncService.PushAsync().ConfigureAwait(false);
                }
                else if (result.IsSuccess)
                {
                    _logger.LogInformation("Automated backup successful. Cloud sync skipped (AutoSyncOnExit is disabled).");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process automated backup after game exit");
        }
    }

    private async Task ProcessTimeBasedSchedulesAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<IBackupScheduler>();

        var allSchedules = await scheduler.GetAllSchedulesAsync(ct).ConfigureAwait(false);
        if (!allSchedules.IsSuccess) return;

        var now = DateTime.UtcNow;

        foreach (var schedule in allSchedules.Value.Where(s => s.IsEnabled))
        {
            if (ShouldRunNow(schedule, now))
            {
                _logger.LogInformation("Triggering scheduled backup: {Name}", schedule.Name);
                await scheduler.TriggerBackupAsync(schedule.Id, ct).ConfigureAwait(false);

                // For scheduled backups, we might also want to push to cloud
                var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();
                await syncService.PushAsync(ct).ConfigureAwait(false);
            }
        }
    }

    private bool ShouldRunNow(BackupSchedule schedule, DateTime now)
    {
        if (schedule.LastExecutedAt.HasValue && (now - schedule.LastExecutedAt.Value).TotalHours < 1)
            return false; // Don't run more than once per hour for any schedule

        switch (schedule.Config.Frequency)
        {
            case BackupFrequency.Daily:
                return !schedule.LastExecutedAt.HasValue || schedule.LastExecutedAt.Value.Date < now.Date;
            case BackupFrequency.Weekly:
                return !schedule.LastExecutedAt.HasValue || (now - schedule.LastExecutedAt.Value).TotalDays >= 7;
            case BackupFrequency.Monthly:
                return !schedule.LastExecutedAt.HasValue || (now - schedule.LastExecutedAt.Value).TotalDays >= 30;
            default:
                return false;
        }
    }

    public override void Dispose()
    {
        _sessionTracking.SessionEnded -= OnSessionEnded;
        base.Dispose();
    }
}
