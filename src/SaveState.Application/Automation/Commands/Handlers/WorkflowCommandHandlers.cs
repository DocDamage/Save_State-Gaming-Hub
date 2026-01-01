using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Automation.Services;
using SaveState.Core.Automation.Services.DTOs;
using SaveState.Core.Common;

namespace SaveState.Application.Automation.Commands.Handlers;

/// <summary>
/// Command handlers for workflow operations.
/// </summary>
public class CreateWorkflowCommandHandler : IRequestHandler<CreateWorkflowCommand, Result<Workflow>>
{
    private readonly IWorkflowAutomationService _workflowService;
    private readonly ILogger<CreateWorkflowCommandHandler> _logger;

    public CreateWorkflowCommandHandler(
        IWorkflowAutomationService workflowService,
        ILogger<CreateWorkflowCommandHandler> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    public async Task<Result<Workflow>> Handle(CreateWorkflowCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _workflowService.CreateWorkflowAsync(request.Config, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Created workflow: {Name}", result.Value!.Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create workflow");
            return Result<Workflow>.Failure($"Failed to create workflow: {ex.Message}");
        }
    }
}

public class UpdateWorkflowCommandHandler : IRequestHandler<UpdateWorkflowCommand, Result>
{
    private readonly IWorkflowAutomationService _workflowService;
    private readonly ILogger<UpdateWorkflowCommandHandler> _logger;

    public UpdateWorkflowCommandHandler(
        IWorkflowAutomationService workflowService,
        ILogger<UpdateWorkflowCommandHandler> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateWorkflowCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _workflowService.UpdateWorkflowAsync(request.WorkflowId, request.Config, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Updated workflow: {Id}", request.WorkflowId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update workflow: {Id}", request.WorkflowId);
            return Result.Failure($"Failed to update workflow: {ex.Message}");
        }
    }
}

public class DeleteWorkflowCommandHandler : IRequestHandler<DeleteWorkflowCommand, Result>
{
    private readonly IWorkflowAutomationService _workflowService;
    private readonly ILogger<DeleteWorkflowCommandHandler> _logger;

    public DeleteWorkflowCommandHandler(
        IWorkflowAutomationService workflowService,
        ILogger<DeleteWorkflowCommandHandler> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteWorkflowCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _workflowService.DeleteWorkflowAsync(request.WorkflowId, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Deleted workflow: {Id}", request.WorkflowId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete workflow: {Id}", request.WorkflowId);
            return Result.Failure($"Failed to delete workflow: {ex.Message}");
        }
    }
}

public class GetWorkflowsCommandHandler : IRequestHandler<GetWorkflowsCommand, Result<IReadOnlyList<Workflow>>>
{
    private readonly IWorkflowAutomationService _workflowService;
    private readonly ILogger<GetWorkflowsCommandHandler> _logger;

    public GetWorkflowsCommandHandler(
        IWorkflowAutomationService workflowService,
        ILogger<GetWorkflowsCommandHandler> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<Workflow>>> Handle(GetWorkflowsCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _workflowService.GetAllWorkflowsAsync(ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Retrieved {Count} workflows", result.Value!.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workflows");
            return Result<IReadOnlyList<Workflow>>.Failure($"Failed to get workflows: {ex.Message}");
        }
    }
}

public class GetWorkflowCommandHandler : IRequestHandler<GetWorkflowCommand, Result<Workflow>>
{
    private readonly IWorkflowAutomationService _workflowService;
    private readonly ILogger<GetWorkflowCommandHandler> _logger;

    public GetWorkflowCommandHandler(
        IWorkflowAutomationService workflowService,
        ILogger<GetWorkflowCommandHandler> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    public async Task<Result<Workflow>> Handle(GetWorkflowCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _workflowService.GetWorkflowAsync(request.WorkflowId, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Retrieved workflow: {Name}", result.Value!.Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workflow: {Id}", request.WorkflowId);
            return Result<Workflow>.Failure($"Failed to get workflow: {ex.Message}");
        }
    }
}

public class ExecuteWorkflowCommandHandler : IRequestHandler<ExecuteWorkflowCommand, Result<WorkflowExecutionResult>>
{
    private readonly IWorkflowAutomationService _workflowService;
    private readonly ILogger<ExecuteWorkflowCommandHandler> _logger;

    public ExecuteWorkflowCommandHandler(
        IWorkflowAutomationService workflowService,
        ILogger<ExecuteWorkflowCommandHandler> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    public async Task<Result<WorkflowExecutionResult>> Handle(ExecuteWorkflowCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _workflowService.ExecuteWorkflowAsync(request.WorkflowId, request.Parameters, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                var executionResult = result.Value!;
                _logger.LogInformation("Workflow executed: {Id}, Success: {Success}, Steps: {Executed}/{Skipped}",
                    request.WorkflowId, executionResult.Success, executionResult.StepsExecuted, executionResult.StepsSkipped);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute workflow: {Id}", request.WorkflowId);
            return Result<WorkflowExecutionResult>.Failure($"Failed to execute workflow: {ex.Message}");
        }
    }
}

public class ValidateWorkflowCommandHandler : IRequestHandler<ValidateWorkflowCommand, Result>
{
    private readonly IWorkflowAutomationService _workflowService;
    private readonly ILogger<ValidateWorkflowCommandHandler> _logger;

    public ValidateWorkflowCommandHandler(
        IWorkflowAutomationService workflowService,
        ILogger<ValidateWorkflowCommandHandler> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    public async Task<Result> Handle(ValidateWorkflowCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _workflowService.ValidateWorkflowAsync(request.Config, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Workflow configuration validated successfully");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate workflow configuration");
            return Result.Failure($"Failed to validate workflow: {ex.Message}");
        }
    }
}

public class GetWorkflowExecutionHistoryCommandHandler : IRequestHandler<GetWorkflowExecutionHistoryCommand, Result<IReadOnlyList<WorkflowExecutionResult>>>
{
    private readonly IWorkflowAutomationService _workflowService;
    private readonly ILogger<GetWorkflowExecutionHistoryCommandHandler> _logger;

    public GetWorkflowExecutionHistoryCommandHandler(
        IWorkflowAutomationService workflowService,
        ILogger<GetWorkflowExecutionHistoryCommandHandler> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<WorkflowExecutionResult>>> Handle(GetWorkflowExecutionHistoryCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _workflowService.GetExecutionHistoryAsync(request.WorkflowId, request.Since, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Retrieved {Count} workflow execution history entries", result.Value!.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workflow execution history: {Id}", request.WorkflowId);
            return Result<IReadOnlyList<WorkflowExecutionResult>>.Failure($"Failed to get history: {ex.Message}");
        }
    }
}

public class SetWorkflowEnabledCommandHandler : IRequestHandler<SetWorkflowEnabledCommand, Result>
{
    private readonly IWorkflowAutomationService _workflowService;
    private readonly ILogger<SetWorkflowEnabledCommandHandler> _logger;

    public SetWorkflowEnabledCommandHandler(
        IWorkflowAutomationService workflowService,
        ILogger<SetWorkflowEnabledCommandHandler> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    public async Task<Result> Handle(SetWorkflowEnabledCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _workflowService.SetWorkflowEnabledAsync(request.WorkflowId, request.Enabled, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Set workflow {Id} enabled: {Enabled}", request.WorkflowId, request.Enabled);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set workflow enabled: {Id}", request.WorkflowId);
            return Result.Failure($"Failed to set workflow enabled: {ex.Message}");
        }
    }
}

public class CreateWorkflowFromMacroCommandHandler : IRequestHandler<CreateWorkflowFromMacroCommand, Result<Workflow>>
{
    private readonly IWorkflowAutomationService _workflowService;
    private readonly ILogger<CreateWorkflowFromMacroCommandHandler> _logger;

    public CreateWorkflowFromMacroCommandHandler(
        IWorkflowAutomationService workflowService,
        ILogger<CreateWorkflowFromMacroCommandHandler> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    public async Task<Result<Workflow>> Handle(CreateWorkflowFromMacroCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _workflowService.CreateWorkflowFromMacroAsync(
                request.MacroId, request.WorkflowName, request.Description, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Created workflow from macro: {Name}", result.Value!.Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create workflow from macro: {MacroId}", request.MacroId);
            return Result<Workflow>.Failure($"Failed to create workflow from macro: {ex.Message}");
        }
    }
}

public class ExportWorkflowCommandHandler : IRequestHandler<ExportWorkflowCommand, Result<string>>
{
    private readonly IWorkflowAutomationService _workflowService;
    private readonly ILogger<ExportWorkflowCommandHandler> _logger;

    public ExportWorkflowCommandHandler(
        IWorkflowAutomationService workflowService,
        ILogger<ExportWorkflowCommandHandler> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(ExportWorkflowCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _workflowService.ExportWorkflowAsync(request.WorkflowId, request.FilePath, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Exported workflow {Id} to {Path}", request.WorkflowId, request.FilePath);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export workflow: {Id}", request.WorkflowId);
            return Result<string>.Failure($"Failed to export workflow: {ex.Message}");
        }
    }
}

public class ImportWorkflowCommandHandler : IRequestHandler<ImportWorkflowCommand, Result<Workflow>>
{
    private readonly IWorkflowAutomationService _workflowService;
    private readonly ILogger<ImportWorkflowCommandHandler> _logger;

    public ImportWorkflowCommandHandler(
        IWorkflowAutomationService workflowService,
        ILogger<ImportWorkflowCommandHandler> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    public async Task<Result<Workflow>> Handle(ImportWorkflowCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _workflowService.ImportWorkflowAsync(request.FilePath, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Imported workflow: {Name}", result.Value!.Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import workflow from {Path}", request.FilePath);
            return Result<Workflow>.Failure($"Failed to import workflow: {ex.Message}");
        }
    }
}
