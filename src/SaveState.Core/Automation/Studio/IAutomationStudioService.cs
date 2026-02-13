using SaveState.Core.Common;

namespace SaveState.Core.Automation.Studio;

/// <summary>
/// Service for managing automation workflows in the Automation Studio.
/// </summary>
public interface IAutomationStudioService
{
    /// <summary>
    /// Creates a new workflow.
    /// </summary>
    Task<Result<Workflow>> CreateWorkflowAsync(CreateWorkflowRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets a workflow by ID.
    /// </summary>
    Task<Result<Workflow>> GetWorkflowAsync(string workflowId, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing workflow.
    /// </summary>
    Task<Result<Workflow>> UpdateWorkflowAsync(string workflowId, UpdateWorkflowRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a workflow.
    /// </summary>
    Task<Result> DeleteWorkflowAsync(string workflowId, CancellationToken ct = default);

    /// <summary>
    /// Lists all workflows.
    /// </summary>
    Task<Result<IReadOnlyList<WorkflowSummary>>> ListWorkflowsAsync(WorkflowFilter? filter = null, CancellationToken ct = default);

    /// <summary>
    /// Activates a workflow (starts listening for triggers).
    /// </summary>
    Task<Result> ActivateWorkflowAsync(string workflowId, CancellationToken ct = default);

    /// <summary>
    /// Deactivates a workflow (stops listening for triggers).
    /// </summary>
    Task<Result> DeactivateWorkflowAsync(string workflowId, CancellationToken ct = default);

    /// <summary>
    /// Manually triggers a workflow.
    /// </summary>
    Task<Result<WorkflowExecutionResult>> TriggerWorkflowAsync(string workflowId, Dictionary<string, object>? context = null, CancellationToken ct = default);

    /// <summary>
    /// Gets the execution history for a workflow.
    /// </summary>
    Task<Result<IReadOnlyList<WorkflowExecution>>> GetExecutionHistoryAsync(string workflowId, int limit = 50, CancellationToken ct = default);

    /// <summary>
    /// Gets available trigger types.
    /// </summary>
    Task<Result<IReadOnlyList<TriggerTypeDefinition>>> GetAvailableTriggersAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets available action types.
    /// </summary>
    Task<Result<IReadOnlyList<ActionTypeDefinition>>> GetAvailableActionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Validates a workflow configuration.
    /// </summary>
    Task<Result<WorkflowValidationResult>> ValidateWorkflowAsync(Workflow workflow, CancellationToken ct = default);

    /// <summary>
    /// Duplicates an existing workflow.
    /// </summary>
    Task<Result<Workflow>> DuplicateWorkflowAsync(string workflowId, string? newName = null, CancellationToken ct = default);

    /// <summary>
    /// Imports a workflow from JSON.
    /// </summary>
    Task<Result<Workflow>> ImportWorkflowAsync(string json, CancellationToken ct = default);

    /// <summary>
    /// Exports a workflow to JSON.
    /// </summary>
    Task<Result<string>> ExportWorkflowAsync(string workflowId, CancellationToken ct = default);

    /// <summary>
    /// Event raised when a workflow is triggered.
    /// </summary>
    event EventHandler<WorkflowTriggeredEventArgs>? WorkflowTriggered;

    /// <summary>
    /// Event raised when a workflow execution completes.
    /// </summary>
    event EventHandler<WorkflowCompletedEventArgs>? WorkflowCompleted;
}

/// <summary>
/// Workflow definition.
/// </summary>
public sealed record Workflow(
    string Id,
    string Name,
    string? Description,
    WorkflowTrigger Trigger,
    IReadOnlyList<WorkflowAction> Actions,
    WorkflowCondition? Condition,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastExecutedAt = null,
    DateTime? LastModifiedAt = null);

/// <summary>
/// Workflow summary for listing.
/// </summary>
public sealed record WorkflowSummary(
    string Id,
    string Name,
    bool IsActive,
    string TriggerType,
    int ActionCount,
    int ExecutionCount,
    DateTime CreatedAt,
    DateTime? LastExecutedAt = null);

/// <summary>
/// Request to create a workflow.
/// </summary>
public sealed record CreateWorkflowRequest(
    string Name,
    string? Description,
    WorkflowTrigger Trigger,
    List<WorkflowAction> Actions,
    WorkflowCondition? Condition = null);

/// <summary>
/// Request to update a workflow.
/// </summary>
public sealed record UpdateWorkflowRequest(
    string? Name = null,
    string? Description = null,
    WorkflowTrigger? Trigger = null,
    List<WorkflowAction>? Actions = null,
    WorkflowCondition? Condition = null);

/// <summary>
/// Workflow trigger definition.
/// </summary>
public sealed record WorkflowTrigger(
    TriggerType Type,
    string Name,
    Dictionary<string, object> Parameters);

/// <summary>
/// Workflow action definition.
/// </summary>
public sealed record WorkflowAction(
    string Id,
    ActionType Type,
    string Name,
    Dictionary<string, object> Parameters,
    ActionCondition? Condition = null,
    int DelayMs = 0);

/// <summary>
/// Workflow condition for conditional execution.
/// </summary>
public sealed record WorkflowCondition(
    ConditionType Type,
    string Expression,
    IReadOnlyList<WorkflowCondition>? SubConditions = null);

/// <summary>
/// Action condition for individual action execution.
/// </summary>
public sealed record ActionCondition(
    string Expression,
    bool Invert = false);

/// <summary>
/// Workflow execution result.
/// </summary>
public sealed record WorkflowExecutionResult(
    string ExecutionId,
    string WorkflowId,
    bool Success,
    TimeSpan Duration,
    IReadOnlyList<ActionExecutionResult> ActionResults,
    string? ErrorMessage = null);

/// <summary>
/// Action execution result.
/// </summary>
public sealed record ActionExecutionResult(
    string ActionId,
    ActionType Type,
    bool Success,
    TimeSpan Duration,
    string? Output = null,
    string? ErrorMessage = null);

/// <summary>
/// Workflow execution record.
/// </summary>
public sealed record WorkflowExecution(
    string Id,
    string WorkflowId,
    TriggerType TriggerType,
    bool Success,
    TimeSpan Duration,
    DateTime ExecutedAt,
    string? ErrorMessage = null);

/// <summary>
/// Trigger type definition.
/// </summary>
public sealed record TriggerTypeDefinition(
    TriggerType Type,
    string Name,
    string Description,
    string Category,
    IReadOnlyList<TriggerParameter> Parameters);

/// <summary>
/// Trigger parameter definition.
/// </summary>
public sealed record TriggerParameter(
    string Name,
    string Type,
    bool Required,
    string? DefaultValue = null,
    string? Description = null);

/// <summary>
/// Action type definition.
/// </summary>
public sealed record ActionTypeDefinition(
    ActionType Type,
    string Name,
    string Description,
    string Category,
    IReadOnlyList<ActionParameter> Parameters);

/// <summary>
/// Action parameter definition.
/// </summary>
public sealed record ActionParameter(
    string Name,
    string Type,
    bool Required,
    string? DefaultValue = null,
    string? Description = null);

/// <summary>
/// Workflow validation result.
/// </summary>
public sealed record WorkflowValidationResult(
    bool IsValid,
    IReadOnlyList<ValidationIssue> Issues);

/// <summary>
/// Validation issue.
/// </summary>
public sealed record ValidationIssue(
    ValidationIssueType Type,
    string Message,
    string? ElementId = null);

/// <summary>
/// Workflow filter for listing.
/// </summary>
public sealed record WorkflowFilter(
    bool? IsActive = null,
    TriggerType? TriggerType = null,
    string? SearchTerm = null,
    DateTime? CreatedAfter = null,
    DateTime? CreatedBefore = null);

/// <summary>
/// Trigger types for automation workflows.
/// </summary>
public enum TriggerType
{
    // Game Events
    GameLaunched,
    GameClosed,
    AchievementUnlocked,
    SessionStarted,
    SessionEnded,
    GameCompleted,

    // Time-based
    TimeOfDay,
    DayOfWeek,
    SpecificTime,
    Interval,
    IdleTime,

    // System Events
    SystemStartup,
    SystemShutdown,
    DisplayConnected,
    DisplayDisconnected,

    // User Actions
    HotkeyPressed,
    VoiceCommand,
    ManualTrigger
}

/// <summary>
/// Action types for automation workflows.
/// </summary>
public enum ActionType
{
    // Game Actions
    LaunchGame,
    CloseGame,
    SwitchGame,

    // System Actions
    EnableBlueLightFilter,
    DisableBlueLightFilter,
    SetDoNotDisturb,
    ClearDoNotDisturb,
    AdjustVolume,
    Mute,
    Unmute,
    EnablePerformanceMode,
    DisablePerformanceMode,

    // Notification Actions
    SendNotification,
    SendDesktopNotification,
    PostToDiscord,

    // Recording Actions
    StartRecording,
    StopRecording,
    TakeScreenshot,

    // Display Actions
    SetResolution,
    SetRefreshRate,
    EnableHdr,
    DisableHdr,

    // Application Actions
    LaunchApplication,
    CloseApplication,
    FocusWindow,

    // Custom
    ExecuteScript,
    HttpRequest,
    Delay
}

/// <summary>
/// Condition types.
/// </summary>
public enum ConditionType
{
    All,
    Any,
    Not,
    Expression
}

/// <summary>
/// Validation issue types.
/// </summary>
public enum ValidationIssueType
{
    Error,
    Warning,
    Info
}

/// <summary>
/// Event args for workflow triggered events.
/// </summary>
public sealed class WorkflowTriggeredEventArgs : EventArgs
{
    public string WorkflowId { get; }
    public string ExecutionId { get; }
    public TriggerType TriggerType { get; }
    public DateTime TriggeredAt { get; }

    public WorkflowTriggeredEventArgs(string workflowId, string executionId, TriggerType triggerType)
    {
        WorkflowId = workflowId;
        ExecutionId = executionId;
        TriggerType = triggerType;
        TriggeredAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event args for workflow completed events.
/// </summary>
public sealed class WorkflowCompletedEventArgs : EventArgs
{
    public string WorkflowId { get; }
    public string ExecutionId { get; }
    public bool Success { get; }
    public TimeSpan Duration { get; }
    public DateTime CompletedAt { get; }

    public WorkflowCompletedEventArgs(string workflowId, string executionId, bool success, TimeSpan duration)
    {
        WorkflowId = workflowId;
        ExecutionId = executionId;
        Success = success;
        Duration = duration;
        CompletedAt = DateTime.UtcNow;
    }
}
