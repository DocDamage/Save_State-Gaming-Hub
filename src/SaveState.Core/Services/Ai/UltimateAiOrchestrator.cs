using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.Safety;
using SaveState.Core.Services.Ai.Orchestration;
using SaveState.Core.Services.Ai.Governance;
using Serilog;

namespace SaveState.Core.Services.Ai
{
    /// <summary>
    /// Ultimate AI Orchestrator that delegates to focused services.
    /// This implementation acts as a Facade for the underlying pipeline, cache, metrics, and health components.
    /// </summary>
    public class UltimateAiOrchestrator : IUltimateAiOrchestrator
    {
        private readonly ILogger _logger = Log.ForContext<UltimateAiOrchestrator>();
        private readonly IPipelineOrchestrator _pipelineOrchestrator;
        private readonly IAiCacheCoordinator _cacheManager;
        private readonly IAiExperimentCoordinator _experimentManager;
        private readonly IAiMetricsAggregator _metricsService;
        private readonly IAiHealthCoordinator _healthMonitor;
        private readonly IAiPipelineBuilder _pipelineBuilder;

        public UltimateAiOrchestrator(
            IPipelineOrchestrator pipelineOrchestrator,
            IAiCacheCoordinator cacheManager,
            IAiExperimentCoordinator experimentManager,
            IAiMetricsAggregator metricsService,
            IAiHealthCoordinator healthMonitor,
            IAiPipelineBuilder pipelineBuilder)
        {
            _pipelineOrchestrator = pipelineOrchestrator ?? throw new ArgumentNullException(nameof(pipelineOrchestrator));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _experimentManager = experimentManager ?? throw new ArgumentNullException(nameof(experimentManager));
            _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
            _healthMonitor = healthMonitor ?? throw new ArgumentNullException(nameof(healthMonitor));
            _pipelineBuilder = pipelineBuilder ?? throw new ArgumentNullException(nameof(pipelineBuilder));

            // Build the standard pipeline on initialization
            _pipelineBuilder.BuildStandardPipeline();
        }

        public async Task<PipelineResult> ExecuteAsync(
            string input,
            PipelineContextData? initialData = null,
            CancellationToken ct = default)
        {
            _metricsService.RecordRequestStart();
            var startTime = DateTime.UtcNow;

            try
            {
                // Create context data if not provided
                var contextData = initialData ?? new PipelineContextData();

                // Check for experiment variants
                var userId = contextData.UserId ?? "anonymous";
                var experimentVariant = _experimentManager.GetAssignedVariant(userId, "response_quality");
                if (experimentVariant != null)
                {
                    contextData.ExperimentVariant = experimentVariant;
                }

                // Execute pipeline
                var result = await _pipelineOrchestrator.ExecuteAsync(input, contextData, ct);

                // Record metrics
                var latency = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                if (result.Status == PipelineStatus.Success || result.Status == PipelineStatus.PartialSuccess)
                {
                    _metricsService.RecordRequestSuccess(latency);
                }
                else if (result.Status == PipelineStatus.Failed)
                {
                    _metricsService.RecordRequestFailure();
                }

                // Add observability event
                _metricsService.AddObservabilityEvent(new ObservabilityData
                {
                    RequestId = result.RequestId,
                    EventType = "pipeline_execution",
                    Timestamp = DateTime.UtcNow,
                    Latency = TimeSpan.FromMilliseconds(latency),
                    Data = new EventData
                    {
                        InputLength = input.Length,
                        OutputLength = result.Output?.Length ?? 0,
                        Status = result.Status.ToString(),
                        ExperimentVariant = experimentVariant ?? "none",
                        LatencyMs = latency
                    }
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ultimate pipeline execution failed");
                _metricsService.RecordRequestFailure();

                return new PipelineResult
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Input = input,
                    Status = PipelineStatus.Failed,
                    Error = ex.Message,
                    ExecutionTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }
        }

        public async Task<PipelineResult> ExecuteWithFallbackAsync(
            string input,
            Func<string, Task<string>> fallbackGenerator,
            CancellationToken ct = default)
        {
            _metricsService.RecordFallbackUsed();
            return await _pipelineOrchestrator.ExecuteWithFallbackAsync(input, fallbackGenerator, null, ct);
        }

        // Cache Management (delegated)
        public void EnableCache(string keyPattern, TimeSpan ttl) => _cacheManager.EnableCache(keyPattern, ttl);
        public void InvalidateCache(string keyPattern) => _cacheManager.InvalidateCache(keyPattern);
        public void ClearCache() => _cacheManager.ClearCache();

        // Experiment Management (delegated)
        public void RegisterExperiment(ExperimentConfig config) => _experimentManager.RegisterExperiment(config);
        public void EndExperiment(string experimentId) => _experimentManager.EndExperiment(experimentId);
        public string? GetAssignedVariant(string userId, string experimentId) => _experimentManager.GetAssignedVariant(userId, experimentId);

        // Observability (delegated)
        public void AddObserver(ObservabilityHandler handler) => _metricsService.AddObserver(handler);
        public OrchestratorMetrics GetMetrics() => _metricsService.GetMetrics();
        public List<ObservabilityData> GetRecentEvents(int count = 100) => _metricsService.GetRecentEvents(count);

        // Health Monitoring (delegated)
        public Task<HealthCheckResult> CheckHealthAsync() => _healthMonitor.CheckHealthAsync();
        public void EnableSelfHealing(bool enable) => _healthMonitor.EnableSelfHealing(enable);

        // Pipeline Management (delegated)
        public void AddStage(string name, PipelineStageHandler handler, AiPipelineStage? config = null) =>
            _pipelineOrchestrator.AddStage(name, handler, config);
        public void RemoveStage(string name) => _pipelineOrchestrator.RemoveStage(name);
        public void SetStageCondition(string stageName, PipelineCondition condition) =>
            _pipelineOrchestrator.SetStageCondition(stageName, condition);
    }
}
