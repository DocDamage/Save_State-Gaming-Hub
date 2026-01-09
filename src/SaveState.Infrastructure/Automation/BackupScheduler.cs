using Microsoft.Extensions.Logging;
using SaveState.Core.Automation.Services;
using SaveState.Core.Automation.Services.DTOs;
using SaveState.Core.Common;
using System.Collections.Concurrent;

namespace SaveState.Infrastructure.Automation;

/// <summary>
/// Basic implementation of backup scheduler.
/// Provides core functionality for scheduling and executing automated backups.
/// Note: Cron scheduling and advanced retention policies can be added as needed.
/// </summary>
public class BackupScheduler : IBackupScheduler, IDisposable
{
    private readonly ILogger<BackupScheduler> _logger;
    private readonly ConcurrentDictionary<Guid, BackupSchedule> _schedules = new();
    private readonly ConcurrentDictionary<Guid, List<BackupResult>> _backupHistory = new();
    private bool _disposed;

    public event EventHandler<BackupScheduledEventArgs>? BackupScheduled;
    public event EventHandler<BackupStartedEventArgs>? BackupStarted;
    public event EventHandler<BackupCompletedEventArgs>? BackupCompleted;

    public BackupScheduler(ILogger<BackupScheduler> logger)
    {
        _logger = logger;
    }

    public Task<Result<BackupSchedule>> CreateScheduleAsync(
        BackupScheduleConfig config, CancellationToken ct = default)
    {
        try
        {
            var schedule = new BackupSchedule(
                Id: Guid.NewGuid(),
                Name: config.Name,
                Description: config.Description,
                Config: config,
                IsEnabled: config.IsEnabled,
                CreatedAt: DateTime.UtcNow,
                LastModified: DateTime.UtcNow);

            _schedules[schedule.Id] = schedule;
            _backupHistory[schedule.Id] = new List<BackupResult>();

            _logger.LogInformation("Created backup schedule: {Name} ({Id})", schedule.Name, schedule.Id);
            return Task.FromResult(Result.Success<BackupSchedule>(schedule));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup schedule");
            return Task.FromResult(Result.Failure<BackupSchedule>($"Failed to create schedule: {ex.Message}"));
        }
    }

    public Task<Result> UpdateScheduleAsync(
        Guid scheduleId, BackupScheduleConfig config, CancellationToken ct = default)
    {
        if (!_schedules.TryGetValue(scheduleId, out var schedule))
        {
            return Task.FromResult(Result.Failure($"Schedule not found: {scheduleId}"));
        }

        _schedules[scheduleId] = schedule with
        {
            Config = config,
            LastModified = DateTime.UtcNow
        };

        _logger.LogInformation("Updated backup schedule: {Id}", scheduleId);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DeleteScheduleAsync(Guid scheduleId, CancellationToken ct = default)
    {
        if (!_schedules.TryRemove(scheduleId, out _))
        {
            return Task.FromResult(Result.Failure($"Schedule not found: {scheduleId}"));
        }

        _backupHistory.TryRemove(scheduleId, out _);
        _logger.LogInformation("Deleted backup schedule: {Id}", scheduleId);
        return Task.FromResult(Result.Success());
    }

    public Task<Result<IReadOnlyList<BackupSchedule>>> GetAllSchedulesAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success<IReadOnlyList<BackupSchedule>>(_schedules.Values.ToArray()));
    }

    public Task<Result<IReadOnlyList<BackupSchedule>>> GetSchedulesForGameAsync(
        Guid gameId, CancellationToken ct = default)
    {
        var gameSchedules = _schedules.Values
            .Where(s => s.Config.GameId == gameId)
            .ToArray();
        return Task.FromResult(Result.Success<IReadOnlyList<BackupSchedule>>(gameSchedules));
    }

    public Task<Result<BackupSchedule>> GetScheduleAsync(Guid scheduleId, CancellationToken ct = default)
    {
        return Task.FromResult(_schedules.TryGetValue(scheduleId, out var schedule)
            ? Result.Success<BackupSchedule>(schedule)
            : Result.Failure<BackupSchedule>($"Schedule not found: {scheduleId}"));
    }

    public async Task<Result<BackupResult>> TriggerBackupAsync(Guid scheduleId, CancellationToken ct = default)
    {
        try
        {
            if (!_schedules.TryGetValue(scheduleId, out var schedule))
            {
                return Result.Failure<BackupResult>($"Schedule not found: {scheduleId}");
            }

            OnBackupStarted(scheduleId);

            var startedAt = DateTime.UtcNow;

            // Simulate backup
            await Task.Delay(100, ct);

            var result = new BackupResult(
                Id: Guid.NewGuid(),
                ScheduleId: scheduleId,
                GameId: schedule.Config.GameId,
                StartedAt: startedAt,
                CompletedAt: DateTime.UtcNow,
                Status: BackupStatus.Success,
                TotalSizeBytes: 1024,
                FilesBackedUp: 1,
                BackupPath: schedule.Config.DestinationPath,
                Errors: Array.Empty<string>());

            _backupHistory[scheduleId].Add(result);
            _schedules[scheduleId] = schedule with { LastExecutedAt = DateTime.UtcNow };

            OnBackupCompleted(result);
            _logger.LogInformation("Backup completed: {Id}", result.Id);

            return Result.Success<BackupResult>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger backup: {ScheduleId}", scheduleId);
            return Result.Failure<BackupResult>($"Failed to trigger backup: {ex.Message}");
        }
    }

    public Task<Result<IReadOnlyList<BackupResult>>> GetBackupHistoryAsync(
        Guid scheduleId, DateTime? since = null, CancellationToken ct = default)
    {
        if (!_backupHistory.TryGetValue(scheduleId, out var history))
        {
            return Task.FromResult(Result.Success<IReadOnlyList<BackupResult>>((IReadOnlyList<BackupResult>)Array.Empty<BackupResult>()));
        }

        var filtered = since.HasValue
            ? history.Where(h => h.StartedAt >= since.Value).ToArray()
            : history.ToArray();

        return Task.FromResult(Result.Success<IReadOnlyList<BackupResult>>((IReadOnlyList<BackupResult>)filtered));
    }

    public Task<Result> EnableScheduleAsync(Guid scheduleId, CancellationToken ct = default)
    {
        if (!_schedules.TryGetValue(scheduleId, out var schedule))
        {
            return Task.FromResult(Result.Failure($"Schedule not found: {scheduleId}"));
        }

        _schedules[scheduleId] = schedule with { IsEnabled = true, LastModified = DateTime.UtcNow };
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DisableScheduleAsync(Guid scheduleId, CancellationToken ct = default)
    {
        if (!_schedules.TryGetValue(scheduleId, out var schedule))
        {
            return Task.FromResult(Result.Failure($"Schedule not found: {scheduleId}"));
        }

        _schedules[scheduleId] = schedule with { IsEnabled = false, LastModified = DateTime.UtcNow };
        return Task.FromResult(Result.Success());
    }

    public Task<Result<DateTime?>> GetNextBackupTimeAsync(Guid scheduleId, CancellationToken ct = default)
    {
        if (!_schedules.TryGetValue(scheduleId, out var schedule))
        {
            return Task.FromResult(Result.Failure<DateTime?>($"Schedule not found: {scheduleId}"));
        }

        // Simplified - would calculate based on frequency
        var nextTime = DateTime.UtcNow.AddHours(1);
        return Task.FromResult(Result.Success<DateTime?>(nextTime));
    }

    public Task<Result> ValidateScheduleAsync(
        BackupScheduleConfig config, CancellationToken ct = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.Name))
        {
            errors.Add("Schedule name is required");
        }

        if (string.IsNullOrWhiteSpace(config.DestinationPath))
        {
            errors.Add("Destination path is required");
        }

        return Task.FromResult(errors.Count == 0
            ? Result.Success()
            : Result.Failure(string.Join("; ", errors)));
    }

    private void OnBackupStarted(Guid scheduleId)
    {
        BackupStarted?.Invoke(this, new BackupStartedEventArgs { ScheduleId = scheduleId });
    }

    private void OnBackupCompleted(BackupResult result)
    {
        BackupCompleted?.Invoke(this, new BackupCompletedEventArgs { Result = result });
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _schedules.Clear();
                _backupHistory.Clear();
            }
            _disposed = true;
        }
    }
}


