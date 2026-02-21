using SaveState.Core.Common.Services;

namespace SaveState.Core.Automation.Models;

/// <summary>
/// Available triggers for automation workflows.
/// </summary>
public enum AutomationTrigger
{
    /// <summary>Triggered when a game is launched.</summary>
    GameLaunched,
    /// <summary>Triggered when a game is closed.</summary>
    GameClosed,
    /// <summary>Triggered when an achievement is unlocked.</summary>
    AchievementUnlocked,
    /// <summary>Triggered when a gaming session starts.</summary>
    SessionStarted,
    /// <summary>Triggered when a gaming session ends.</summary>
    SessionEnded,
    /// <summary>Triggered at a specific time of day.</summary>
    TimeOfDay,
    /// <summary>Triggered on a specific day of the week.</summary>
    DayOfWeek,
    /// <summary>Triggered at a specific date and time.</summary>
    SpecificTime,
    /// <summary>Triggered when hardware changes (e.g., controller connected).</summary>
    HardwareChange,
    /// <summary>Triggered when a notification is received.</summary>
    NotificationReceived,
    /// <summary>Triggered when a save state is created.</summary>
    SaveStateCreated,
    /// <summary>Triggered when a playtime milestone is reached.</summary>
    PlaytimeMilestone
}

/// <summary>
/// Available actions for automation workflows.
/// </summary>
public enum AutomationAction
{
    /// <summary>Launch a specific game.</summary>
    LaunchGame,
    /// <summary>Enable blue light filter.</summary>
    EnableBlueLightFilter,
    /// <summary>Set do not disturb mode.</summary>
    SetDoNotDisturb,
    /// <summary>Send a notification.</summary>
    SendNotification,
    /// <summary>Adjust system volume.</summary>
    AdjustVolume,
    /// <summary>Change display settings.</summary>
    ChangeDisplaySettings,
    /// <summary>Post a message to Discord.</summary>
    PostToDiscord,
    /// <summary>Start recording gameplay.</summary>
    StartRecording,
    /// <summary>Enable performance mode.</summary>
    EnablePerformanceMode,
    /// <summary>Run a custom script.</summary>
    RunScript,
    /// <summary>Adjust RGB lighting.</summary>
    AdjustRgbLighting,
    /// <summary>Launch an application.</summary>
    LaunchApplication,
    /// <summary>Close an application.</summary>
    CloseApplication
}

/// <summary>
/// Represents an automation workflow.
/// </summary>
public record Workflow
{
    /// <summary>Unique identifier for the workflow.</summary>
    public required Guid Id { get; init; }
    
    /// <summary>Name of the workflow.</summary>
    public required string Name { get; init; }
    
    /// <summary>Description of what the workflow does.</summary>
    public required string Description { get; init; }
    
    /// <summary>Whether the workflow is currently enabled.</summary>
    public required bool IsEnabled { get; init; }
    
    /// <summary>The trigger that starts this workflow.</summary>
    public required AutomationTrigger Trigger { get; init; }
    
    /// <summary>Configuration for the trigger.</summary>
    public required TriggerConfiguration TriggerConfig { get; init; }
    
    /// <summary>List of actions to execute when triggered.</summary>
    public required IReadOnlyList<WorkflowAction> Actions { get; init; }
    
    /// <summary>When the workflow was created.</summary>
    public required DateTime CreatedAt { get; init; }
    
    /// <summary>When the workflow was last executed.</summary>
    public required DateTime? LastExecuted { get; init; }
    
    /// <summary>Number of times the workflow has been executed.</summary>
    public required int ExecutionCount { get; init; }

    /// <summary>
    /// Creates a new workflow with the specified parameters.
    /// </summary>
    public static Workflow Create(
        string name,
        string description,
        AutomationTrigger trigger,
        TriggerConfiguration triggerConfig,
        IEnumerable<WorkflowAction> actions,
        ITimeProvider timeProvider)
    {
        var actionList = actions.ToList();
        for (int i = 0; i < actionList.Count; i++)
        {
            actionList[i] = actionList[i] with { Order = i + 1 };
        }

        return new Workflow
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsEnabled = true,
            Trigger = trigger,
            TriggerConfig = triggerConfig,
            Actions = actionList,
            CreatedAt = timeProvider.UtcNow,
            LastExecuted = null,
            ExecutionCount = 0
        };
    }
}

/// <summary>
/// Configuration for an automation trigger.
/// </summary>
public record TriggerConfiguration
{
    /// <summary>Game ID for game-related triggers.</summary>
    public required string? GameId { get; init; }
    
    /// <summary>Time for time-based triggers.</summary>
    public required TimeSpan? Time { get; init; }
    
    /// <summary>Day of week for day-based triggers.</summary>
    public required DayOfWeek? Day { get; init; }
    
    /// <summary>Playtime threshold in minutes for playtime triggers.</summary>
    public required int? PlaytimeMinutes { get; init; }
    
    /// <summary>Additional condition expression.</summary>
    public required string? Condition { get; init; }

    /// <summary>
    /// Creates a trigger configuration for game launch/close events.
    /// </summary>
    public static TriggerConfiguration ForGame(string gameId)
    {
        return new TriggerConfiguration
        {
            GameId = gameId,
            Time = null,
            Day = null,
            PlaytimeMinutes = null,
            Condition = null
        };
    }

    /// <summary>
    /// Creates a trigger configuration for time-based events.
    /// </summary>
    public static TriggerConfiguration ForTimeOfDay(TimeSpan time, DayOfWeek? day = null)
    {
        return new TriggerConfiguration
        {
            GameId = null,
            Time = time,
            Day = day,
            PlaytimeMinutes = null,
            Condition = null
        };
    }

    /// <summary>
    /// Creates a trigger configuration for playtime milestones.
    /// </summary>
    public static TriggerConfiguration ForPlaytime(int minutes, string? gameId = null)
    {
        return new TriggerConfiguration
        {
            GameId = gameId,
            Time = null,
            Day = null,
            PlaytimeMinutes = minutes,
            Condition = null
        };
    }

    /// <summary>
    /// Creates a default empty trigger configuration.
    /// </summary>
    public static TriggerConfiguration Default()
    {
        return new TriggerConfiguration
        {
            GameId = null,
            Time = null,
            Day = null,
            PlaytimeMinutes = null,
            Condition = null
        };
    }
}

/// <summary>
/// An action to execute within a workflow.
/// </summary>
public record WorkflowAction
{
    /// <summary>Unique identifier for the action.</summary>
    public required Guid Id { get; init; }
    
    /// <summary>Type of action to execute.</summary>
    public required AutomationAction Type { get; init; }
    
    /// <summary>Parameters for the action.</summary>
    public required Dictionary<string, object> Parameters { get; init; }
    
    /// <summary>Execution order within the workflow.</summary>
    public required int Order { get; init; }
    
    /// <summary>Optional delay before executing this action (in seconds).</summary>
    public required int? DelaySeconds { get; init; }

    /// <summary>
    /// Creates a new workflow action.
    /// </summary>
    public static WorkflowAction Create(AutomationAction type, Dictionary<string, object>? parameters = null, int? delaySeconds = null)
    {
        return new WorkflowAction
        {
            Id = Guid.NewGuid(),
            Type = type,
            Parameters = parameters ?? new Dictionary<string, object>(),
            Order = 0,
            DelaySeconds = delaySeconds
        };
    }
}

/// <summary>
/// Context for workflow execution.
/// </summary>
public record WorkflowExecutionContext
{
    /// <summary>ID of the workflow being executed.</summary>
    public required Guid WorkflowId { get; init; }
    
    /// <summary>Source that triggered the workflow.</summary>
    public required string TriggerSource { get; init; }
    
    /// <summary>Data from the trigger event.</summary>
    public required Dictionary<string, object> TriggerData { get; init; }
    
    /// <summary>When the workflow was executed.</summary>
    public required DateTime ExecutedAt { get; init; }

    /// <summary>
    /// Creates a new execution context.
    /// </summary>
    public static WorkflowExecutionContext Create(Guid workflowId, string triggerSource, Dictionary<string, object>? triggerData = null, ITimeProvider? timeProvider = null)
    {
        timeProvider ??= SystemTimeProvider.Instance;
        return new WorkflowExecutionContext
        {
            WorkflowId = workflowId,
            TriggerSource = triggerSource,
            TriggerData = triggerData ?? new Dictionary<string, object>(),
            ExecutedAt = timeProvider.UtcNow
        };
    }
}

/// <summary>
/// Visual node representation for workflow editor.
/// </summary>
public record WorkflowNode
{
    /// <summary>Unique identifier for the node.</summary>
    public required Guid Id { get; init; }
    
    /// <summary>Type of node (Trigger, Action, Condition).</summary>
    public required NodeType Type { get; init; }
    
    /// <summary>Display label for the node.</summary>
    public required string Label { get; init; }
    
    /// <summary>X position on the canvas.</summary>
    public required double X { get; init; }
    
    /// <summary>Y position on the canvas.</summary>
    public required double Y { get; init; }
    
    /// <summary>Node-specific data.</summary>
    public required Dictionary<string, object> Data { get; init; }
    
    /// <summary>IDs of connected output nodes.</summary>
    public required IReadOnlyList<Guid> Connections { get; init; }
}

/// <summary>
/// Types of nodes in the workflow editor.
/// </summary>
public enum NodeType
{
    /// <summary>Trigger node - entry point of workflow.</summary>
    Trigger,
    /// <summary>Action node - performs an operation.</summary>
    Action,
    /// <summary>Condition node - branching logic.</summary>
    Condition,
    /// <summary>Delay node - pauses execution.</summary>
    Delay
}

/// <summary>
/// Represents a workflow template for quick setup.
/// </summary>
public record WorkflowTemplate
{
    /// <summary>Unique identifier for the template.</summary>
    public required Guid Id { get; init; }
    
    /// <summary>Name of the template.</summary>
    public required string Name { get; init; }
    
    /// <summary>Description of what the template does.</summary>
    public required string Description { get; init; }
    
    /// <summary>Category for grouping templates.</summary>
    public required string Category { get; init; }
    
    /// <summary>Icon identifier for the template.</summary>
    public required string Icon { get; init; }
    
    /// <summary>Default trigger type.</summary>
    public required AutomationTrigger DefaultTrigger { get; init; }
    
    /// <summary>Default actions for this template.</summary>
    public required IReadOnlyList<AutomationAction> DefaultActions { get; init; }

    /// <summary>
    /// Creates a workflow from this template.
    /// </summary>
    public Workflow CreateWorkflow(string name, string description, ITimeProvider timeProvider)
    {
        var actions = DefaultActions.Select(a => WorkflowAction.Create(a)).ToList();
        return Workflow.Create(name, description, DefaultTrigger, TriggerConfiguration.Default(), actions, timeProvider);
    }
}

/// <summary>
/// Execution log entry for a workflow.
/// </summary>
public record WorkflowExecutionLog
{
    /// <summary>Unique identifier for the log entry.</summary>
    public required Guid Id { get; init; }
    
    /// <summary>ID of the workflow that executed.</summary>
    public required Guid WorkflowId { get; init; }
    
    /// <summary>When the execution started.</summary>
    public required DateTime StartedAt { get; init; }
    
    /// <summary>When the execution completed (null if still running).</summary>
    public required DateTime? CompletedAt { get; init; }
    
    /// <summary>Whether the execution was successful.</summary>
    public required bool IsSuccess { get; init; }
    
    /// <summary>Error message if execution failed.</summary>
    public required string? ErrorMessage { get; init; }
    
    /// <summary>Actions that were executed.</summary>
    public required IReadOnlyList<WorkflowActionExecutionLog> ActionLogs { get; init; }
}

/// <summary>
/// Execution log for a single action within a workflow.
/// </summary>
public record WorkflowActionExecutionLog
{
    /// <summary>ID of the action.</summary>
    public required Guid ActionId { get; init; }
    
    /// <summary>Type of action.</summary>
    public required AutomationAction ActionType { get; init; }
    
    /// <summary>When the action started.</summary>
    public required DateTime StartedAt { get; init; }
    
    /// <summary>When the action completed.</summary>
    public required DateTime? CompletedAt { get; init; }
    
    /// <summary>Whether the action succeeded.</summary>
    public required bool IsSuccess { get; init; }
    
    /// <summary>Error message if action failed.</summary>
    public required string? ErrorMessage { get; init; }
}
