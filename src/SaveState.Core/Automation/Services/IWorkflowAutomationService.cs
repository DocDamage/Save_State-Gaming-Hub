using SaveState.Core.Common;
using SaveState.Core.Automation.Services.DTOs;

namespace SaveState.Core.Automation.Services;

/// <summary>
/// Service for automating complex gaming workflows and processes.
/// </summary>
public interface IWorkflowAutomationService
{
    /// <summary>
    /// Creates a new workflow from a configuration.
    /// </summary>
    Task<Result<Workflow>> CreateWorkflowAsync(
        WorkflowConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a workflow with optional variables.
    /// </summary>
    Task<Result<WorkflowExecutionResult>> ExecuteWorkflowAsync(
        Guid workflowId,
        IReadOnlyDictionary<string, object>? parameters = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a workflow by ID.
    /// </summary>
    Task<Result<Workflow>> GetWorkflowAsync(
        Guid workflowId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all workflows.
    /// </summary>
    Task<Result<IReadOnlyList<Workflow>>> GetAllWorkflowsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Updates a workflow configuration.
    /// </summary>
    Task<Result> UpdateWorkflowAsync(
        Guid workflowId,
        WorkflowConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a workflow.
    /// </summary>
    Task<Result> DeleteWorkflowAsync(
        Guid workflowId,
        CancellationToken ct = default);

    /// <summary>
    /// Validates a workflow configuration.
    /// </summary>
    Task<Result> ValidateWorkflowAsync(
        WorkflowConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Gets workflow execution history.
    /// </summary>
    Task<Result<IReadOnlyList<WorkflowExecutionResult>>> GetExecutionHistoryAsync(
        Guid workflowId,
        DateTime? since = null,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a workflow from a recorded macro sequence.
    /// </summary>
    Task<Result<Workflow>> CreateWorkflowFromMacroAsync(
        Guid macroId,
        string workflowName,
        string description,
        CancellationToken ct = default);

    /// <summary>
    /// Enables or disables a workflow.
    /// </summary>
    Task<Result> SetWorkflowEnabledAsync(
        Guid workflowId,
        bool enabled,
        CancellationToken ct = default);

    /// <summary>
    /// Exports a workflow to a file.
    /// </summary>
    Task<Result<string>> ExportWorkflowAsync(
        Guid workflowId,
        string filePath,
        CancellationToken ct = default);

    /// <summary>
    /// Imports a workflow from a file.
    /// </summary>
    Task<Result<Workflow>> ImportWorkflowAsync(
        string filePath,
        CancellationToken ct = default);

    /// <summary>
    /// Event raised when workflow execution starts.
    /// </summary>
    event EventHandler<WorkflowExecutionStartedEventArgs>? WorkflowExecutionStarted;

    /// <summary>
    /// Event raised when workflow execution completes.
    /// </summary>
    event EventHandler<WorkflowExecutionCompletedEventArgs>? WorkflowExecutionCompleted;

    /// <summary>
    /// Event raised when a workflow step executes.
    /// </summary>
    event EventHandler<WorkflowStepExecutedEventArgs>? WorkflowStepExecuted;
}
