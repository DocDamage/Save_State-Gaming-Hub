using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Core.Automation.Studio;

/// <summary>
/// Engine for executing automation workflows.
/// </summary>
public interface IWorkflowEngine
{
    /// <summary>
    /// Executes a workflow.
    /// </summary>
    Task<Result<WorkflowExecutionContext>> ExecuteAsync(Workflow workflow, WorkflowContext context, CancellationToken ct = default);

    /// <summary>
    /// Executes a single action.
    /// </summary>
    Task<Result<ActionResult>> ExecuteActionAsync(WorkflowAction action, WorkflowContext context, CancellationToken ct = default);

    /// <summary>
    /// Evaluates a condition.
    /// </summary>
    Task<Result<bool>> EvaluateConditionAsync(WorkflowCondition condition, WorkflowContext context, CancellationToken ct = default);

    /// <summary>
    /// Registers a trigger listener.
    /// </summary>
    Task<Result> RegisterTriggerAsync(TriggerType type, TriggerListener listener, CancellationToken ct = default);

    /// <summary>
    /// Unregisters a trigger listener.
    /// </summary>
    Task<Result> UnregisterTriggerAsync(TriggerType type, string listenerId, CancellationToken ct = default);

    /// <summary>
    /// Gets the currently executing workflows.
    /// </summary>
    Task<Result<IReadOnlyList<WorkflowExecutionContext>>> GetActiveExecutionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Cancels an executing workflow.
    /// </summary>
    Task<Result> CancelExecutionAsync(string executionId, CancellationToken ct = default);

    /// <summary>
    /// Registers a custom action handler.
    /// </summary>
    Task<Result> RegisterActionHandlerAsync(ActionType type, IActionHandler handler, CancellationToken ct = default);

    /// <summary>
    /// Unregisters a custom action handler.
    /// </summary>
    Task<Result> UnregisterActionHandlerAsync(ActionType type, CancellationToken ct = default);

    /// <summary>
    /// Gets execution statistics.
    /// </summary>
    Task<Result<ExecutionStatistics>> GetStatisticsAsync(CancellationToken ct = default);

    /// <summary>
    /// Event raised when a workflow starts execution.
    /// </summary>
    event EventHandler<WorkflowExecutionStartedEventArgs>? ExecutionStarted;

    /// <summary>
    /// Event raised when an action starts execution.
    /// </summary>
    event EventHandler<ActionExecutionStartedEventArgs>? ActionStarted;

    /// <summary>
    /// Event raised when an action completes execution.
    /// </summary>
    event EventHandler<ActionExecutionCompletedEventArgs>? ActionCompleted;
}

/// <summary>
/// Workflow execution context.
/// </summary>
public sealed record WorkflowExecutionContext(
    string ExecutionId,
    string WorkflowId,
    WorkflowStatus Status,
    WorkflowContext Context,
    IReadOnlyList<ActionResult> CompletedActions,
    WorkflowAction? CurrentAction,
    DateTime StartedAt,
    DateTime? CompletedAt = null,
    string? ErrorMessage = null);

/// <summary>
/// Workflow context containing variables and state.
/// </summary>
public sealed record WorkflowContext(
    TriggerType TriggerType,
    Dictionary<string, object> Variables,
    DateTime TriggeredAt,
    string? TriggeredBy = null);

/// <summary>
/// Action execution result.
/// </summary>
public sealed record ActionResult(
    string ActionId,
    ActionType Type,
    ActionStatus Status,
    TimeSpan Duration,
    Dictionary<string, object>? Output = null,
    string? ErrorMessage = null);

/// <summary>
/// Trigger listener registration.
/// </summary>
public sealed record TriggerListener(
    string Id,
    string WorkflowId,
    Dictionary<string, object>? Filter = null);

/// <summary>
/// Execution statistics.
/// </summary>
public sealed record ExecutionStatistics(
    int TotalExecutions,
    int SuccessfulExecutions,
    int FailedExecutions,
    int ActiveExecutions,
    double AverageExecutionTimeMs,
    IReadOnlyDictionary<ActionType, ActionStatistics> ActionStatistics);

/// <summary>
/// Per-action statistics.
/// </summary>
public sealed record ActionStatistics(
    ActionType Type,
    int ExecutionCount,
    int SuccessCount,
    int FailureCount,
    double AverageExecutionTimeMs);

/// <summary>
/// Workflow status.
/// </summary>
public enum WorkflowStatus
{
    Pending,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Action status.
/// </summary>
public enum ActionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped,
    Cancelled
}

/// <summary>
/// Action handler interface for custom actions.
/// </summary>
public interface IActionHandler
{
    /// <summary>
    /// Executes the action.
    /// </summary>
    Task<Result<Dictionary<string, object>>> ExecuteAsync(WorkflowAction action, WorkflowContext context, CancellationToken ct = default);

    /// <summary>
    /// Validates the action parameters.
    /// </summary>
    Task<Result<ValidationResult>> ValidateAsync(WorkflowAction action, CancellationToken ct = default);
}

/// <summary>
/// Validation result.
/// </summary>
public sealed record ValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors);

/// <summary>
/// Event args for workflow execution started events.
/// </summary>
public sealed class WorkflowExecutionStartedEventArgs : EventArgs
{
    public string ExecutionId { get; }
    public string WorkflowId { get; }
    public TriggerType TriggerType { get; }
    public DateTime StartedAt { get; }

    public WorkflowExecutionStartedEventArgs(string executionId, string workflowId, TriggerType triggerType)
        : this(executionId, workflowId, triggerType, SystemTimeProvider.Instance.UtcNow)
    {
    }

    public WorkflowExecutionStartedEventArgs(string executionId, string workflowId, TriggerType triggerType, DateTime startedAt)
    {
        ExecutionId = executionId;
        WorkflowId = workflowId;
        TriggerType = triggerType;
        StartedAt = startedAt;
    }
}

/// <summary>
/// Event args for action execution started events.
/// </summary>
public sealed class ActionExecutionStartedEventArgs : EventArgs
{
    public string ExecutionId { get; }
    public string ActionId { get; }
    public ActionType Type { get; }
    public DateTime StartedAt { get; }

    public ActionExecutionStartedEventArgs(string executionId, string actionId, ActionType type)
        : this(executionId, actionId, type, SystemTimeProvider.Instance.UtcNow)
    {
    }

    public ActionExecutionStartedEventArgs(string executionId, string actionId, ActionType type, DateTime startedAt)
    {
        ExecutionId = executionId;
        ActionId = actionId;
        Type = type;
        StartedAt = startedAt;
    }
}

/// <summary>
/// Event args for action execution completed events.
/// </summary>
public sealed class ActionExecutionCompletedEventArgs : EventArgs
{
    public string ExecutionId { get; }
    public string ActionId { get; }
    public ActionType Type { get; }
    public bool Success { get; }
    public TimeSpan Duration { get; }
    public DateTime CompletedAt { get; }

    public ActionExecutionCompletedEventArgs(string executionId, string actionId, ActionType type, bool success, TimeSpan duration)
        : this(executionId, actionId, type, success, duration, SystemTimeProvider.Instance.UtcNow)
    {
    }

    public ActionExecutionCompletedEventArgs(string executionId, string actionId, ActionType type, bool success, TimeSpan duration, DateTime completedAt)
    {
        ExecutionId = executionId;
        ActionId = actionId;
        Type = type;
        Success = success;
        Duration = duration;
        CompletedAt = completedAt;
    }
}
