namespace SaveState.Core.Automation.Services.DTOs;

// ============================================================================
// WORKFLOW CONFIGURATION TYPES (Implementation-compatible)
// ============================================================================

/// <summary>
/// Configuration for creating a workflow (implementation-compatible).
/// </summary>
public sealed record WorkflowConfig(
    IReadOnlyList<WorkflowStep> Steps,
    IReadOnlyList<WorkflowParameter>? Parameters = null,
    WorkflowTrigger Trigger = WorkflowTrigger.Manual,
    TimeSpan? Timeout = null);

/// <summary>
/// Trigger types for workflows.
/// </summary>
public enum WorkflowTrigger
{
    Manual,
    OnGameLaunch,
    OnGameExit,
    Scheduled,
    OnEvent
}

/// <summary>
/// Parameter definition for workflows.
/// </summary>
public sealed record WorkflowParameter(
    string Name,
    Type Type,
    string? Description = null,
    bool IsRequired = true,
    object? DefaultValue = null);

/// <summary>
/// Condition for workflow execution (implementation version).
/// </summary>
public abstract record WorkflowCondition(
    string ConditionType);

/// <summary>
/// File existence condition (implementation version).
/// </summary>
public sealed record FileExistsWorkflowCondition(
    string FilePath) : WorkflowCondition("FileExists");

/// <summary>
/// Process running condition.
/// </summary>
public sealed record ProcessRunningCondition(
    string ProcessName) : WorkflowCondition("ProcessRunning");

/// <summary>
/// Time-based condition.
/// </summary>
public sealed record TimeCondition(
    TimeSpan? AfterTime = null,
    TimeSpan? BeforeTime = null,
    IReadOnlyList<DayOfWeek>? OnDays = null) : WorkflowCondition("Time");

// ============================================================================
// WORKFLOW STEP TYPES (Implementation-compatible)
// ============================================================================

/// <summary>
/// Step to launch a game (implementation version).
/// </summary>
public sealed record LaunchGameStep(
    Guid GameId,
    string Name,
    TimeSpan? Delay = null,
    IReadOnlyList<WorkflowCondition>? Conditions = null) : WorkflowStep("LaunchGame", Name, "", new Dictionary<string, object>
    {
        ["GameId"] = GameId
    });

/// <summary>
/// Step to create a save state (implementation version).
/// </summary>
public sealed record CreateSaveStateStep(
    Guid GameId,
    string Description,
    string Name,
    TimeSpan? Delay = null,
    IReadOnlyList<WorkflowCondition>? Conditions = null) : WorkflowStep("CreateSaveState", Name, Description, new Dictionary<string, object>
    {
        ["GameId"] = GameId,
        ["Description"] = Description
    });

/// <summary>
/// Step to execute a macro (implementation version).
/// </summary>
public sealed record ExecuteMacroStepImpl(
    Guid MacroId,
    string Name,
    MacroPlaybackConfig? Options = null,
    TimeSpan? Delay = null,
    IReadOnlyList<WorkflowCondition>? Conditions = null) : WorkflowStep("ExecuteMacro", Name, "", new Dictionary<string, object>
    {
        ["MacroId"] = MacroId
    });

/// <summary>
/// Step to wait for a duration (implementation version).
/// </summary>
public sealed record WaitStepImpl(
    TimeSpan WaitDuration,
    string Name = "Wait",
    TimeSpan? Delay = null,
    IReadOnlyList<WorkflowCondition>? Conditions = null) : WorkflowStep("Wait", Name, "", new Dictionary<string, object>
    {
        ["Duration"] = WaitDuration
    })
{
    public TimeSpan Duration => WaitDuration;
}

/// <summary>
/// Step to run a system command (implementation version).
/// </summary>
public sealed record RunCommandStep(
    string Command,
    string Arguments,
    string Name = "Run Command",
    TimeSpan? Delay = null,
    IReadOnlyList<WorkflowCondition>? Conditions = null) : WorkflowStep("RunCommand", Name, "", new Dictionary<string, object>
    {
        ["Command"] = Command,
        ["Arguments"] = Arguments
    });

// ============================================================================
// WORKFLOW EVENT TYPES (for implementation)
// ============================================================================

/// <summary>
/// Event arguments for workflow execution events.
/// </summary>
public sealed class WorkflowExecutionEventArgs : EventArgs
{
    public Guid WorkflowId { get; init; }
    public Guid ExecutionId { get; init; }
    public WorkflowExecutionResult? Result { get; init; }
}

/// <summary>
/// Event arguments for workflow step events.
/// </summary>
public sealed class WorkflowStepEventArgs : EventArgs
{
    public Guid WorkflowId { get; init; }
    public Guid ExecutionId { get; init; }
    public string StepName { get; init; } = string.Empty;
    public WorkflowStepResult? Result { get; init; }
}

// ============================================================================
// MACRO TYPES FOR IMPLEMENTATION
// ============================================================================

/// <summary>
/// Represents an active macro recording session (implementation version).
/// </summary>
public sealed record MacroRecording(
    Guid Id,
    Guid? GameId,
    string Name,
    string Description,
    RecordingMode Mode,
    DateTime StartedAt,
    IReadOnlyList<MacroAction> Actions,
    MacroRecordingStateImpl State);

/// <summary>
/// State of macro recording.
/// </summary>
public enum MacroRecordingStateImpl
{
    Recording,
    Paused,
    Stopped,
    Completed
}

/// <summary>
/// Options for macro playback (implementation version).
/// </summary>
public sealed record MacroPlaybackOptions(
    float SpeedMultiplier = 1.0f,
    int RepeatCount = 1,
    TimeSpan? DelayBetweenRepeats = null,
    bool StopOnError = true);

/// <summary>
/// Result of macro execution (implementation version).
/// </summary>
public sealed record MacroExecutionResult(
    Guid MacroId,
    Guid ExecutionId,
    DateTime StartedAt,
    DateTime CompletedAt,
    bool Success,
    int ActionsExecuted,
    int TotalActions,
    TimeSpan Duration,
    string? ErrorMessage = null,
    IReadOnlyList<string>? Errors = null);

// ============================================================================
// MACRO EVENT TYPES (for implementation)
// ============================================================================

/// <summary>
/// Event arguments for macro recording events (implementation version).
/// </summary>
public sealed class MacroRecordingEventArgs : EventArgs
{
    public Guid RecordingId { get; init; }
    public Guid GameId { get; init; }
    public string MacroName { get; init; } = string.Empty;
    public MacroRecordingStateImpl State { get; init; }
}

/// <summary>
/// Event arguments for macro action recorded events (implementation version).
/// </summary>
public sealed class MacroActionRecordedEventArgs : EventArgs
{
    public Guid RecordingId { get; init; }
    public MacroAction Action { get; init; } = null!;
    public int ActionIndex { get; init; }
}

/// <summary>
/// Event arguments for macro playback events (implementation version).
/// </summary>
public sealed class MacroPlaybackEventArgs : EventArgs
{
    public Guid MacroId { get; init; }
    public Guid ExecutionId { get; init; }
    public MacroExecutionResult? Result { get; init; }
    public int CurrentAction { get; init; }
    public int TotalActions { get; init; }
}

// ============================================================================
// MACRO ACTIONS (for CreateWorkflowFromMacro conversion)
// ============================================================================

/// <summary>
/// Action to launch a game (for macro-to-workflow conversion).
/// </summary>
public sealed record LaunchGameAction(
    Guid GameId,
    TimeSpan Timestamp,
    TimeSpan? Delay = null) : MacroAction("LaunchGame", Timestamp);

/// <summary>
/// Action to create a save state (for macro-to-workflow conversion).
/// </summary>
public sealed record CreateSaveStateAction(
    Guid GameId,
    string Description,
    TimeSpan Timestamp,
    TimeSpan? Delay = null) : MacroAction("CreateSaveState", Timestamp);
