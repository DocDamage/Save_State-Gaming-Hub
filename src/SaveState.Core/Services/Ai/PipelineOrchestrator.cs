using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace SaveState.Core.Services.Ai;

/// <summary>
/// Core pipeline orchestrator responsible for managing and executing AI processing pipelines.
/// Handles stage management, execution flow, and fallback mechanisms.
/// </summary>
public class PipelineOrchestrator
{
    private readonly ILogger _logger = Log.ForContext<PipelineOrchestrator>();
    private readonly ConcurrentDictionary<string, (PipelineStageHandler Handler, AiPipelineStage Config)> _stages = new();
    private readonly ConcurrentDictionary<string, PipelineCondition> _conditions = new();

    /// <summary>
    /// Adds a pipeline stage with the specified handler and configuration.
    /// </summary>
    public void AddStage(string name, PipelineStageHandler handler, AiPipelineStage? config = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Stage name cannot be empty", nameof(name));

        config ??= new AiPipelineStage { Name = name };
        _stages[name] = (handler, config);
        _logger.Information("Added pipeline stage: {StageName} (Priority: {Priority})", name, config.Priority);
    }

    /// <summary>
    /// Removes a pipeline stage by name.
    /// </summary>
    public void RemoveStage(string name)
    {
        if (_stages.TryRemove(name, out _))
        {
            _logger.Information("Removed pipeline stage: {StageName}", name);
        }
    }

    /// <summary>
    /// Sets a condition for when a pipeline stage should execute.
    /// </summary>
    public void SetStageCondition(string stageName, PipelineCondition condition)
    {
        _conditions[stageName] = condition;
        _logger.Debug("Set condition for stage: {StageName}", stageName);
    }

    /// <summary>
    /// Executes the pipeline with the given input and context data.
    /// </summary>
    public async Task<PipelineResult> ExecuteAsync(
        string input,
        Dictionary<string, object>? contextData = null,
        CancellationToken cancellationToken = default)
    {
        var context = new PipelineContext
        {
            Input = input,
            Data = contextData ?? new Dictionary<string, object>(),
            Errors = new List<string>(),
            Warnings = new List<string>(),
            StartTime = DateTime.UtcNow
        };

        var result = new PipelineResult
        {
            RequestId = Guid.NewGuid().ToString(),
            Input = input,
            ContextData = context.Data
        };

        try
        {
            // Execute stages in priority order
            var orderedStages = _stages
                .Where(kvp => ShouldExecuteStage(kvp.Key, context))
                .OrderBy(kvp => kvp.Value.Config.Priority)
                .ToList();

            foreach (var stage in orderedStages)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result.Status = PipelineStatus.Cancelled;
                    break;
                }

                try
                {
                    await stage.Value.Handler(context);
                    _logger.Debug("Executed pipeline stage: {StageName}", stage.Key);
                }
                catch (Exception ex) when (stage.Value.Config.CriticalStage)
                {
                    _logger.Error(ex, "Critical pipeline stage failed: {StageName}", stage.Key);
                    context.Errors.Add($"Critical stage '{stage.Key}' failed: {ex.Message}");
                    result.Success = false;
                    result.Status = PipelineStatus.Failed;
                    result.Error = ex.Message;
                    result.ExecutionTimeMs = (DateTime.UtcNow - context.StartTime).TotalMilliseconds;
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Non-critical pipeline stage failed: {StageName}", stage.Key);
                    context.Warnings.Add($"Stage '{stage.Key}' failed: {ex.Message}");
                }
            }

            result.Success = !context.Errors.Any();
            result.Status = context.Errors.Any() ? PipelineStatus.PartialSuccess : PipelineStatus.Success;
            result.Output = context.Output;
            result.Errors = context.Errors;
            result.Warnings = context.Warnings;
            result.ExecutionTimeMs = (DateTime.UtcNow - context.StartTime).TotalMilliseconds;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Pipeline execution failed");
            result.Status = PipelineStatus.Failed;
            result.Error = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Executes the pipeline with fallback mechanisms.
    /// </summary>
    public async Task<PipelineResult> ExecuteWithFallbackAsync(
        string input,
        Func<string, Task<string>> fallbackGenerator,
        Dictionary<string, object>? contextData = null,
        CancellationToken cancellationToken = default)
    {
        var primaryResult = await ExecuteAsync(input, contextData, cancellationToken);

        if (primaryResult.Status == PipelineStatus.Success && !string.IsNullOrEmpty(primaryResult.Output))
        {
            return primaryResult;
        }

        _logger.Warning("Primary pipeline failed, attempting fallback for input: {InputPrefix}",
            input.Length > 50 ? input.Substring(0, 50) + "..." : input);

        try
        {
            var fallbackOutput = await fallbackGenerator(input);
            return new PipelineResult
            {
                RequestId = Guid.NewGuid().ToString(),
                Input = input,
                Output = fallbackOutput,
                Status = PipelineStatus.SuccessWithFallback,
                ContextData = contextData,
                ExecutionTimeMs = primaryResult.ExecutionTimeMs,
                Warnings = new List<string> { "Used fallback generation" }
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Fallback generation also failed");
            return new PipelineResult
            {
                RequestId = primaryResult.RequestId,
                Input = input,
                Status = PipelineStatus.Failed,
                Error = $"Both primary and fallback failed. Primary: {primaryResult.Error ?? "Unknown"}. Fallback: {ex.Message}",
                ContextData = contextData,
                ExecutionTimeMs = primaryResult.ExecutionTimeMs
            };
        }
    }

    /// <summary>
    /// Gets all registered stage names.
    /// </summary>
    public IEnumerable<string> GetStageNames() => _stages.Keys;

    /// <summary>
    /// Gets configuration for a specific stage.
    /// </summary>
    public AiPipelineStage? GetStageConfig(string stageName)
    {
        return _stages.TryGetValue(stageName, out var stage) ? stage.Config : null;
    }

        private bool ShouldExecuteStage(string stageName, PipelineContext context)
        {
            if (!_conditions.TryGetValue(stageName, out var condition))
                return true;

            try
            {
                return condition(context);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Stage condition evaluation failed for: {StageName}", stageName);
                return false;
            }
        }

        /// <summary>
        /// Delegate for pipeline stage handlers.
        /// </summary>
        public delegate Task PipelineStageHandler(PipelineContext context);

        /// <summary>
        /// Delegate for pipeline stage conditions.
        /// </summary>
        public delegate bool PipelineCondition(PipelineContext context);
}
