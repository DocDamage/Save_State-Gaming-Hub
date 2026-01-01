using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Automation.Services;
using SaveState.Core.Automation.Services.DTOs;
using SaveState.Core.Common;

namespace SaveState.Application.Automation.Commands.Handlers;

/// <summary>
/// Command handlers for backup operations.
/// </summary>
public class ScheduleBackupCommandHandler : IRequestHandler<ScheduleBackupCommand, Result<BackupSchedule>>
{
    private readonly IBackupScheduler _backupScheduler;
    private readonly ILogger<ScheduleBackupCommandHandler> _logger;

    public ScheduleBackupCommandHandler(
        IBackupScheduler backupScheduler,
        ILogger<ScheduleBackupCommandHandler> logger)
    {
        _backupScheduler = backupScheduler;
        _logger = logger;
    }

    public async Task<Result<BackupSchedule>> Handle(ScheduleBackupCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _backupScheduler.CreateScheduleAsync(request.Config, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Scheduled backup: {Name}", result.Value!.Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule backup");
            return Result<BackupSchedule>.Failure($"Failed to schedule backup: {ex.Message}");
        }
    }
}

public class UpdateBackupScheduleCommandHandler : IRequestHandler<UpdateBackupScheduleCommand, Result>
{
    private readonly IBackupScheduler _backupScheduler;
    private readonly ILogger<UpdateBackupScheduleCommandHandler> _logger;

    public UpdateBackupScheduleCommandHandler(
        IBackupScheduler backupScheduler,
        ILogger<UpdateBackupScheduleCommandHandler> logger)
    {
        _backupScheduler = backupScheduler;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateBackupScheduleCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _backupScheduler.UpdateScheduleAsync(request.ScheduleId, request.Config, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Updated backup schedule: {Id}", request.ScheduleId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update backup schedule: {Id}", request.ScheduleId);
            return Result.Failure($"Failed to update schedule: {ex.Message}");
        }
    }
}

public class RemoveBackupScheduleCommandHandler : IRequestHandler<RemoveBackupScheduleCommand, Result>
{
    private readonly IBackupScheduler _backupScheduler;
    private readonly ILogger<RemoveBackupScheduleCommandHandler> _logger;

    public RemoveBackupScheduleCommandHandler(
        IBackupScheduler backupScheduler,
        ILogger<RemoveBackupScheduleCommandHandler> logger)
    {
        _backupScheduler = backupScheduler;
        _logger = logger;
    }

    public async Task<Result> Handle(RemoveBackupScheduleCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _backupScheduler.DeleteScheduleAsync(request.ScheduleId, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Removed backup schedule: {Id}", request.ScheduleId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove backup schedule: {Id}", request.ScheduleId);
            return Result.Failure($"Failed to remove schedule: {ex.Message}");
        }
    }
}

public class GetBackupSchedulesCommandHandler : IRequestHandler<GetBackupSchedulesCommand, Result<IReadOnlyList<BackupSchedule>>>
{
    private readonly IBackupScheduler _backupScheduler;
    private readonly ILogger<GetBackupSchedulesCommandHandler> _logger;

    public GetBackupSchedulesCommandHandler(
        IBackupScheduler backupScheduler,
        ILogger<GetBackupSchedulesCommandHandler> logger)
    {
        _backupScheduler = backupScheduler;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<BackupSchedule>>> Handle(GetBackupSchedulesCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _backupScheduler.GetAllSchedulesAsync(ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Retrieved {Count} backup schedules", result.Value!.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get backup schedules");
            return Result<IReadOnlyList<BackupSchedule>>.Failure($"Failed to get schedules: {ex.Message}");
        }
    }
}

public class GetBackupScheduleCommandHandler : IRequestHandler<GetBackupScheduleCommand, Result<BackupSchedule>>
{
    private readonly IBackupScheduler _backupScheduler;
    private readonly ILogger<GetBackupScheduleCommandHandler> _logger;

    public GetBackupScheduleCommandHandler(
        IBackupScheduler backupScheduler,
        ILogger<GetBackupScheduleCommandHandler> logger)
    {
        _backupScheduler = backupScheduler;
        _logger = logger;
    }

    public async Task<Result<BackupSchedule>> Handle(GetBackupScheduleCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _backupScheduler.GetScheduleAsync(request.ScheduleId, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Retrieved backup schedule: {Name}", result.Value!.Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get backup schedule: {Id}", request.ScheduleId);
            return Result<BackupSchedule>.Failure($"Failed to get schedule: {ex.Message}");
        }
    }
}

public class ExecuteBackupCommandHandler : IRequestHandler<ExecuteBackupCommand, Result<BackupResult>>
{
    private readonly IBackupScheduler _backupScheduler;
    private readonly ILogger<ExecuteBackupCommandHandler> _logger;

    public ExecuteBackupCommandHandler(
        IBackupScheduler backupScheduler,
        ILogger<ExecuteBackupCommandHandler> logger)
    {
        _backupScheduler = backupScheduler;
        _logger = logger;
    }

    public async Task<Result<BackupResult>> Handle(ExecuteBackupCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _backupScheduler.TriggerBackupAsync(request.ScheduleId, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                var backupResult = result.Value!;
                _logger.LogInformation("Backup executed: {Status}, Files: {Files}, Size: {Size} bytes",
                    backupResult.Status, backupResult.FilesBackedUp, backupResult.TotalSizeBytes);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute backup: {Id}", request.ScheduleId);
            return Result<BackupResult>.Failure($"Failed to execute backup: {ex.Message}");
        }
    }
}

public class GetBackupHistoryCommandHandler : IRequestHandler<GetBackupHistoryCommand, Result<IReadOnlyList<BackupResult>>>
{
    private readonly IBackupScheduler _backupScheduler;
    private readonly ILogger<GetBackupHistoryCommandHandler> _logger;

    public GetBackupHistoryCommandHandler(
        IBackupScheduler backupScheduler,
        ILogger<GetBackupHistoryCommandHandler> logger)
    {
        _backupScheduler = backupScheduler;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<BackupResult>>> Handle(GetBackupHistoryCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _backupScheduler.GetBackupHistoryAsync(request.ScheduleId, request.Since, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Retrieved {Count} backup history entries", result.Value!.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get backup history: {Id}", request.ScheduleId);
            return Result<IReadOnlyList<BackupResult>>.Failure($"Failed to get history: {ex.Message}");
        }
    }
}

public class SetBackupScheduleEnabledCommandHandler : IRequestHandler<SetBackupScheduleEnabledCommand, Result>
{
    private readonly IBackupScheduler _backupScheduler;
    private readonly ILogger<SetBackupScheduleEnabledCommandHandler> _logger;

    public SetBackupScheduleEnabledCommandHandler(
        IBackupScheduler backupScheduler,
        ILogger<SetBackupScheduleEnabledCommandHandler> logger)
    {
        _backupScheduler = backupScheduler;
        _logger = logger;
    }

    public async Task<Result> Handle(SetBackupScheduleEnabledCommand request, CancellationToken ct)
    {
        try
        {
            // Use EnableScheduleAsync or DisableScheduleAsync based on request
            var result = request.Enabled
                ? await _backupScheduler.EnableScheduleAsync(request.ScheduleId, ct).ConfigureAwait(false)
                : await _backupScheduler.DisableScheduleAsync(request.ScheduleId, ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Set backup schedule {Id} enabled: {Enabled}", request.ScheduleId, request.Enabled);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set backup schedule enabled: {Id}", request.ScheduleId);
            return Result.Failure($"Failed to set schedule enabled: {ex.Message}");
        }
    }
}

public class GetNextBackupExecutionTimeCommandHandler : IRequestHandler<GetNextBackupExecutionTimeCommand, Result<DateTime?>>
{
    private readonly IBackupScheduler _backupScheduler;
    private readonly ILogger<GetNextBackupExecutionTimeCommandHandler> _logger;

    public GetNextBackupExecutionTimeCommandHandler(
        IBackupScheduler backupScheduler,
        ILogger<GetNextBackupExecutionTimeCommandHandler> logger)
    {
        _backupScheduler = backupScheduler;
        _logger = logger;
    }

    public async Task<Result<DateTime?>> Handle(GetNextBackupExecutionTimeCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _backupScheduler.GetNextBackupTimeAsync(request.ScheduleId, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Next backup execution for {Id}: {Time}",
                    request.ScheduleId, result.Value?.ToString("yyyy-MM-dd HH:mm") ?? "None");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get next backup execution time: {Id}", request.ScheduleId);
            return Result<DateTime?>.Failure($"Failed to get next execution time: {ex.Message}");
        }
    }
}

public class ValidateBackupScheduleCommandHandler : IRequestHandler<ValidateBackupScheduleCommand, Result>
{
    private readonly IBackupScheduler _backupScheduler;
    private readonly ILogger<ValidateBackupScheduleCommandHandler> _logger;

    public ValidateBackupScheduleCommandHandler(
        IBackupScheduler backupScheduler,
        ILogger<ValidateBackupScheduleCommandHandler> logger)
    {
        _backupScheduler = backupScheduler;
        _logger = logger;
    }

    public async Task<Result> Handle(ValidateBackupScheduleCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _backupScheduler.ValidateScheduleAsync(request.Config, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Backup schedule configuration validated successfully");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate backup schedule");
            return Result.Failure($"Failed to validate schedule: {ex.Message}");
        }
    }
}
