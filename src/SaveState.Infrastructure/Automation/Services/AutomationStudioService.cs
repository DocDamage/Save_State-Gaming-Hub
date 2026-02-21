using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Automation.Models;
using SaveState.Core.Automation.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Infrastructure.Automation.Services;

/// <summary>
/// Implementation of the Automation Studio service for managing automation workflows.
/// </summary>
public sealed class AutomationStudioService : IAutomationStudioService
{
    private readonly IWorkflowRepository _workflowRepo;
    private readonly ITriggerMonitor _triggerMonitor;
    private readonly IActionExecutor _actionExecutor;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<AutomationStudioService> _logger;

    /// <summary>
    /// Initializes a new instance of the AutomationStudioService.
    /// </summary>
    public AutomationStudioService(
        IWorkflowRepository workflowRepo,
        ITriggerMonitor triggerMonitor,
        IActionExecutor actionExecutor,
        ITimeProvider timeProvider,
        ILogger<AutomationStudioService> logger)
    {
        _workflowRepo = workflowRepo;
        _triggerMonitor = triggerMonitor;
        _actionExecutor = actionExecutor;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<Workflow>> CreateWorkflowAsync(
        Workflow workflow,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        try
        {
            await _workflowRepo.AddAsync(workflow, ct).ConfigureAwait(false);

            if (workflow.IsEnabled)
            {
                await _triggerMonitor.RegisterWorkflowAsync(workflow, ct).ConfigureAwait(false);
            }

            _logger.LogInformation("Created workflow {WorkflowName} ({WorkflowId})",
                workflow.Name, workflow.Id);

            return Result<Workflow>.Success(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create workflow {WorkflowName}", workflow.Name);
            return Result<Workflow>.Failure("Failed to create workflow", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateWorkflowAsync(
        Workflow workflow,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        try
        {
            var existing = await _workflowRepo.GetByIdAsync(workflow.Id, ct).ConfigureAwait(false);
            if (existing is null)
            {
                return Result.Failure($"Workflow {workflow.Id} not found", ErrorType.NotFound);
            }

            await _workflowRepo.UpdateAsync(workflow, ct).ConfigureAwait(false);
            await _triggerMonitor.UpdateWorkflowAsync(workflow, ct).ConfigureAwait(false);

            _logger.LogInformation("Updated workflow {WorkflowId}", workflow.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update workflow {WorkflowId}", workflow.Id);
            return Result.Failure("Failed to update workflow", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteWorkflowAsync(
        Guid workflowId,
        CancellationToken ct = default)
    {
        try
        {
            var existing = await _workflowRepo.GetByIdAsync(workflowId, ct).ConfigureAwait(false);
            if (existing is null)
            {
                return Result.Failure($"Workflow {workflowId} not found", ErrorType.NotFound);
            }

            await _triggerMonitor.UnregisterWorkflowAsync(workflowId, ct).ConfigureAwait(false);
            await _workflowRepo.DeleteAsync(workflowId, ct).ConfigureAwait(false);

            _logger.LogInformation("Deleted workflow {WorkflowId}", workflowId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete workflow {WorkflowId}", workflowId);
            return Result.Failure("Failed to delete workflow", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<Workflow>>> GetWorkflowsAsync(
        CancellationToken ct = default)
    {
        try
        {
            var workflows = await _workflowRepo.GetAllAsync(ct).ConfigureAwait(false);
            return Result<IReadOnlyList<Workflow>>.Success(workflows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workflows");
            return Result<IReadOnlyList<Workflow>>.Failure("Failed to retrieve workflows", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<Workflow>> GetWorkflowByIdAsync(
        Guid workflowId,
        CancellationToken ct = default)
    {
        try
        {
            var workflow = await _workflowRepo.GetByIdAsync(workflowId, ct).ConfigureAwait(false);
            if (workflow is null)
            {
                return Result<Workflow>.Failure($"Workflow {workflowId} not found", ErrorType.NotFound);
            }

            return Result<Workflow>.Success(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workflow {WorkflowId}", workflowId);
            return Result<Workflow>.Failure("Failed to retrieve workflow", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> ExecuteWorkflowAsync(
        Guid workflowId,
        WorkflowExecutionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            var workflow = await _workflowRepo.GetByIdAsync(workflowId, ct).ConfigureAwait(false);
            if (workflow is null)
            {
                return Result.Failure($"Workflow {workflowId} not found", ErrorType.NotFound);
            }

            _logger.LogInformation("Executing workflow {WorkflowId} - {WorkflowName}",
                workflowId, workflow.Name);

            foreach (var action in workflow.Actions.OrderBy(a => a.Order))
            {
                if (action.DelaySeconds.HasValue && action.DelaySeconds.Value > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(action.DelaySeconds.Value), ct).ConfigureAwait(false);
                }

                var result = await _actionExecutor.ExecuteAsync(action, context, ct).ConfigureAwait(false);
                if (result.IsFailure)
                {
                    _logger.LogWarning("Action {ActionId} ({ActionType}) failed: {Error}",
                        action.Id, action.Type, result.Error);
                }
            }

            await _workflowRepo.UpdateExecutionStatsAsync(workflowId, _timeProvider.UtcNow, ct)
                .ConfigureAwait(false);

            _logger.LogInformation("Completed workflow execution {WorkflowId}", workflowId);
            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Workflow execution {WorkflowId} was cancelled", workflowId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute workflow {WorkflowId}", workflowId);
            return Result.Failure("Workflow execution failed", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> EnableWorkflowAsync(
        Guid workflowId,
        CancellationToken ct = default)
    {
        try
        {
            var workflow = await _workflowRepo.GetByIdAsync(workflowId, ct).ConfigureAwait(false);
            if (workflow is null)
            {
                return Result.Failure($"Workflow {workflowId} not found", ErrorType.NotFound);
            }

            if (workflow.IsEnabled)
            {
                return Result.Success(); // Already enabled
            }

            workflow = workflow with { IsEnabled = true };
            await _workflowRepo.UpdateAsync(workflow, ct).ConfigureAwait(false);
            await _triggerMonitor.RegisterWorkflowAsync(workflow, ct).ConfigureAwait(false);

            _logger.LogInformation("Enabled workflow {WorkflowId}", workflowId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable workflow {WorkflowId}", workflowId);
            return Result.Failure("Failed to enable workflow", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> DisableWorkflowAsync(
        Guid workflowId,
        CancellationToken ct = default)
    {
        try
        {
            await _triggerMonitor.UnregisterWorkflowAsync(workflowId, ct).ConfigureAwait(false);

            var workflow = await _workflowRepo.GetByIdAsync(workflowId, ct).ConfigureAwait(false);
            if (workflow is not null && workflow.IsEnabled)
            {
                workflow = workflow with { IsEnabled = false };
                await _workflowRepo.UpdateAsync(workflow, ct).ConfigureAwait(false);
            }

            _logger.LogInformation("Disabled workflow {WorkflowId}", workflowId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable workflow {WorkflowId}", workflowId);
            return Result.Failure("Failed to disable workflow", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<AutomationTrigger>>> GetAvailableTriggersAsync(
        CancellationToken ct = default)
    {
        var triggers = Enum.GetValues<AutomationTrigger>().ToList();
        return Task.FromResult(Result<IReadOnlyList<AutomationTrigger>>.Success(triggers));
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<AutomationAction>>> GetAvailableActionsAsync(
        CancellationToken ct = default)
    {
        var actions = Enum.GetValues<AutomationAction>().ToList();
        return Task.FromResult(Result<IReadOnlyList<AutomationAction>>.Success(actions));
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<WorkflowTemplate>>> GetWorkflowTemplatesAsync(
        CancellationToken ct = default)
    {
        var templates = GetDefaultTemplates();
        return Task.FromResult(Result<IReadOnlyList<WorkflowTemplate>>.Success(templates));
    }

    /// <inheritdoc />
    public async Task<Result<Workflow>> CreateWorkflowFromTemplateAsync(
        Guid templateId,
        string name,
        string description,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        try
        {
            var templates = GetDefaultTemplates();
            var template = templates.FirstOrDefault(t => t.Id == templateId);

            if (template is null)
            {
                return Result<Workflow>.Failure($"Template {templateId} not found", ErrorType.NotFound);
            }

            var workflow = template.CreateWorkflow(name, description, _timeProvider);
            await _workflowRepo.AddAsync(workflow, ct).ConfigureAwait(false);

            _logger.LogInformation("Created workflow {WorkflowName} from template {TemplateName}",
                name, template.Name);

            return Result<Workflow>.Success(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create workflow from template {TemplateId}", templateId);
            return Result<Workflow>.Failure("Failed to create workflow from template", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public Task<Result<WorkflowValidationResult>> ValidateWorkflowAsync(
        Workflow workflow,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        var errors = new List<string>();
        var warnings = new List<string>();

        // Validate name
        if (string.IsNullOrWhiteSpace(workflow.Name))
        {
            errors.Add("Workflow name is required");
        }
        else if (workflow.Name.Length > 100)
        {
            errors.Add("Workflow name must be 100 characters or less");
        }

        // Validate trigger configuration
        ValidateTriggerConfiguration(workflow.Trigger, workflow.TriggerConfig, errors, warnings);

        // Validate actions
        if (!workflow.Actions.Any())
        {
            errors.Add("Workflow must have at least one action");
        }

        // Check for duplicate action orders
        var duplicateOrders = workflow.Actions
            .GroupBy(a => a.Order)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateOrders.Any())
        {
            warnings.Add($"Duplicate action orders detected: {string.Join(", ", duplicateOrders)}");
        }

        var result = errors.Any()
            ? WorkflowValidationResult.Failure(errors)
            : warnings.Any()
                ? WorkflowValidationResult.WithWarnings(warnings)
                : WorkflowValidationResult.Success();

        return Task.FromResult(Result<WorkflowValidationResult>.Success(result));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<WorkflowExecutionLog>>> GetExecutionHistoryAsync(
        Guid workflowId,
        int limit = 50,
        CancellationToken ct = default)
    {
        // This would typically query a log repository
        // For now, return empty list as placeholder
        return Result<IReadOnlyList<WorkflowExecutionLog>>.Success(Array.Empty<WorkflowExecutionLog>());
    }

    /// <inheritdoc />
    public async Task<Result<Workflow>> DuplicateWorkflowAsync(
        Guid workflowId,
        string newName,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(newName);

        try
        {
            var sourceWorkflow = await _workflowRepo.GetByIdAsync(workflowId, ct).ConfigureAwait(false);
            if (sourceWorkflow is null)
            {
                return Result<Workflow>.Failure($"Workflow {workflowId} not found", ErrorType.NotFound);
            }

            var newActions = sourceWorkflow.Actions
                .Select(a => a with { Id = Guid.NewGuid() })
                .ToList();

            var duplicated = new Workflow
            {
                Id = Guid.NewGuid(),
                Name = newName,
                Description = $"Copy of {sourceWorkflow.Name}: {sourceWorkflow.Description}",
                IsEnabled = false, // Disabled by default for safety
                Trigger = sourceWorkflow.Trigger,
                TriggerConfig = sourceWorkflow.TriggerConfig,
                Actions = newActions,
                CreatedAt = _timeProvider.UtcNow,
                LastExecuted = null,
                ExecutionCount = 0
            };

            await _workflowRepo.AddAsync(duplicated, ct).ConfigureAwait(false);

            _logger.LogInformation("Duplicated workflow {SourceId} to {NewId} - {NewName}",
                workflowId, duplicated.Id, newName);

            return Result<Workflow>.Success(duplicated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to duplicate workflow {WorkflowId}", workflowId);
            return Result<Workflow>.Failure("Failed to duplicate workflow", ErrorType.Internal);
        }
    }

    private static void ValidateTriggerConfiguration(
        AutomationTrigger trigger,
        TriggerConfiguration config,
        List<string> errors,
        List<string> warnings)
    {
        switch (trigger)
        {
            case AutomationTrigger.GameLaunched:
            case AutomationTrigger.GameClosed:
            case AutomationTrigger.AchievementUnlocked:
            case AutomationTrigger.SaveStateCreated:
                if (string.IsNullOrEmpty(config.GameId))
                {
                    warnings.Add("No specific game selected - trigger will fire for all games");
                }
                break;

            case AutomationTrigger.TimeOfDay:
                if (!config.Time.HasValue)
                {
                    errors.Add("Time is required for TimeOfDay trigger");
                }
                break;

            case AutomationTrigger.DayOfWeek:
                if (!config.Day.HasValue)
                {
                    errors.Add("Day is required for DayOfWeek trigger");
                }
                break;

            case AutomationTrigger.PlaytimeMilestone:
                if (!config.PlaytimeMinutes.HasValue || config.PlaytimeMinutes.Value <= 0)
                {
                    errors.Add("Valid playtime minutes are required for PlaytimeMilestone trigger");
                }
                break;
        }
    }

    private static IReadOnlyList<WorkflowTemplate> GetDefaultTemplates()
    {
        return new List<WorkflowTemplate>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Gaming Session Starter",
                Description = "Automatically enable performance mode and disable notifications when starting a game",
                Category = "Performance",
                Icon = "Rocket",
                DefaultTrigger = AutomationTrigger.GameLaunched,
                DefaultActions = new[]
                {
                    AutomationAction.EnablePerformanceMode,
                    AutomationAction.SetDoNotDisturb
                }
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Achievement Celebrator",
                Description = "Post to Discord and start recording when unlocking an achievement",
                Category = "Social",
                Icon = "Trophy",
                DefaultTrigger = AutomationTrigger.AchievementUnlocked,
                DefaultActions = new[]
                {
                    AutomationAction.PostToDiscord,
                    AutomationAction.StartRecording
                }
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Late Night Gaming",
                Description = "Enable blue light filter when gaming after 8 PM",
                Category = "Health",
                Icon = "Moon",
                DefaultTrigger = AutomationTrigger.TimeOfDay,
                DefaultActions = new[]
                {
                    AutomationAction.EnableBlueLightFilter
                }
            },
            new()
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Stream Ready",
                Description = "Launch OBS and adjust RGB lighting when launching a specific game",
                Category = "Streaming",
                Icon = "Video",
                DefaultTrigger = AutomationTrigger.GameLaunched,
                DefaultActions = new[]
                {
                    AutomationAction.LaunchApplication,
                    AutomationAction.AdjustRgbLighting
                }
            },
            new()
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "Save State Backup",
                Description = "Run backup script when creating a save state",
                Category = "Utility",
                Icon = "Save",
                DefaultTrigger = AutomationTrigger.SaveStateCreated,
                DefaultActions = new[]
                {
                    AutomationAction.RunScript,
                    AutomationAction.SendNotification
                }
            }
        };
    }
}
