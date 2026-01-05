namespace SaveState.Core.Automation.Services.DTOs;

/// <summary>
/// A scheduled task definition.
/// </summary>
public sealed record ScheduledTask(
    Guid Id,
    string Name,
    string Schedule,
    bool IsEnabled,
    DateTime CreatedAt,
    DateTime? LastRunTime = null,
    DateTime? NextRunTime = null,
    TaskStatus Status = TaskStatus.Pending);

/// <summary>
/// Status of a task.
/// </summary>
public enum TaskStatus
{
    Pending,
    Running,
    Success,
    Failed,
    Disabled
}

/// <summary>
/// Result of a task execution.
/// </summary>
public sealed record TaskExecutionResult(
    Guid TaskId,
    Guid ExecutionId,
    DateTime StartedAt,
    DateTime CompletedAt,
    bool Success,
    string? Output = null,
    string? ErrorMessage = null);
