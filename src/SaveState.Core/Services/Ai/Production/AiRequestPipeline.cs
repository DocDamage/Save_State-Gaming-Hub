using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.Governance;
using SaveState.Core.Services.Ai.Memory;
using SaveState.Core.Services.Ai.Orchestration;
using SaveState.Core.Services.Ai.Resilience;
using SaveState.Core.Services.Ai.Safety;
using SaveState.Core.Services.Ai.Telemetry;
using SaveState.Core.Services.Ai.Validation;
using SaveState.Core.Services.Ai;

namespace SaveState.Core.Services.Ai.Production
{
    public interface IAiRequestPipeline
    {
        Task<ProductionAiResponse> ProcessAsync(ProductionAiRequest request, CancellationToken ct = default);
    }

    public class AiRequestPipeline : IAiRequestPipeline
    {
        private readonly ILlmService _llmService;
        private readonly IEdgeCaseHandler _edgeCaseHandler;
        private readonly IAiResponseCache _cache;
        private readonly IAiConversationManager _conversationManager;
        private readonly IAiPromptAssembler _promptAssembler;
        private readonly IAiFallbackGenerator _fallbackGenerator;
        private readonly IAiStatisticsCollector _statsCollector;
        private readonly IEnhancedIntentClassifier? _intentClassifier;
        private readonly IEnhancedOutputValidator? _validator;
        private readonly IEnhancedShortTermMemory? _memory;
        private readonly IResilientAiService? _resilientService;
        private readonly IGlobalKillSwitch? _killSwitch;
        private readonly IPolicyGate? _policyGate;
        private readonly IProvenanceLedger? _provenanceLedger;
        private readonly IEnumerable<ISpecialistAgent> _specialistAgents;
        private readonly ProductionAiConfig _config;

        public AiRequestPipeline(
            ILlmService llmService,
            IEdgeCaseHandler edgeCaseHandler,
            IAiResponseCache cache,
            IAiConversationManager conversationManager,
            IAiPromptAssembler promptAssembler,
            IAiFallbackGenerator fallbackGenerator,
            IAiStatisticsCollector statsCollector,
            ProductionAiConfig config,
            IEnhancedIntentClassifier? intentClassifier = null,
            IEnhancedOutputValidator? validator = null,
            IEnhancedShortTermMemory? memory = null,
            IResilientAiService? resilientService = null,
            IGlobalKillSwitch? killSwitch = null,
            IPolicyGate? policyGate = null,
            IProvenanceLedger? provenanceLedger = null,
            IEnumerable<ISpecialistAgent>? specialistAgents = null)
        {
            _llmService = llmService;
            _edgeCaseHandler = edgeCaseHandler;
            _cache = cache;
            _conversationManager = conversationManager;
            _promptAssembler = promptAssembler;
            _fallbackGenerator = fallbackGenerator;
            _statsCollector = statsCollector;
            _config = config;
            _intentClassifier = intentClassifier;
            _validator = validator;
            _memory = memory;
            _resilientService = resilientService;
            _killSwitch = killSwitch;
            _policyGate = policyGate;
            _provenanceLedger = provenanceLedger;
            _specialistAgents = specialistAgents ?? Enumerable.Empty<ISpecialistAgent>();
        }

        public async Task<ProductionAiResponse> ProcessAsync(ProductionAiRequest request, CancellationToken ct = default)
        {
            var startTime = DateTime.UtcNow;
            var options = request.Options ?? new ProductionAiRequestOptions();

            var response = new ProductionAiResponse
            {
                RequestId = request.Id,
                DebugInfo = _config.EnableDebugInfo ? new ProductionAiDebugInfo() : null
            };

            try
            {
                // ===== STAGE 0: Kill Switch =====
                if (_killSwitch != null && !_killSwitch.IsFeatureAllowed("AiGeneration"))
                {
                    return CreateErrorResponse(response, "AI Generation is currently disabled by KillSwitch.", startTime);
                }

                // ===== STAGE 1: Input Sanitization =====
                var stageStart = DateTime.UtcNow;
                var sanitizedResult = await _edgeCaseHandler.SanitizeInputAsync(request.Prompt);
                AddDebugStage(response, "Sanitization", stageStart, true);

                if (sanitizedResult.DetectedEdgeCases.Any(e => e.Severity >= 0.8f))
                {
                    return CreateErrorResponse(response, "Input was rejected due to critical edge cases", startTime);
                }
                var sanitizedInput = sanitizedResult.Sanitized;

                // ===== STAGE 2: Cache Check =====
                stageStart = DateTime.UtcNow;
                if (options.AllowCaching)
                {
                    var cached = _cache.Get(sanitizedInput, request.Context);
                    if (cached != null)
                    {
                        AddDebugStage(response, "CacheCheck", stageStart, true);
                        cached.Duration = DateTime.UtcNow - startTime;
                        cached.UsedCache = true;
                        _statsCollector.RecordRequest(true, (float)cached.Duration.TotalMilliseconds, cached.Confidence, cached.AgentUsed, cached.IntentDetected, true, false, false);
                        return cached;
                    }
                }
                AddDebugStage(response, "CacheCheck", stageStart, true);

                // ===== STAGE 3: Intent Classification =====
                stageStart = DateTime.UtcNow;
                string? detectedIntent = null;
                if (_intentClassifier != null)
                {
                    var intentResult = await _intentClassifier.ClassifyAsync(sanitizedInput);
                    detectedIntent = intentResult.PrimaryIntent.ToString();
                    response.IntentDetected = detectedIntent;
                    response.Confidence = intentResult.PrimaryConfidence;
                }
                AddDebugStage(response, "IntentClassification", stageStart, true);

                // ===== STAGE 4: Memory Context =====
                stageStart = DateTime.UtcNow;
                string? memoryContext = null;
                if (options.EnableMemory && _memory != null)
                {
                    memoryContext = await _memory.BuildContextWindowAsync(options.MaxTokens / 4);
                }
                AddDebugStage(response, "MemoryContext", stageStart, true);

                // ===== STAGE 5: Prompt Assembly =====
                stageStart = DateTime.UtcNow;
                var history = _conversationManager.GetHistory(request.ConversationId ?? "");
                var fullPrompt = _promptAssembler.AssemblePrompt(sanitizedInput, request, memoryContext, history);
                var systemPrompt = _promptAssembler.AssembleSystemPrompt(request, detectedIntent, _config);
                AddDebugStage(response, "PromptAssembly", stageStart, true);

                // ===== STAGE 6: LLM Call =====
                stageStart = DateTime.UtcNow;
                string? llmResponse = null;
                if (_resilientService != null)
                {
                    var res = await _resilientService.ExecuteAsync(new AiRequest { Prompt = fullPrompt, SystemPrompt = systemPrompt });
                    if (res.Success) llmResponse = res.Content;
                }
                else
                {
                    llmResponse = await _llmService.CompleteAsync(fullPrompt, systemPrompt);
                }
                AddDebugStage(response, "LlmCall", stageStart, llmResponse != null);

                if (string.IsNullOrEmpty(llmResponse))
                {
                    llmResponse = _fallbackGenerator.GenerateFallbackResponse(sanitizedInput, detectedIntent);
                    response.UsedFallback = true;
                }

                // ===== STAGE 7: Validation =====
                stageStart = DateTime.UtcNow;
                bool validationFailed = false;
                if (options.EnableValidation && _validator != null)
                {
                    var valRes = await _validator.ValidateAndRepairAsync(llmResponse, new EnhancedValidationContext { AllowAutoRepair = true });
                    if (valRes.WasRepaired) llmResponse = valRes.RepairedContent;
                    validationFailed = !valRes.IsValid;
                }
                AddDebugStage(response, "Validation", stageStart, true);

                // ===== STAGE 7.5: Provenance Recording =====
                if (_provenanceLedger != null && !string.IsNullOrEmpty(llmResponse))
                {
                    await _provenanceLedger.RecordGenerationAsync(
                        agentId: response.AgentUsed ?? "GenericLLM",
                        prompt: sanitizedInput,
                        content: llmResponse,
                        score: response.Confidence,
                        quarantined: false
                    );
                }

                // ===== STAGE 8: Memory Record =====
                stageStart = DateTime.UtcNow;
                if (options.EnableMemory && _memory != null && !string.IsNullOrEmpty(llmResponse))
                {
                    await _memory.AddAsync(sanitizedInput, llmResponse);
                }
                AddDebugStage(response, "MemoryRecord", stageStart, true);

                // ===== STAGE 9: Update Conversation =====
                if (!string.IsNullOrEmpty(request.ConversationId))
                {
                    _conversationManager.AddTurn(request.ConversationId, "user", sanitizedInput);
                    _conversationManager.AddTurn(request.ConversationId, "assistant", llmResponse!);
                }

                // Finalize
                response.Success = true;
                response.Content = llmResponse;
                response.Duration = DateTime.UtcNow - startTime;
                response.Metadata.LatencyMs = (float)response.Duration.TotalMilliseconds;

                _statsCollector.RecordRequest(true, response.Metadata.LatencyMs, response.Confidence, response.AgentUsed, response.IntentDetected, false, validationFailed, sanitizedResult.DetectedEdgeCases.Any());

                if (options.AllowCaching) _cache.Set(sanitizedInput, request.Context, response);

                return response;
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(response, $"Pipeline error: {ex.Message}", startTime);
            }
        }

        private void AddDebugStage(ProductionAiResponse response, string name, DateTime start, bool success)
        {
            if (response.DebugInfo != null)
            {
                response.DebugInfo.Stages.Add(new PipelineStageDebug
                {
                    Name = name,
                    DurationMs = (float)(DateTime.UtcNow - start).TotalMilliseconds,
                    Success = success
                });
            }
        }

        private ProductionAiResponse CreateErrorResponse(ProductionAiResponse response, string error, DateTime startTime)
        {
            response.Success = false;
            response.Errors.Add(error);
            response.Duration = DateTime.UtcNow - startTime;
            _statsCollector.RecordRequest(false, (float)response.Duration.TotalMilliseconds, 0, null, null, false, false, false);
            return response;
        }
    }
}
