using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Automation.Studio;
using System.Text.Json;

namespace SaveState.Infrastructure.Automation.Studio;

/// <summary>
/// Implementation of the Automation Studio service for managing workflows.
/// </summary>
public sealed class AutomationStudioService : IAutomationStudioService
{
    private readonly IWorkflowEngine _workflowEngine;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<AutomationStudioService> _logger;

    private readonly Dictionary<string, Workflow> _workflows = new();
    private readonly Dictionary<string, List<WorkflowExecution>> _executionHistory = new();
    private readonly Dictionary<string, TriggerTypeDefinition> _triggerTypes = new();
    private readonly Dictionary<string, ActionTypeDefinition> _actionTypes = new();

    public event EventHandler<WorkflowTriggeredEventArgs>? WorkflowTriggered;
    public event EventHandler<WorkflowCompletedEventArgs>? WorkflowCompleted;

    public AutomationStudioService(
        IWorkflowEngine workflowEngine,
        ITimeProvider timeProvider,
        ILogger<AutomationStudioService> logger)
    {
        _workflowEngine = workflowEngine ?? throw new ArgumentNullException(nameof(workflowEngine));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        InitializeTriggerTypes();
        InitializeActionTypes();
    }

    public Task<Result<Workflow>> CreateWorkflowAsync(CreateWorkflowRequest request, CancellationToken ct = default)
    {
        try
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrEmpty(request.Name)) throw new ArgumentException("Name cannot be empty", nameof(request.Name));
            if (request.Trigger is null) throw new ArgumentNullException(nameof(request.Trigger));

            var workflowId = Guid.NewGuid().ToString();
            var workflow = new Workflow(
                Id: workflowId,
                Name: request.Name,
                Description: request.Description,
                Trigger: request.Trigger,
                Actions: request.Actions ?? new List<WorkflowAction>(),
                Condition: request.Condition,
                IsActive: false,
                CreatedAt: _timeProvider.UtcNow);

            lock (_workflows)
            {
                _workflows[workflowId] = workflow;
                _executionHistory[workflowId] = new List<WorkflowExecution>();
            }

            _logger.LogInformation("Created workflow: {WorkflowId} - {Name}", workflowId, request.Name);
            return Task.FromResult(Result<Workflow>.Success(workflow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create workflow");
            return Task.FromResult(Result<Workflow>.Failure($"Failed to create workflow: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<Workflow>> GetWorkflowAsync(string workflowId, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(workflowId)) throw new ArgumentException("WorkflowId cannot be empty", nameof(workflowId));

            lock (_workflows)
            {
                if (!_workflows.TryGetValue(workflowId, out var workflow))
                {
                    return Task.FromResult(Result<Workflow>.Failure("Workflow not found", ErrorType.NotFound));
                }

                return Task.FromResult(Result<Workflow>.Success(workflow));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workflow");
            return Task.FromResult(Result<Workflow>.Failure($"Failed to get workflow: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<Workflow>> UpdateWorkflowAsync(string workflowId, UpdateWorkflowRequest request, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(workflowId)) throw new ArgumentException("WorkflowId cannot be empty", nameof(workflowId));
            if (request is null) throw new ArgumentNullException(nameof(request));

            lock (_workflows)
            {
                if (!_workflows.TryGetValue(workflowId, out var workflow))
                {
                    return Task.FromResult(Result<Workflow>.Failure("Workflow not found", ErrorType.NotFound));
                }

                workflow = workflow with
                {
                    Name = request.Name ?? workflow.Name,
                    Description = request.Description ?? workflow.Description,
                    Trigger = request.Trigger ?? workflow.Trigger,
                    Actions = request.Actions ?? workflow.Actions,
                    Condition = request.Condition ?? workflow.Condition,
                    LastModifiedAt = _timeProvider.UtcNow
                };

                _workflows[workflowId] = workflow;

                _logger.LogInformation("Updated workflow: {WorkflowId}", workflowId);
                return Task.FromResult(Result<Workflow>.Success(workflow));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update workflow");
            return Task.FromResult(Result<Workflow>.Failure($"Failed to update workflow: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> DeleteWorkflowAsync(string workflowId, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(workflowId)) throw new ArgumentException("WorkflowId cannot be empty", nameof(workflowId));

            lock (_workflows)
            {
                if (!_workflows.Remove(workflowId))
                {
                    return Task.FromResult(Result.Failure("Workflow not found", ErrorType.NotFound));
                }

                _executionHistory.Remove(workflowId);
            }

            _logger.LogInformation("Deleted workflow: {WorkflowId}", workflowId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete workflow");
            return Task.FromResult(Result.Failure($"Failed to delete workflow: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<IReadOnlyList<WorkflowSummary>>> ListWorkflowsAsync(WorkflowFilter? filter = null, CancellationToken ct = default)
    {
        try
        {
            lock (_workflows)
            {
                var workflows = _workflows.Values.AsEnumerable();

                if (filter?.IsActive.HasValue == true)
                    workflows = workflows.Where(w => w.IsActive == filter.IsActive.Value);

                if (filter?.TriggerType.HasValue == true)
                    workflows = workflows.Where(w => w.Trigger.Type == filter.TriggerType.Value);

                if (!string.IsNullOrEmpty(filter?.SearchTerm))
                    workflows = workflows.Where(w =>
                        w.Name.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (w.Description?.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false));

                if (filter?.CreatedAfter.HasValue == true)
                    workflows = workflows.Where(w => w.CreatedAt >= filter.CreatedAfter.Value);

                if (filter?.CreatedBefore.HasValue == true)
                    workflows = workflows.Where(w => w.CreatedAt <= filter.CreatedBefore.Value);

                var summaries = workflows.Select(w => new WorkflowSummary(
                    Id: w.Id,
                    Name: w.Name,
                    IsActive: w.IsActive,
                    TriggerType: w.Trigger.Type.ToString(),
                    ActionCount: w.Actions.Count,
                    ExecutionCount: _executionHistory.TryGetValue(w.Id, out var history) ? history.Count : 0,
                    CreatedAt: w.CreatedAt,
                    LastExecutedAt: w.LastExecutedAt)).ToList();

                return Task.FromResult(Result<IReadOnlyList<WorkflowSummary>>.Success(summaries));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list workflows");
            return Task.FromResult(Result<IReadOnlyList<WorkflowSummary>>.Failure($"Failed to list workflows: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> ActivateWorkflowAsync(string workflowId, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(workflowId)) throw new ArgumentException("WorkflowId cannot be empty", nameof(workflowId));

            lock (_workflows)
            {
                if (!_workflows.TryGetValue(workflowId, out var workflow))
                {
                    return Task.FromResult(Result.Failure("Workflow not found", ErrorType.NotFound));
                }

                _workflows[workflowId] = workflow with { IsActive = true };
            }

            _logger.LogInformation("Activated workflow: {WorkflowId}", workflowId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate workflow");
            return Task.FromResult(Result.Failure($"Failed to activate workflow: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> DeactivateWorkflowAsync(string workflowId, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(workflowId)) throw new ArgumentException("WorkflowId cannot be empty", nameof(workflowId));

            lock (_workflows)
            {
                if (!_workflows.TryGetValue(workflowId, out var workflow))
                {
                    return Task.FromResult(Result.Failure("Workflow not found", ErrorType.NotFound));
                }

                _workflows[workflowId] = workflow with { IsActive = false };
            }

            _logger.LogInformation("Deactivated workflow: {WorkflowId}", workflowId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate workflow");
            return Task.FromResult(Result.Failure($"Failed to deactivate workflow: {ex.Message}", ErrorType.Internal));
        }
    }

    public async Task<Result<WorkflowExecutionResult>> TriggerWorkflowAsync(string workflowId, Dictionary<string, object>? context = null, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(workflowId)) throw new ArgumentException("WorkflowId cannot be empty", nameof(workflowId));

            Workflow workflow;
            lock (_workflows)
            {
                if (!_workflows.TryGetValue(workflowId, out workflow))
                {
                    return Result<WorkflowExecutionResult>.Failure("Workflow not found", ErrorType.NotFound);
                }
            }

            var executionId = Guid.NewGuid().ToString();
            WorkflowTriggered?.Invoke(this, new WorkflowTriggeredEventArgs(workflowId, executionId, workflow.Trigger.Type));

            var workflowContext = new WorkflowContext(
                TriggerType: workflow.Trigger.Type,
                Variables: context ?? new Dictionary<string, object>(),
                TriggeredAt: _timeProvider.UtcNow);

            var startTime = _timeProvider.UtcNow;
            var actionResults = new List<ActionExecutionResult>();
            bool success = true;
            string? errorMessage = null;

            foreach (var action in workflow.Actions)
            {
                var actionStart = _timeProvider.UtcNow;
                try
                {
                    actionResults.Add(new ActionExecutionResult(
                        ActionId: action.Id,
                        Type: action.Type,
                        Success: true,
                        Duration: _timeProvider.UtcNow - actionStart));
                }
                catch (Exception ex)
                {
                    success = false;
                    errorMessage = ex.Message;
                    actionResults.Add(new ActionExecutionResult(
                        ActionId: action.Id,
                        Type: action.Type,
                        Success: false,
                        Duration: _timeProvider.UtcNow - actionStart,
                        ErrorMessage: ex.Message));
                    break;
                }
            }

            var duration = _timeProvider.UtcNow - startTime;

            var result = new WorkflowExecutionResult(
                ExecutionId: executionId,
                WorkflowId: workflowId,
                Success: success,
                Duration: duration,
                ActionResults: actionResults,
                ErrorMessage: errorMessage);

            var execution = new WorkflowExecution(
                Id: executionId,
                WorkflowId: workflowId,
                TriggerType: workflow.Trigger.Type,
                Success: success,
                Duration: duration,
                ExecutedAt: startTime,
                ErrorMessage: errorMessage);

            lock (_executionHistory)
            {
                if (!_executionHistory.ContainsKey(workflowId))
                    _executionHistory[workflowId] = new List<WorkflowExecution>();
                _executionHistory[workflowId].Add(execution);
            }

            lock (_workflows)
            {
                if (_workflows.TryGetValue(workflowId, out var wf))
                {
                    _workflows[workflowId] = wf with { LastExecutedAt = startTime };
                }
            }

            WorkflowCompleted?.Invoke(this, new WorkflowCompletedEventArgs(workflowId, executionId, success, duration));

            _logger.LogInformation("Workflow triggered: {WorkflowId}, Success: {Success}", workflowId, success);
            return Result<WorkflowExecutionResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger workflow");
            return Result<WorkflowExecutionResult>.Failure($"Failed to trigger workflow: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<IReadOnlyList<WorkflowExecution>>> GetExecutionHistoryAsync(string workflowId, int limit = 50, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(workflowId)) throw new ArgumentException("WorkflowId cannot be empty", nameof(workflowId));

            lock (_executionHistory)
            {
                if (!_executionHistory.TryGetValue(workflowId, out var history))
                {
                    return Task.FromResult(Result<IReadOnlyList<WorkflowExecution>>.Success(new List<WorkflowExecution>()));
                }

                var results = history.OrderByDescending(e => e.ExecutedAt).Take(limit).ToList();
                return Task.FromResult(Result<IReadOnlyList<WorkflowExecution>>.Success(results));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get execution history");
            return Task.FromResult(Result<IReadOnlyList<WorkflowExecution>>.Failure($"Failed to get history: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<IReadOnlyList<TriggerTypeDefinition>>> GetAvailableTriggersAsync(CancellationToken ct = default)
    {
        lock (_triggerTypes)
        {
            return Task.FromResult(Result<IReadOnlyList<TriggerTypeDefinition>>.Success(_triggerTypes.Values.ToList()));
        }
    }

    public Task<Result<IReadOnlyList<ActionTypeDefinition>>> GetAvailableActionsAsync(CancellationToken ct = default)
    {
        lock (_actionTypes)
        {
            return Task.FromResult(Result<IReadOnlyList<ActionTypeDefinition>>.Success(_actionTypes.Values.ToList()));
        }
    }

    public Task<Result<WorkflowValidationResult>> ValidateWorkflowAsync(Workflow workflow, CancellationToken ct = default)
    {
        try
        {
            if (workflow is null) throw new ArgumentNullException(nameof(workflow));

            var issues = new List<ValidationIssue>();

            if (string.IsNullOrEmpty(workflow.Name))
                issues.Add(new ValidationIssue(ValidationIssueType.Error, "Workflow name is required"));

            if (workflow.Actions.Count == 0)
                issues.Add(new ValidationIssue(ValidationIssueType.Error, "Workflow must have at least one action"));

            foreach (var action in workflow.Actions)
            {
                if (string.IsNullOrEmpty(action.Id))
                    issues.Add(new ValidationIssue(ValidationIssueType.Error, "Action ID is required", action.Id));
            }

            return Task.FromResult(Result<WorkflowValidationResult>.Success(
                new WorkflowValidationResult(issues.Count == 0, issues)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate workflow");
            return Task.FromResult(Result<WorkflowValidationResult>.Failure($"Failed to validate: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<Workflow>> DuplicateWorkflowAsync(string workflowId, string? newName = null, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(workflowId)) throw new ArgumentException("WorkflowId cannot be empty", nameof(workflowId));

            lock (_workflows)
            {
                if (!_workflows.TryGetValue(workflowId, out var workflow))
                {
                    return Task.FromResult(Result<Workflow>.Failure("Workflow not found", ErrorType.NotFound));
                }

                var duplicatedId = Guid.NewGuid().ToString();
                var duplicated = workflow with
                {
                    Id = duplicatedId,
                    Name = newName ?? $"{workflow.Name} (Copy)",
                    IsActive = false,
                    CreatedAt = _timeProvider.UtcNow,
                    LastExecutedAt = null
                };

                _workflows[duplicatedId] = duplicated;
                _executionHistory[duplicatedId] = new List<WorkflowExecution>();

                _logger.LogInformation("Duplicated workflow: {SourceId} -> {NewId}", workflowId, duplicatedId);
                return Task.FromResult(Result<Workflow>.Success(duplicated));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to duplicate workflow");
            return Task.FromResult(Result<Workflow>.Failure($"Failed to duplicate: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<Workflow>> ImportWorkflowAsync(string json, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(json)) throw new ArgumentException("Json cannot be empty", nameof(json));

            var workflow = JsonSerializer.Deserialize<Workflow>(json);
            if (workflow == null)
            {
                return Task.FromResult(Result<Workflow>.Failure("Invalid workflow JSON", ErrorType.Validation));
            }

            var newId = Guid.NewGuid().ToString();
            workflow = workflow with
            {
                Id = newId,
                CreatedAt = _timeProvider.UtcNow,
                IsActive = false
            };

            lock (_workflows)
            {
                _workflows[newId] = workflow;
                _executionHistory[newId] = new List<WorkflowExecution>();
            }

            _logger.LogInformation("Imported workflow: {WorkflowId}", newId);
            return Task.FromResult(Result<Workflow>.Success(workflow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import workflow");
            return Task.FromResult(Result<Workflow>.Failure($"Failed to import: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<string>> ExportWorkflowAsync(string workflowId, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(workflowId)) throw new ArgumentException("WorkflowId cannot be empty", nameof(workflowId));

            lock (_workflows)
            {
                if (!_workflows.TryGetValue(workflowId, out var workflow))
                {
                    return Task.FromResult(Result<string>.Failure("Workflow not found", ErrorType.NotFound));
                }

                var json = JsonSerializer.Serialize(workflow, new JsonSerializerOptions { WriteIndented = true });
                return Task.FromResult(Result<string>.Success(json));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export workflow");
            return Task.FromResult(Result<string>.Failure($"Failed to export: {ex.Message}", ErrorType.Internal));
        }
    }

    private void InitializeTriggerTypes()
    {
        _triggerTypes["GameLaunched"] = new TriggerTypeDefinition(
            TriggerType.GameLaunched,
            "Game Launched",
            "Triggers when a game is launched",
            "Game Events",
            new List<TriggerParameter>
            {
                new("gameId", "string", false, null, "Specific game ID to match"),
                new("platform", "string", false, null, "Game platform filter")
            });

        _triggerTypes["GameClosed"] = new TriggerTypeDefinition(
            TriggerType.GameClosed,
            "Game Closed",
            "Triggers when a game is closed",
            "Game Events",
            new List<TriggerParameter>
            {
                new("gameId", "string", false, null, "Specific game ID to match"),
                new("sessionDuration", "number", false, null, "Minimum session duration in minutes")
            });

        _triggerTypes["AchievementUnlocked"] = new TriggerTypeDefinition(
            TriggerType.AchievementUnlocked,
            "Achievement Unlocked",
            "Triggers when an achievement is unlocked",
            "Game Events",
            new List<TriggerParameter>
            {
                new("gameId", "string", false, null, "Specific game ID to match"),
                new("rarity", "string", false, null, "Achievement rarity filter")
            });

        _triggerTypes["TimeOfDay"] = new TriggerTypeDefinition(
            TriggerType.TimeOfDay,
            "Time of Day",
            "Triggers at a specific time of day",
            "Time-based",
            new List<TriggerParameter>
            {
                new("hour", "number", true, "0", "Hour (0-23)"),
                new("minute", "number", true, "0", "Minute (0-59)")
            });

        _triggerTypes["DayOfWeek"] = new TriggerTypeDefinition(
            TriggerType.DayOfWeek,
            "Day of Week",
            "Triggers on specific days of the week",
            "Time-based",
            new List<TriggerParameter>
            {
                new("days", "array", true, null, "Array of days (0=Sunday, 6=Saturday)")
            });

        _triggerTypes["SessionStarted"] = new TriggerTypeDefinition(
            TriggerType.SessionStarted,
            "Session Started",
            "Triggers when a gaming session starts",
            "Game Events",
            Array.Empty<TriggerParameter>());

        _triggerTypes["SessionEnded"] = new TriggerTypeDefinition(
            TriggerType.SessionEnded,
            "Session Ended",
            "Triggers when a gaming session ends",
            "Game Events",
            new List<TriggerParameter>
            {
                new("minDuration", "number", false, "0", "Minimum session duration in minutes")
            });
    }

    private void InitializeActionTypes()
    {
        _actionTypes["LaunchGame"] = new ActionTypeDefinition(
            ActionType.LaunchGame,
            "Launch Game",
            "Launches a specific game",
            "Game Actions",
            new List<ActionParameter>
            {
                new("gameId", "string", true, null, "ID of the game to launch")
            });

        _actionTypes["EnableBlueLightFilter"] = new ActionTypeDefinition(
            ActionType.EnableBlueLightFilter,
            "Enable Blue Light Filter",
            "Enables blue light filter on displays",
            "System Actions",
            new List<ActionParameter>
            {
                new("intensity", "number", false, "50", "Filter intensity (0-100)")
            });

        _actionTypes["SetDoNotDisturb"] = new ActionTypeDefinition(
            ActionType.SetDoNotDisturb,
            "Set Do Not Disturb",
            "Enables Do Not Disturb mode",
            "System Actions",
            new List<ActionParameter>
            {
                new("duration", "number", false, "60", "Duration in minutes")
            });

        _actionTypes["SendNotification"] = new ActionTypeDefinition(
            ActionType.SendNotification,
            "Send Notification",
            "Sends a desktop notification",
            "Notification Actions",
            new List<ActionParameter>
            {
                new("title", "string", true, null, "Notification title"),
                new("message", "string", true, null, "Notification message")
            });

        _actionTypes["PostToDiscord"] = new ActionTypeDefinition(
            ActionType.PostToDiscord,
            "Post to Discord",
            "Posts a message to Discord webhook",
            "Notification Actions",
            new List<ActionParameter>
            {
                new("webhookUrl", "string", true, null, "Discord webhook URL"),
                new("message", "string", true, null, "Message to post")
            });

        _actionTypes["AdjustVolume"] = new ActionTypeDefinition(
            ActionType.AdjustVolume,
            "Adjust Volume",
            "Adjusts system volume",
            "System Actions",
            new List<ActionParameter>
            {
                new("level", "number", true, null, "Volume level (0-100)"),
                new("application", "string", false, null, "Specific application (optional)")
            });

        _actionTypes["StartRecording"] = new ActionTypeDefinition(
            ActionType.StartRecording,
            "Start Recording",
            "Starts gameplay recording",
            "Recording Actions",
            new List<ActionParameter>
            {
                new("quality", "string", false, "high", "Recording quality")
            });

        _actionTypes["EnablePerformanceMode"] = new ActionTypeDefinition(
            ActionType.EnablePerformanceMode,
            "Enable Performance Mode",
            "Enables high-performance mode",
            "System Actions",
            new List<ActionParameter>
            {
                new("priority", "string", false, "high", "Process priority")
            });

        _actionTypes["Delay"] = new ActionTypeDefinition(
            ActionType.Delay,
            "Delay",
            "Waits for specified time",
            "Utility",
            new List<ActionParameter>
            {
                new("milliseconds", "number", true, null, "Delay in milliseconds")
            });
    }
}
