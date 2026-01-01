using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Automation.Services.DTOs;

namespace SaveState.Application.Automation.Commands;

/// <summary>
/// Commands for backup operations.
/// </summary>
public record ScheduleBackupCommand(
    BackupScheduleConfig Config) : IRequest<Result<BackupSchedule>>;

public record UpdateBackupScheduleCommand(
    Guid ScheduleId,
    BackupScheduleConfig Config) : IRequest<Result>;

public record RemoveBackupScheduleCommand(
    Guid ScheduleId) : IRequest<Result>;

public record GetBackupSchedulesCommand : IRequest<Result<IReadOnlyList<BackupSchedule>>>;

public record GetBackupScheduleCommand(
    Guid ScheduleId) : IRequest<Result<BackupSchedule>>;

public record ExecuteBackupCommand(
    Guid ScheduleId) : IRequest<Result<BackupResult>>;

public record GetBackupHistoryCommand(
    Guid ScheduleId,
    DateTime? Since = null) : IRequest<Result<IReadOnlyList<BackupResult>>>;

public record SetBackupScheduleEnabledCommand(
    Guid ScheduleId,
    bool Enabled) : IRequest<Result>;

public record GetNextBackupExecutionTimeCommand(
    Guid ScheduleId) : IRequest<Result<DateTime?>>;

public record ValidateBackupScheduleCommand(
    BackupScheduleConfig Config) : IRequest<Result>;