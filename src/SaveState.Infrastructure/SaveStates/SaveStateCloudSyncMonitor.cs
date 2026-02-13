using Microsoft.Extensions.Options;
using SaveState.Core.Configuration;
using SaveState.Core.Common.Services;
using SaveState.Core.SaveStates.Services;
using SaveState.Core.SaveStates.Services.DTOs;

namespace SaveState.Infrastructure.SaveStates;

/// <summary>
/// Thread-safe in-memory monitor for background save-state cloud sync daemon telemetry.
/// </summary>
public sealed class SaveStateCloudSyncMonitor : ISaveStateCloudSyncMonitor
{
    private readonly object _gate = new();
    private readonly ITimeProvider _timeProvider;
    private SaveStateCloudDaemonStatus _currentStatus;

    public SaveStateCloudSyncMonitor(
        ITimeProvider timeProvider,
        IOptions<CloudSyncOptions> options)
    {
        _timeProvider = timeProvider;
        _currentStatus = new SaveStateCloudDaemonStatus
        {
            Enabled = options.Value.SaveStateDaemon.Enabled,
            IsRunning = false,
            UpdatedAtUtc = _timeProvider.UtcNow,
            LastSyncAtUtc = null,
            LastGameId = null,
            SuccessfulSyncCount = 0,
            FailedSyncCount = 0,
            ConflictCount = 0,
            SkippedCount = 0,
            LastMessage = "Save-state cloud daemon is initializing."
        };
    }

    /// <inheritdoc />
    public SaveStateCloudDaemonStatus CurrentStatus
    {
        get
        {
            lock (_gate)
            {
                return _currentStatus;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<SaveStateCloudDaemonStatus>? StatusChanged;

    public void SetEnabled(bool enabled, string message)
    {
        Update(status => status with
        {
            Enabled = enabled,
            UpdatedAtUtc = _timeProvider.UtcNow,
            LastMessage = message
        });
    }

    public void SetRunning(bool isRunning, string message)
    {
        Update(status => status with
        {
            IsRunning = isRunning,
            UpdatedAtUtc = _timeProvider.UtcNow,
            LastMessage = message
        });
    }

    public void RecordSyncSuccess(Guid gameId, string message)
    {
        Update(status => status with
        {
            LastGameId = gameId,
            LastSyncAtUtc = _timeProvider.UtcNow,
            UpdatedAtUtc = _timeProvider.UtcNow,
            SuccessfulSyncCount = status.SuccessfulSyncCount + 1,
            LastMessage = message
        });
    }

    public void RecordSyncFailure(Guid? gameId, string message)
    {
        Update(status => status with
        {
            LastGameId = gameId,
            UpdatedAtUtc = _timeProvider.UtcNow,
            FailedSyncCount = status.FailedSyncCount + 1,
            LastMessage = message
        });
    }

    public void RecordConflict(Guid gameId, SaveStateConflictType conflictType, string message)
    {
        Update(status => status with
        {
            LastGameId = gameId,
            LastSyncAtUtc = _timeProvider.UtcNow,
            UpdatedAtUtc = _timeProvider.UtcNow,
            ConflictCount = status.ConflictCount + 1,
            LastMessage = $"{message} (Conflict: {conflictType})"
        });
    }

    public void RecordSkipped(Guid gameId, string message)
    {
        Update(status => status with
        {
            LastGameId = gameId,
            UpdatedAtUtc = _timeProvider.UtcNow,
            SkippedCount = status.SkippedCount + 1,
            LastMessage = message
        });
    }

    public void RecordHeartbeat(string message)
    {
        Update(status => status with
        {
            UpdatedAtUtc = _timeProvider.UtcNow,
            LastMessage = message
        });
    }

    private void Update(Func<SaveStateCloudDaemonStatus, SaveStateCloudDaemonStatus> update)
    {
        SaveStateCloudDaemonStatus snapshot;
        lock (_gate)
        {
            _currentStatus = update(_currentStatus);
            snapshot = _currentStatus;
        }

        StatusChanged?.Invoke(this, snapshot);
    }
}
