using System;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.Governance;
using SaveState.Core.Services.Ai.Orchestration;
using SaveState.Core.Services.Ai.Safety;
using Serilog;

namespace SaveState.Core.Services.Ai
{
    public class AiPipelineBuilder : IAiPipelineBuilder
    {
        private readonly ILogger _logger = Log.ForContext<AiPipelineBuilder>();
        private readonly IPipelineOrchestrator _pipelineOrchestrator;
        private readonly IAiCacheCoordinator _cacheManager;
        private readonly IAiMetricsAggregator _metricsService;
        private readonly IGlobalKillSwitch? _killSwitch;
        private readonly IIntentRouter? _intentRouter;
        private readonly IProvenanceLedger? _provenanceLedger;

        public AiPipelineBuilder(
            IPipelineOrchestrator pipelineOrchestrator,
            IAiCacheCoordinator cacheManager,
            IAiMetricsAggregator metricsService,
            IGlobalKillSwitch? killSwitch = null,
            IIntentRouter? intentRouter = null,
            IProvenanceLedger? provenanceLedger = null)
        {
            _pipelineOrchestrator = pipelineOrchestrator ?? throw new ArgumentNullException(nameof(pipelineOrchestrator));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
            _killSwitch = killSwitch;
            _intentRouter = intentRouter;
            _provenanceLedger = provenanceLedger;
        }

        public void BuildStandardPipeline()
        {
            // Stage 1: Governance & Safety
            _pipelineOrchestrator.AddStage("Governance", (context) =>
            {
                if (_killSwitch != null && !_killSwitch.IsFeatureAllowed("AiGeneration"))
                {
                    context.Errors.Add("AI Generation is globally disabled");
                    throw new OperationCanceledException("AI Generation disabled");
                }
                return Task.CompletedTask;
            }, new AiPipelineStage { Name = "Governance", Priority = 0, CriticalStage = true });

            // Stage 2: Intent Routing & Execution
            _pipelineOrchestrator.AddStage("CoreExecution", async (context) =>
            {
                var sessionId = context.Data.SessionId ?? "default";
                var userId = context.Data.UserId ?? "anonymous";

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

                // Delegate to the Intent Router (The Brain) if available
                if (_intentRouter != null)
                {
                    var result = await _intentRouter.RouteAndProcessAsync(context.Input, sessionId!, userId!);
                    context.Output = result;
                }
                else
                {
                    _logger.Warning("IntentRouter not injected, using fallback");
                    context.Output = "Intent routing not configured.";
                }

                if (!string.IsNullOrEmpty(context.Output))
                {
                    // Cache the result
                    _cacheManager.Store(cacheKey, context.Output, TimeSpan.FromMinutes(5));
                }

            }, new AiPipelineStage { Name = "CoreExecution", Priority = 10, CriticalStage = true });

            // Stage 3: Post-Processing & Provenance
            _pipelineOrchestrator.AddStage("Provenance", async (context) =>
            {
                if (!string.IsNullOrEmpty(context.Output) && _provenanceLedger != null)
                {
                    // Record to ledger with improved agent identification and quality scoring
                    var agentId = context.Data.AgentId ?? "Orchestrator";
                    var qualityScore = context.Data.QualityScore ?? 1.0f;

                    await _provenanceLedger.RecordGenerationAsync(
                        agentId: agentId,
                        prompt: context.Input,
                        content: context.Output,
                        score: qualityScore,
                        quarantined: false
                    );
                }
            }, new AiPipelineStage { Name = "Provenance", Priority = 20 });
        }
    }
}
