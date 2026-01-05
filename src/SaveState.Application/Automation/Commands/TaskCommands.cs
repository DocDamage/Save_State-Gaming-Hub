using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Automation.Services.DTOs;

namespace SaveState.Application.Automation.Commands;

/// <summary>
/// Commands for scheduled task operations.
/// </summary>
public record CreateTaskCommand(
    string Name,
    string Schedule,
    bool Enabled) : IRequest<Result<ScheduledTask>>;

public record UpdateTaskCommand(
    Guid TaskId,
    string Name,
    string Schedule,
    bool Enabled) : IRequest<Result>;

public record DeleteTaskCommand(
    Guid TaskId) : IRequest<Result>;

public record GetTasksCommand : IRequest<Result<IReadOnlyList<ScheduledTask>>>;

public record GetTaskCommand(
    Guid TaskId) : IRequest<Result<ScheduledTask>>;

public record ToggleTaskCommand(
    Guid TaskId,
    bool Enabled) : IRequest<Result>;

public record ExecuteTaskNowCommand(
    Guid TaskId) : IRequest<Result<TaskExecutionResult>>;

public record GetTaskExecutionHistoryCommand(
    Guid TaskId) : IRequest<Result<IReadOnlyList<TaskExecutionResult>>>;
