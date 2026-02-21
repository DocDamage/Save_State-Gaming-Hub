using SaveState.Core.Common;
using SaveState.Core.Automation.Models;

namespace SaveState.Core.Automation.Services;

/// <summary>
/// Service for managing automation workflows in the Automation Studio.
/// Provides CRUD operations, execution, and template management for gaming automation.
/// </summary>
public interface IAutomationStudioService
{
    /// <summary>
    /// Creates a new automation workflow.
    /// </summary>
    /// <param name="workflow">The workflow to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created workflow.</returns>
    Task<Result<Workflow>> CreateWorkflowAsync(
        Workflow workflow,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing automation workflow.
    /// </summary>
    /// <param name="workflow">The workflow with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success or failure result.</returns>
    Task<Result> UpdateWorkflowAsync(
        Workflow workflow,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes an automation workflow.
    /// </summary>
    /// <param name="workflowId">ID of the workflow to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success or failure result.</returns>
    Task<Result> DeleteWorkflowAsync(
        Guid workflowId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all automation workflows.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of all workflows.</returns>
    Task<Result<IReadOnlyList<Workflow>>> GetWorkflowsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets a workflow by its ID.
    /// </summary>
    /// <param name="workflowId">The workflow ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The workflow if found.</returns>
    Task<Result<Workflow>> GetWorkflowByIdAsync(
        Guid workflowId,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a workflow manually.
    /// </summary>
    /// <param name="workflowId">ID of the workflow to execute.</param>
    /// <param name="context">Execution context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success or failure result.</returns>
    Task<Result> ExecuteWorkflowAsync(
        Guid workflowId,
        WorkflowExecutionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Enables a workflow (starts monitoring its trigger).
    /// </summary>
    /// <param name="workflowId">ID of the workflow to enable.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success or failure result.</returns>
    Task<Result> EnableWorkflowAsync(
        Guid workflowId,
        CancellationToken ct = default);

    /// <summary>
    /// Disables a workflow (stops monitoring its trigger).
    /// </summary>
    /// <param name="workflowId">ID of the workflow to disable.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success or failure result.</returns>
    Task<Result> DisableWorkflowAsync(
        Guid workflowId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all available trigger types.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of available triggers.</returns>
    Task<Result<IReadOnlyList<AutomationTrigger>>> GetAvailableTriggersAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets all available action types.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of available actions.</returns>
    Task<Result<IReadOnlyList<AutomationAction>>> GetAvailableActionsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets available workflow templates.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of workflow templates.</returns>
    Task<Result<IReadOnlyList<WorkflowTemplate>>> GetWorkflowTemplatesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Creates a workflow from a template.
    /// </summary>
    /// <param name="templateId">ID of the template.</param>
    /// <param name="name">Name for the new workflow.</param>
    /// <param name="description">Description for the new workflow.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created workflow.</returns>
    Task<Result<Workflow>> CreateWorkflowFromTemplateAsync(
        Guid templateId,
        string name,
        string description,
        CancellationToken ct = default);

    /// <summary>
    /// Validates a workflow configuration.
    /// </summary>
    /// <param name="workflow">The workflow to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result with any errors.</returns>
    Task<Result<WorkflowValidationResult>> ValidateWorkflowAsync(
        Workflow workflow,
        CancellationToken ct = default);

    /// <summary>
    /// Gets execution history for a workflow.
    /// </summary>
    /// <param name="workflowId">ID of the workflow.</param>
    /// <param name="limit">Maximum number of entries to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of execution log entries.</returns>
    Task<Result<IReadOnlyList<WorkflowExecutionLog>>> GetExecutionHistoryAsync(
        Guid workflowId,
        int limit = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Duplicates an existing workflow.
    /// </summary>
    /// <param name="workflowId">ID of the workflow to duplicate.</param>
    /// <param name="newName">Name for the duplicated workflow.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The duplicated workflow.</returns>
    Task<Result<Workflow>> DuplicateWorkflowAsync(
        Guid workflowId,
        string newName,
        CancellationToken ct = default);
}

/// <summary>
/// Repository interface for workflow persistence.
/// </summary>
public interface IWorkflowRepository
{
    /// <summary>
    /// Adds a new workflow to the repository.
    /// </summary>
    Task AddAsync(Workflow workflow, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing workflow.
    /// </summary>
    Task UpdateAsync(Workflow workflow, CancellationToken ct = default);

    /// <summary>
    /// Deletes a workflow by ID.
    /// </summary>
    Task DeleteAsync(Guid workflowId, CancellationToken ct = default);

    /// <summary>
    /// Gets a workflow by ID.
    /// </summary>
    Task<Workflow?> GetByIdAsync(Guid workflowId, CancellationToken ct = default);

    /// <summary>
    /// Gets all workflows.
    /// </summary>
    Task<IReadOnlyList<Workflow>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets only enabled workflows.
    /// </summary>
    Task<IReadOnlyList<Workflow>> GetEnabledAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates execution statistics for a workflow.
    /// </summary>
    Task UpdateExecutionStatsAsync(Guid workflowId, DateTime executedAt, CancellationToken ct = default);

    /// <summary>
    /// Gets workflows by trigger type.
    /// </summary>
    Task<IReadOnlyList<Workflow>> GetByTriggerAsync(AutomationTrigger trigger, CancellationToken ct = default);
}

/// <summary>
/// Service for monitoring workflow triggers.
/// </summary>
public interface ITriggerMonitor
{
    /// <summary>
    /// Registers a workflow for trigger monitoring.
    /// </summary>
    Task RegisterWorkflowAsync(Workflow workflow, CancellationToken ct = default);

    /// <summary>
    /// Unregisters a workflow from trigger monitoring.
    /// </summary>
    Task UnregisterWorkflowAsync(Guid workflowId, CancellationToken ct = default);

    /// <summary>
    /// Updates a registered workflow's trigger configuration.
    /// </summary>
    Task UpdateWorkflowAsync(Workflow workflow, CancellationToken ct = default);

    /// <summary>
    /// Starts monitoring all enabled workflows.
    /// </summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>
    /// Stops monitoring.
    /// </summary>
    Task StopAsync(CancellationToken ct = default);
}

/// <summary>
/// Service for executing workflow actions.
/// </summary>
public interface IActionExecutor
{
    /// <summary>
    /// Executes a single workflow action.
    /// </summary>
    Task<Result> ExecuteAsync(WorkflowAction action, WorkflowExecutionContext context, CancellationToken ct = default);

    /// <summary>
    /// Gets the display name for an action type.
    /// </summary>
    string GetActionDisplayName(AutomationAction action);

    /// <summary>
    /// Gets the description for an action type.
    /// </summary>
    string GetActionDescription(AutomationAction action);

    /// <summary>
    /// Gets the required parameters for an action type.
    /// </summary>
    IReadOnlyList<ActionParameterDefinition> GetRequiredParameters(AutomationAction action);
}

/// <summary>
/// Definition of an action parameter.
/// </summary>
public record ActionParameterDefinition
{
    /// <summary>Name of the parameter.</summary>
    public required string Name { get; init; }

    /// <summary>Display name for the UI.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Type of the parameter.</summary>
    public required ActionParameterType Type { get; init; }

    /// <summary>Whether the parameter is required.</summary>
    public required bool IsRequired { get; init; }

    /// <summary>Default value if not required.</summary>
    public required object? DefaultValue { get; init; }

    /// <summary>Description of the parameter.</summary>
    public required string Description { get; init; }
}

/// <summary>
/// Types of action parameters.
/// </summary>
public enum ActionParameterType
{
    /// <summary>Text string.</summary>
    String,
    /// <summary>Integer number.</summary>
    Integer,
    /// <summary>Floating point number.</summary>
    Decimal,
    /// <summary>Boolean value.</summary>
    Boolean,
    /// <summary>File path.</summary>
    FilePath,
    /// <summary>Dropdown selection.</summary>
    Select,
    /// <summary>Color value.</summary>
    Color,
    /// <summary>Time duration.</summary>
    Duration
}

/// <summary>
/// Result of workflow validation.
/// </summary>
public record WorkflowValidationResult
{
    /// <summary>Whether the workflow is valid.</summary>
    public required bool IsValid { get; init; }

    /// <summary>List of validation errors.</summary>
    public required IReadOnlyList<string> Errors { get; init; }

    /// <summary>List of validation warnings.</summary>
    public required IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static WorkflowValidationResult Success()
    {
        return new WorkflowValidationResult
        {
            IsValid = true,
            Errors = Array.Empty<string>(),
            Warnings = Array.Empty<string>()
        };
    }

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static WorkflowValidationResult Failure(IEnumerable<string> errors)
    {
        return new WorkflowValidationResult
        {
            IsValid = false,
            Errors = errors.ToList(),
            Warnings = Array.Empty<string>()
        };
    }

    /// <summary>
    /// Creates a validation result with warnings.
    /// </summary>
    public static WorkflowValidationResult WithWarnings(IEnumerable<string> warnings)
    {
        return new WorkflowValidationResult
        {
            IsValid = true,
            Errors = Array.Empty<string>(),
            Warnings = warnings.ToList()
        };
    }
}
