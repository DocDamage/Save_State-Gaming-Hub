using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace SaveState.Core.Services.Ai
{
    /// <summary>
    /// Refactored Ultimate AI Orchestrator that delegates to focused services.
    /// This implementation breaks down the monolithic orchestrator into manageable components.
    /// </summary>
    public class UltimateAiOrchestratorRefactored : IUltimateAiOrchestrator
    {
        private readonly ILogger _logger = Log.ForContext<UltimateAiOrchestratorRefactored>();
        private readonly PipelineOrchestrator _pipelineOrchestrator;
        private readonly CacheManager _cacheManager;
        private readonly ExperimentManager _experimentManager;
        private readonly MetricsService _metricsService;
        private readonly HealthMonitor _healthMonitor;
        private readonly UltimateOrchestratorConfig _config;

        public UltimateAiOrchestratorRefactored(UltimateOrchestratorConfig? config = null)
        {
            _config = config ?? new UltimateOrchestratorConfig();

            // Initialize focused services
            _metricsService = new MetricsService();
            _cacheManager = new CacheManager();
            _experimentManager = new ExperimentManager();
            _pipelineOrchestrator = new PipelineOrchestrator();
            _healthMonitor = new HealthMonitor(_metricsService, _cacheManager);

            // Build the standard pipeline
            BuildStandardPipeline();
        }

        /// <summary>
        /// Configures the orchestrator with the standard game pipeline.
        /// </summary>
        public void BuildStandardPipeline()
        {
            var provider = AiServiceProvider.Instance;

            // Stage 1: Governance & Safety
            _pipelineOrchestrator.AddStage("Governance", (context) =>
            {
                if (!provider.KillSwitch.IsFeatureAllowed("AiGeneration"))
                {
                    context.Errors.Add("AI Generation is globally disabled");
                    throw new OperationCanceledException("AI Generation disabled");
                }
                return Task.CompletedTask;
            }, new AiPipelineStage { Name = "Governance", Priority = 0, CriticalStage = true });

            // Stage 2: Intent Routing & Execution
            _pipelineOrchestrator.AddStage("CoreExecution", async (context) =>
            {
                var sessionId = context.Data.ContainsKey("SessionId") ? context.Data["SessionId"].ToString() : "default";
                var userId = context.Data.ContainsKey("UserId") ? context.Data["UserId"].ToString() : "anonymous";

                // Check cache first
                var cacheKey = CacheManager.GenerateCacheKey(context.Input);
                var cachedResult = _cacheManager.Get(cacheKey);
                if (cachedResult != null)
                {
                    _metricsService.RecordCacheHit();
                    context.Output = cachedResult;
                    return;
                }

                _metricsService.RecordCacheMiss();

                // Delegate to the Intent Router (The Brain)
                var result = await provider.IntentRouter.RouteAndProcessAsync(context.Input, sessionId!, userId!);
                context.Output = result;

                // Cache the result
                _cacheManager.Store(cacheKey, result, TimeSpan.FromMinutes(5));

            }, new AiPipelineStage { Name = "CoreExecution", Priority = 10, CriticalStage = true });

            // Stage 3: Post-Processing & Provenance
            _pipelineOrchestrator.AddStage("Provenance", async (context) =>
            {
                if (!string.IsNullOrEmpty(context.Output))
                {
                    // Record to ledger
                    // Record to ledger with improved agent identification and quality scoring
                    var agentId = context.Data.ContainsKey("AgentId") ? context.Data["AgentId"].ToString() : "Orchestrator";
                    var qualityScore = context.Data.ContainsKey("QualityScore") ? Convert.ToSingle(context.Data["QualityScore"]) : 1.0f;

                    await provider.ProvenanceLedger.RecordGenerationAsync(
                        agentId: agentId,
                        prompt: context.Input,
                        content: context.Output,
                        score: qualityScore,
                        quarantined: false
                    );
                }
            }, new AiPipelineStage { Name = "Provenance", Priority = 20 });
        }

        /// <summary>
        /// Executes the AI pipeline with the given input.
        /// </summary>
        public async Task<PipelineResult> ExecuteAsync(
            string input,
            Dictionary<string, object>? initialData = null,
            CancellationToken ct = default)
        {
            _metricsService.RecordRequestStart();
            var startTime = DateTime.UtcNow;

            try
            {
                // Check for experiment variants
                var userId = initialData?.ContainsKey("UserId") == true ? initialData["UserId"].ToString() : "anonymous";
                var experimentVariant = _experimentManager.GetAssignedVariant(userId!, "response_quality");
                if (experimentVariant != null && initialData != null)
                {
                    initialData["ExperimentVariant"] = experimentVariant;
                }

                // Execute pipeline
                var result = await _pipelineOrchestrator.ExecuteAsync(input, initialData, ct);

                // Record metrics
                var latency = (DateTime.UtcNow - startTime).TotalMilliseconds;
                if (result.Status == PipelineStatus.Success)
                {
                    _metricsService.RecordRequestSuccess((long)latency);
                }
                else
                {
                    _metricsService.RecordRequestFailure();
                }

                // Add observability event
                _metricsService.AddObservabilityEvent(new ObservabilityData
                {
                    EventType = "pipeline_execution",
                    Timestamp = DateTime.UtcNow,
                    Data = new Dictionary<string, object>
                    {
                        ["input_length"] = input.Length,
                        ["output_length"] = result.Output?.Length ?? 0,
                        ["latency_ms"] = latency,
                        ["status"] = result.Status.ToString(),
                        ["experiment_variant"] = experimentVariant ?? "none"
                    }
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Pipeline execution failed");
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

        /// <summary>
        /// Executes with fallback mechanisms.
        /// </summary>
        public async Task<PipelineResult> ExecuteWithFallbackAsync(
            string input,
            Func<string, Task<string>> fallbackGenerator,
            CancellationToken ct = default)
        {
            _metricsService.RecordFallbackUsed();
            return await _pipelineOrchestrator.ExecuteWithFallbackAsync(input, fallbackGenerator, null, ct);
        }

        // Cache Management
        public void EnableCache(string keyPattern, TimeSpan ttl) => _cacheManager.EnableCache(keyPattern, ttl);
        public void InvalidateCache(string keyPattern) => _cacheManager.InvalidateCache(keyPattern);
        public void ClearCache() => _cacheManager.ClearCache();

        // Experiment Management
        public void RegisterExperiment(ExperimentConfig config) => _experimentManager.RegisterExperiment(config);
        public void EndExperiment(string experimentId) => _experimentManager.EndExperiment(experimentId);
        public string? GetAssignedVariant(string userId, string experimentId) => _experimentManager.GetAssignedVariant(userId, experimentId);

        // Observability
        public void AddObserver(ObservabilityHandler handler) => _metricsService.AddObserver(handler);
        public OrchestratorMetrics GetMetrics() => _metricsService.GetMetrics();
        public List<ObservabilityData> GetRecentEvents(int count = 100) => _metricsService.GetRecentEvents(count);

        // Health Monitoring
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
