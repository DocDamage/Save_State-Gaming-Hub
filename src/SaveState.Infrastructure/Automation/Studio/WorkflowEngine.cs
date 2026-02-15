using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Automation.Studio;

namespace SaveState.Infrastructure.Automation.Studio;

/// <summary>
/// Implementation of the workflow execution engine.
/// </summary>
public sealed class WorkflowEngine : IWorkflowEngine
{
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<WorkflowEngine> _logger;

    private readonly Dictionary<TriggerType, List<TriggerListener>> _triggerListeners = new();
    private readonly Dictionary<string, WorkflowExecutionContext> _activeExecutions = new();
    private readonly Dictionary<ActionType, IActionHandler> _actionHandlers = new();
    private long _totalExecutions;
    private long _successfulExecutions;
    private long _failedExecutions;

    public event EventHandler<WorkflowExecutionStartedEventArgs>? ExecutionStarted;
    public event EventHandler<ActionExecutionStartedEventArgs>? ActionStarted;
    public event EventHandler<ActionExecutionCompletedEventArgs>? ActionCompleted;

    public WorkflowEngine(ITimeProvider timeProvider, ILogger<WorkflowEngine> logger)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<WorkflowExecutionContext>> ExecuteAsync(Workflow workflow, WorkflowContext context, CancellationToken ct = default)
    {
        try
        {
            if (workflow is null) throw new ArgumentNullException(nameof(workflow));
            if (context is null) throw new ArgumentNullException(nameof(context));

            var executionId = Guid.NewGuid().ToString();
            var startTime = _timeProvider.UtcNow;

            _logger.LogInformation("Starting workflow execution: {ExecutionId} for workflow {WorkflowId}",
                executionId, workflow.Id);

            ExecutionStarted?.Invoke(this, new WorkflowExecutionStartedEventArgs(executionId, workflow.Id, context.TriggerType));

            var executionContext = new WorkflowExecutionContext(
                ExecutionId: executionId,
                WorkflowId: workflow.Id,
                Status: WorkflowStatus.Running,
                Context: context,
                CompletedActions: new List<ActionResult>(),
                CurrentAction: null,
                StartedAt: startTime);

            lock (_activeExecutions)
            {
                _activeExecutions[executionId] = executionContext;
            }

            Interlocked.Increment(ref _totalExecutions);

            // Check condition if present
            if (workflow.Condition != null)
            {
                var conditionResult = await EvaluateConditionAsync(workflow.Condition, context, ct).ConfigureAwait(false);
                if (conditionResult.IsFailure || !conditionResult.Value)
                {
                    executionContext = executionContext with
                    {
                        Status = WorkflowStatus.Completed,
                        CompletedAt = _timeProvider.UtcNow
                    };

                    lock (_activeExecutions)
                    {
                        _activeExecutions.Remove(executionId);
                    }

                    Interlocked.Increment(ref _successfulExecutions);
                    return Result<WorkflowExecutionContext>.Success(executionContext);
                }
            }

            // Execute actions
            foreach (var action in workflow.Actions)
            {
                if (ct.IsCancellationRequested)
                {
                    executionContext = executionContext with
                    {
                        Status = WorkflowStatus.Cancelled,
                        CompletedAt = _timeProvider.UtcNow
                    };

                    lock (_activeExecutions)
                    {
                        _activeExecutions.Remove(executionId);
                    }

                    return Result<WorkflowExecutionContext>.Failure("Workflow cancelled", ErrorType.Cancelled);
                }

                executionContext = executionContext with { CurrentAction = action };

                var actionResult = await ExecuteActionAsync(action, context, ct).ConfigureAwait(false);
                var completedAction = actionResult.IsSuccess && actionResult.Value is not null
                    ? actionResult.Value
                    : new ActionResult(
                        ActionId: action.Id,
                        Type: action.Type,
                        Status: ActionStatus.Failed,
                        Duration: TimeSpan.FromMilliseconds(100),
                        ErrorMessage: actionResult.Error);

                var completedActions = executionContext.CompletedActions.ToList();
                completedActions.Add(completedAction);
                executionContext = executionContext with { CompletedActions = completedActions };

                if (actionResult.IsFailure)
                {
                    executionContext = executionContext with
                    {
                        Status = WorkflowStatus.Failed,
                        CompletedAt = _timeProvider.UtcNow,
                        ErrorMessage = actionResult.Error
                    };

                    lock (_activeExecutions)
                    {
                        _activeExecutions.Remove(executionId);
                    }

                    Interlocked.Increment(ref _failedExecutions);
                    return Result<WorkflowExecutionContext>.Failure(actionResult.Error!, actionResult.ErrorType);
                }
            }

            executionContext = executionContext with
            {
                Status = WorkflowStatus.Completed,
                CompletedAt = _timeProvider.UtcNow,
                CurrentAction = null
            };

            lock (_activeExecutions)
            {
                _activeExecutions.Remove(executionId);
            }

            Interlocked.Increment(ref _successfulExecutions);

            _logger.LogInformation("Workflow execution completed: {ExecutionId}", executionId);
            return Result<WorkflowExecutionContext>.Success(executionContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow execution failed");
            Interlocked.Increment(ref _failedExecutions);
            return Result<WorkflowExecutionContext>.Failure($"Workflow execution failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<ActionResult>> ExecuteActionAsync(WorkflowAction action, WorkflowContext context, CancellationToken ct = default)
    {
        try
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            ActionStarted?.Invoke(this, new ActionExecutionStartedEventArgs(
                context.TriggeredBy ?? Guid.NewGuid().ToString(), action.Id, action.Type));

            var startTime = _timeProvider.UtcNow;

            // Check action condition
            if (action.Condition != null)
            {
                var conditionExpr = action.Condition.Expression;
                // Simplified condition evaluation - in real implementation use expression parser
                if (conditionExpr.Contains("false"))
                {
                    var skippedResult = new ActionResult(
                        ActionId: action.Id,
                        Type: action.Type,
                        Status: ActionStatus.Skipped,
                        Duration: TimeSpan.Zero);

                    ActionCompleted?.Invoke(this, new ActionExecutionCompletedEventArgs(
                        context.TriggeredBy ?? Guid.NewGuid().ToString(), action.Id, action.Type, true, TimeSpan.Zero));

                    return Result<ActionResult>.Success(skippedResult);
                }
            }

            // Handle delay action
            if (action.Type == ActionType.Delay && action.Parameters.TryGetValue("milliseconds", out var delayValue))
            {
                var delayMs = Convert.ToInt32(delayValue);
                await Task.Delay(delayMs, ct).ConfigureAwait(false);
            }

            // Check for custom handler
            IActionHandler? handler = null;
            lock (_actionHandlers)
            {
                _actionHandlers.TryGetValue(action.Type, out handler);
            }

            Dictionary<string, object>? output = null;
            if (handler != null)
            {
                var handlerResult = await handler.ExecuteAsync(action, context, ct).ConfigureAwait(false);
                if (handlerResult.IsFailure)
                {
                    return Result<ActionResult>.Failure(handlerResult.Error!, handlerResult.ErrorType);
                }
                output = handlerResult.Value;
            }

            var duration = _timeProvider.UtcNow - startTime;

            var result = new ActionResult(
                ActionId: action.Id,
                Type: action.Type,
                Status: ActionStatus.Completed,
                Duration: duration,
                Output: output);

            ActionCompleted?.Invoke(this, new ActionExecutionCompletedEventArgs(
                context.TriggeredBy ?? Guid.NewGuid().ToString(), action.Id, action.Type, true, duration));

            return Result<ActionResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Action execution failed: {ActionId}", action.Id);
            return Result<ActionResult>.Failure($"Action execution failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<bool>> EvaluateConditionAsync(WorkflowCondition condition, WorkflowContext context, CancellationToken ct = default)
    {
        try
        {
            if (condition is null) throw new ArgumentNullException(nameof(condition));

            var result = EvaluateConditionInternal(condition, context);
            return Task.FromResult(Result<bool>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Condition evaluation failed");
            return Task.FromResult(Result<bool>.Failure($"Condition evaluation failed: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> RegisterTriggerAsync(TriggerType type, TriggerListener listener, CancellationToken ct = default)
    {
        try
        {
            if (listener is null) throw new ArgumentNullException(nameof(listener));

            lock (_triggerListeners)
            {
                if (!_triggerListeners.TryGetValue(type, out var listeners))
                {
                    listeners = new List<TriggerListener>();
                    _triggerListeners[type] = listeners;
                }
                listeners.Add(listener);
            }

            _logger.LogDebug("Registered trigger listener: {ListenerId} for {TriggerType}", listener.Id, type);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register trigger listener");
            return Task.FromResult(Result.Failure($"Failed to register trigger: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> UnregisterTriggerAsync(TriggerType type, string listenerId, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(listenerId)) throw new ArgumentException("ListenerId cannot be empty", nameof(listenerId));

            lock (_triggerListeners)
            {
                if (_triggerListeners.TryGetValue(type, out var listeners))
                {
                    listeners.RemoveAll(l => l.Id == listenerId);
                }
            }

            _logger.LogDebug("Unregistered trigger listener: {ListenerId} for {TriggerType}", listenerId, type);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister trigger listener");
            return Task.FromResult(Result.Failure($"Failed to unregister trigger: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<IReadOnlyList<WorkflowExecutionContext>>> GetActiveExecutionsAsync(CancellationToken ct = default)
    {
        lock (_activeExecutions)
        {
            return Task.FromResult(Result<IReadOnlyList<WorkflowExecutionContext>>.Success(
                _activeExecutions.Values.ToList()));
        }
    }

    public Task<Result> CancelExecutionAsync(string executionId, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(executionId)) throw new ArgumentException("ExecutionId cannot be empty", nameof(executionId));

            lock (_activeExecutions)
            {
                if (!_activeExecutions.TryGetValue(executionId, out var context))
                {
                    return Task.FromResult(Result.Failure("Execution not found", ErrorType.NotFound));
                }

                _activeExecutions[executionId] = context with { Status = WorkflowStatus.Cancelled };
            }

            _logger.LogInformation("Cancelled workflow execution: {ExecutionId}", executionId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel execution");
            return Task.FromResult(Result.Failure($"Failed to cancel: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> RegisterActionHandlerAsync(ActionType type, IActionHandler handler, CancellationToken ct = default)
    {
        try
        {
            if (handler is null) throw new ArgumentNullException(nameof(handler));

            lock (_actionHandlers)
            {
                _actionHandlers[type] = handler;
            }

            _logger.LogDebug("Registered action handler for {ActionType}", type);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register action handler");
            return Task.FromResult(Result.Failure($"Failed to register handler: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> UnregisterActionHandlerAsync(ActionType type, CancellationToken ct = default)
    {
        try
        {
            lock (_actionHandlers)
            {
                _actionHandlers.Remove(type);
            }

            _logger.LogDebug("Unregistered action handler for {ActionType}", type);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister action handler");
            return Task.FromResult(Result.Failure($"Failed to unregister handler: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<ExecutionStatistics>> GetStatisticsAsync(CancellationToken ct = default)
    {
        try
        {
            var activeCount = 0;
            lock (_activeExecutions)
            {
                activeCount = _activeExecutions.Count;
            }

            var total = Interlocked.Read(ref _totalExecutions);
            var successful = Interlocked.Read(ref _successfulExecutions);
            var failed = Interlocked.Read(ref _failedExecutions);

            var avgTime = total > 0 ? 500.0 : 0.0; // Simplified average

            var stats = new ExecutionStatistics(
                TotalExecutions: (int)total,
                SuccessfulExecutions: (int)successful,
                FailedExecutions: (int)failed,
                ActiveExecutions: activeCount,
                AverageExecutionTimeMs: avgTime,
                ActionStatistics: new Dictionary<ActionType, ActionStatistics>());

            return Task.FromResult(Result<ExecutionStatistics>.Success(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statistics");
            return Task.FromResult(Result<ExecutionStatistics>.Failure($"Failed to get statistics: {ex.Message}", ErrorType.Internal));
        }
    }

    private bool EvaluateConditionInternal(WorkflowCondition condition, WorkflowContext context)
    {
        return condition.Type switch
        {
            ConditionType.Expression => EvaluateExpression(condition.Expression, context),
            ConditionType.All => condition.SubConditions?.All(c => EvaluateConditionInternal(c, context)) ?? true,
            ConditionType.Any => condition.SubConditions?.Any(c => EvaluateConditionInternal(c, context)) ?? false,
            ConditionType.Not => !(condition.SubConditions?.FirstOrDefault() is { } first) || !EvaluateConditionInternal(first, context),
            _ => true
        };
    }

    private bool EvaluateExpression(string expression, WorkflowContext context)
    {
        // Simplified expression evaluation
        // In real implementation, use a proper expression parser
        if (expression.Contains("=="))
        {
            var parts = expression.Split("==");
            if (parts.Length == 2)
            {
                var left = parts[0].Trim();
                var right = parts[1].Trim().Trim('\'', '"');

                if (context.Variables.TryGetValue(left, out var value))
                {
                    return value?.ToString() == right;
                }
            }
        }

        return !expression.Contains("false");
    }
}
