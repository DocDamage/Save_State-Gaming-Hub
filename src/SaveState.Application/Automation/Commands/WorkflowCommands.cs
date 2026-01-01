using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Automation.Services.DTOs;

namespace SaveState.Application.Automation.Commands;

/// <summary>
/// Commands for workflow operations.
/// </summary>
public record CreateWorkflowCommand(
    WorkflowConfig Config) : IRequest<Result<Workflow>>;

public record UpdateWorkflowCommand(
    Guid WorkflowId,
    WorkflowConfig Config) : IRequest<Result>;

public record DeleteWorkflowCommand(
    Guid WorkflowId) : IRequest<Result>;

public record GetWorkflowsCommand : IRequest<Result<IReadOnlyList<Workflow>>>;

public record GetWorkflowCommand(
    Guid WorkflowId) : IRequest<Result<Workflow>>;

public record ExecuteWorkflowCommand(
    Guid WorkflowId,
    IReadOnlyDictionary<string, object>? Parameters = null) : IRequest<Result<WorkflowExecutionResult>>;

public record ValidateWorkflowCommand(
    WorkflowConfig Config) : IRequest<Result>;

public record GetWorkflowExecutionHistoryCommand(
    Guid WorkflowId,
    DateTime? Since = null) : IRequest<Result<IReadOnlyList<WorkflowExecutionResult>>>;

public record SetWorkflowEnabledCommand(
    Guid WorkflowId,
    bool Enabled) : IRequest<Result>;

public record CreateWorkflowFromMacroCommand(
    Guid MacroId,
    string WorkflowName,
    string Description) : IRequest<Result<Workflow>>;

public record ExportWorkflowCommand(
    Guid WorkflowId,
    string FilePath) : IRequest<Result<string>>;

public record ImportWorkflowCommand(
    string FilePath) : IRequest<Result<Workflow>>;
