namespace SaveState.Core.Automation.Services.DTOs;

/// <summary>
/// A workflow definition with steps and configuration.
/// </summary>
public sealed record Workflow(
    Guid Id,
    string Name,
    string Description,
    WorkflowConfig Config,
    bool IsEnabled,
    DateTime CreatedAt,
    DateTime LastModified,
    DateTime? LastExecutedAt = null,
    bool IsActive = true)
{
    // Convenience property to access steps from config
    public IReadOnlyList<WorkflowStep> Steps => Config.Steps.Cast<WorkflowStep>().ToList();
}

/// <summary>
/// Definition for creating a workflow.
/// </summary>
public sealed record WorkflowDefinition(
    string Name,
    string Description,
    IReadOnlyList<WorkflowStep> Steps,
    WorkflowMetadata Metadata);

/// <summary>
/// Metadata for workflows.
/// </summary>
public sealed record WorkflowMetadata(
    string Author,
    string Version,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Properties,
    WorkflowStatistics? Statistics = null);

/// <summary>
/// Statistics for workflow usage.
/// </summary>
public sealed record WorkflowStatistics(
    int TotalExecutions,
    TimeSpan TotalRuntime,
    TimeSpan AverageRuntime,
    DateTime LastExecuted,
    int SuccessCount,
    int FailureCount);

/// <summary>
/// A step within a workflow.
/// </summary>
public abstract record WorkflowStep(
    string StepType,
    string Name,
    string Description,
    IReadOnlyDictionary<string, object> Parameters,
    WorkflowStepCondition? Condition = null,
    TimeSpan? Timeout = null);

/// <summary>
/// Macro execution step.
/// </summary>
public sealed record MacroStep(
    string Name,
    string Description,
    Guid MacroId,
    MacroPlaybackConfig PlaybackConfig,
    WorkflowStepCondition? Condition = null,
    TimeSpan? Timeout = null) : WorkflowStep("Macro", Name, Description, new Dictionary<string, object>
    {
        ["MacroId"] = MacroId,
        ["PlaybackConfig"] = PlaybackConfig
    }, Condition, Timeout);

/// <summary>
/// Backup execution step.
/// </summary>
public sealed record BackupStep(
    string Name,
    string Description,
    Guid GameId,
    BackupOptions BackupOptions,
    WorkflowStepCondition? Condition = null,
    TimeSpan? Timeout = null) : WorkflowStep("Backup", Name, Description, new Dictionary<string, object>
    {
        ["GameId"] = GameId,
        ["BackupOptions"] = BackupOptions
    }, Condition, Timeout);

/// <summary>
/// Game launch step.
/// </summary>
public sealed record GameLaunchStep(
    string Name,
    string Description,
    Guid GameId,
    IReadOnlyDictionary<string, string>? LaunchParameters = null,
    WorkflowStepCondition? Condition = null,
    TimeSpan? Timeout = null) : WorkflowStep("GameLaunch", Name, Description, new Dictionary<string, object>
    {
        ["GameId"] = GameId,
        ["LaunchParameters"] = LaunchParameters ?? new Dictionary<string, string>()
    }, Condition, Timeout);

/// <summary>
/// System command step.
/// </summary>
public sealed record SystemCommandStep(
    string Name,
    string Description,
    string Command,
    IReadOnlyList<string> Arguments,
    WorkflowStepCondition? Condition = null,
    TimeSpan? Timeout = null) : WorkflowStep("SystemCommand", Name, Description, new Dictionary<string, object>
    {
        ["Command"] = Command,
        ["Arguments"] = Arguments
    }, Condition, Timeout);

/// <summary>
/// Condition for executing a workflow step.
/// </summary>
public abstract record WorkflowStepCondition(
    string ConditionType);

/// <summary>
/// File existence condition.
/// </summary>
public sealed record FileExistsCondition(
    string FilePath) : WorkflowStepCondition("FileExists");

/// <summary>
/// Variable condition.
/// </summary>
public sealed record VariableCondition(
    string VariableName,
    string Operator,
    object Value) : WorkflowStepCondition("Variable");

/// <summary>
/// Parameters for workflow execution.
/// </summary>
public sealed record WorkflowExecutionParameters(
    IReadOnlyDictionary<string, object>? Variables = null,
    bool DryRun = false,
    TimeSpan? Timeout = null);

/// <summary>
/// Result of workflow execution.
/// </summary>
public sealed record WorkflowExecutionResult(
    Guid WorkflowId,
    Guid ExecutionId,
    DateTime StartedAt,
    DateTime CompletedAt,
    bool Success,
    int StepsExecuted,
    int StepsSkipped,
    TimeSpan Duration,
    IReadOnlyList<WorkflowStepResult> StepResults,
    string? ErrorMessage = null)
{
    // Computed property for interface compatibility
    public WorkflowExecutionStatus Status => Success ? WorkflowExecutionStatus.Success : WorkflowExecutionStatus.Failed;
    public TimeSpan TotalDuration => Duration;
}

/// <summary>
/// Status of workflow execution.
/// </summary>
public enum WorkflowExecutionStatus
{
    Success,
    PartialSuccess,
    Failed,
    Cancelled,
    Timeout
}

/// <summary>
/// Result of executing a single workflow step.
/// </summary>
public sealed record WorkflowStepResult(
    string StepName,
    bool Success,
    TimeSpan Duration,
    string? Result = null,
    string? ErrorMessage = null)
{
    // Computed property for interface compatibility
    public WorkflowStepStatus Status => Success ? WorkflowStepStatus.Success :
        (ErrorMessage != null ? WorkflowStepStatus.Failed : WorkflowStepStatus.Skipped);
}

/// <summary>
/// Status of workflow step execution.
/// </summary>
public enum WorkflowStepStatus
{
    Success,
    Failed,
    Skipped,
    Timeout
}

/// <summary>
/// Result of workflow validation.
/// </summary>
public sealed record WorkflowValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    TimeSpan EstimatedDuration);

/// <summary>
/// Schedule configuration for workflows.
/// </summary>
public sealed record WorkflowScheduleConfig(
    WorkflowScheduleType Type,
    TimeSpan? TimeOfDay = null,
    IReadOnlyList<DayOfWeek>? DaysOfWeek = null,
    DateTime? StartDate = null,
    bool IsEnabled = true);

/// <summary>
/// Type of workflow schedule.
/// </summary>
public enum WorkflowScheduleType
{
    Manual,
    Daily,
    Weekly,
    OnGameLaunch,
    OnGameExit
}

/// <summary>
/// A scheduled workflow.
/// </summary>
public sealed record WorkflowSchedule(
    Guid Id,
    Guid WorkflowId,
    WorkflowScheduleConfig Config,
    DateTime CreatedAt,
    DateTime? NextRunTime = null);

/// <summary>
/// Workflow template for common scenarios.
/// </summary>
public sealed record WorkflowTemplate(
    string Id,
    string Name,
    string Description,
    string Category,
    IReadOnlyList<WorkflowStep> Steps,
    IReadOnlyDictionary<string, string> Parameters);

/// <summary>
/// Parameters for creating workflow from template.
/// </summary>
public sealed record WorkflowTemplateParameters(
    string WorkflowName,
    IReadOnlyDictionary<string, object> ParameterValues);

/// <summary>
/// Event arguments for workflow execution started.
/// </summary>
public sealed class WorkflowExecutionStartedEventArgs : EventArgs
{
    public Guid ExecutionId { get; init; }
    public Guid WorkflowId { get; init; }
    public required WorkflowExecutionParameters Parameters { get; init; }
}

/// <summary>
/// Event arguments for workflow execution completed.
/// </summary>
public sealed class WorkflowExecutionCompletedEventArgs : EventArgs
{
    public required WorkflowExecutionResult Result { get; init; }
}

/// <summary>
/// Event arguments for workflow step executed.
/// </summary>
public sealed class WorkflowStepExecutedEventArgs : EventArgs
{
    public Guid ExecutionId { get; init; }
    public required WorkflowStepResult StepResult { get; init; }
}
