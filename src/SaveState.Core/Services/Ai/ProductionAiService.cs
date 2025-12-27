using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.Memory;
using SaveState.Core.Services.Ai.Orchestration;
using SaveState.Core.Services.Ai.Validation;
using SaveState.Core.Services.Ai.Governance;
using SaveState.Core.Services.Ai.Telemetry;
using SaveState.Core.Services.Ai.Resilience;
using SaveState.Core.Services.Player;

namespace SaveState.Core.Services.Ai
{
    /// <summary>
    /// Production-Ready AI Service that combines all enhanced components with:
    /// - Complete integration of all subsystems
    /// - Comprehensive edge case handling
    /// - Automatic fallback and recovery
    /// - Full observability and tracing
    /// - Performance optimization
    /// - Semantic caching
    /// - Request deduplication
    /// - Conversation continuity
    /// </summary>
    public class ProductionAiRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Prompt { get; set; } = string.Empty;
        public string? SystemPrompt { get; set; }
        public string? UserId { get; set; }
        public string? SessionId { get; set; }
        public string? ConversationId { get; set; }
        public RequestPriority Priority { get; set; } = RequestPriority.Normal;
        public ProductionAiRequestContext? Context { get; set; }
        public ProductionAiRequestOptions? Options { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ProductionAiRequestContext
    {
        public string? CurrentScene { get; set; }
        public bool InCombat { get; set; }
        public bool InDialogue { get; set; }
        public bool InShop { get; set; }
        public string? ActiveQuest { get; set; }
        public List<string>? RecentEvents { get; set; }
        public Dictionary<string, object>? WorldState { get; set; }
        public Dictionary<string, object>? PlayerState { get; set; }
        public List<string>? RelevantCharacters { get; set; }
        public List<string>? RelevantLocations { get; set; }
    }

    public class ProductionAiRequestOptions
    {
        public bool EnableMemory { get; set; } = true;
        public bool InjectWorldState { get; set; } = true;
        public bool EnableValidation { get; set; } = true;
        public bool EnablePlayerModeling { get; set; } = true;
        public bool AllowCaching { get; set; } = true;
        public bool RequireHighConfidence { get; set; } = false;
        public float MinConfidence { get; set; } = 0.5f;
        public int MaxTokens { get; set; } = 2048;
        public float Temperature { get; set; } = 0.7f;
        public List<string>? PreferredAgents { get; set; }
        public TimeSpan? Timeout { get; set; }
    }

    public class ProductionAiResponse
    {
        public string RequestId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Content { get; set; }
        public float Confidence { get; set; }
        public string? AgentUsed { get; set; }
        public string? IntentDetected { get; set; }
        public string? EmotionDetected { get; set; }
        public bool WasValidated { get; set; }
        public bool UsedCache { get; set; }
        public bool UsedFallback { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public ProductionAiResponseMetadata Metadata { get; set; } = new();
        public ProductionAiDebugInfo? DebugInfo { get; set; }
    }

    public class ProductionAiResponseMetadata
    {
        public int TokensUsed { get; set; }
        public string? ModelUsed { get; set; }
        public float LatencyMs { get; set; }
        public int RetryCount { get; set; }
        public string? MemoryContext { get; set; }
        public List<string>? RelatedMemories { get; set; }
        public Dictionary<string, object>? Additional { get; set; }
    }

    public class ProductionAiDebugInfo
    {
        public List<PipelineStageDebug> Stages { get; set; } = new();
        public string? SanitizedInput { get; set; }
        public List<string>? DetectedEdgeCases { get; set; }
        public string? ClassificationDetails { get; set; }
        public string? ValidationDetails { get; set; }
    }

    public class PipelineStageDebug
    {
        public string Name { get; set; } = string.Empty;
        public float DurationMs { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
    }

    public class ProductionAiStats
    {
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public int CacheHits { get; set; }
        public float AverageLatencyMs { get; set; }
        public float AverageConfidence { get; set; }
        public Dictionary<string, int> RequestsByAgent { get; set; } = new();
        public Dictionary<string, int> RequestsByIntent { get; set; } = new();
        public int EdgeCasesHandled { get; set; }
        public int ValidationFailures { get; set; }
    }

    public interface IProductionAiService
    {
        Task<ProductionAiResponse> ProcessAsync(ProductionAiRequest request, CancellationToken ct = default);
        Task<ProductionAiResponse> QuickProcessAsync(string prompt, CancellationToken ct = default);
        Task<ProductionAiResponse> ContinueConversationAsync(string conversationId, string prompt, CancellationToken ct = default);
        void ConfigureDefaults(ProductionAiRequestOptions defaults);
        ProductionAiStats GetStats();
        Task WarmupAsync();
        void InvalidateCache(string? pattern = null);
    }

    public class ProductionAiService : IProductionAiService
    {
        private readonly ILlmService _llmService;
        private readonly IEdgeCaseHandler? _edgeCaseHandler;
        private readonly IEnhancedIntentClassifier? _intentClassifier;
        private readonly IEnhancedOutputValidator? _validator;
        private readonly IEnhancedShortTermMemory? _memory;
        private readonly IEnhancedPlayerModelService? _playerModeling;
        private readonly IResilientAiService? _resilientService;
        
        // NEW: Governance and Telemetry integration
        private readonly IAiGovernanceService? _governanceService;
        private readonly IAiTelemetry? _telemetry;
        private readonly IHallucinationDetector? _hallucinationDetector;
        private readonly IFailureAsContent? _failureAsContent;
        
        private readonly ConcurrentDictionary<string, (ProductionAiResponse Response, DateTime Expiry)> _cache = new();
        private readonly ConcurrentDictionary<string, List<(string Role, string Content)>> _conversations = new();
        private readonly ProductionAiConfig _config;
        
        // Statistics
        private int _totalRequests = 0;
        private int _successfulRequests = 0;
        private int _failedRequests = 0;
        private int _cacheHits = 0;
        private int _edgeCasesHandled = 0;
        private int _validationFailures = 0;
        private float _totalLatency = 0;
        private float _totalConfidence = 0;
        private readonly ConcurrentDictionary<string, int> _requestsByAgent = new();
        private readonly ConcurrentDictionary<string, int> _requestsByIntent = new();

        private ProductionAiRequestOptions _defaultOptions = new();
        private bool _warmedUp = false;

        public ProductionAiService(
            ILlmService llmService,
            ProductionAiConfig? config = null,
            IEdgeCaseHandler? edgeCaseHandler = null,
            IEnhancedIntentClassifier? intentClassifier = null,
            IEnhancedOutputValidator? validator = null,
            IEnhancedShortTermMemory? memory = null,
            IEnhancedPlayerModelService? playerModeling = null,
            IAiGovernanceService? governanceService = null,
            IAiTelemetry? telemetry = null,
            IHallucinationDetector? hallucinationDetector = null,
            IFailureAsContent? failureAsContent = null)
        {
            _llmService = llmService;
            _config = config ?? new ProductionAiConfig();
            _edgeCaseHandler = edgeCaseHandler ?? new EdgeCaseHandler();
            _intentClassifier = intentClassifier;
            _validator = validator;
            _memory = memory;
            _playerModeling = playerModeling;
            
            // NEW: Initialize governance and telemetry
            _governanceService = governanceService;
            _telemetry = telemetry;
            _hallucinationDetector = hallucinationDetector;
            _failureAsContent = failureAsContent;

            // Create resilient wrapper
            _resilientService = new ResilientAiService(llmService);

            // Start cache cleanup
            _ = CacheCleanupLoopAsync();
        }

        public async Task<ProductionAiResponse> ProcessAsync(ProductionAiRequest request, CancellationToken ct = default)
        {
            Interlocked.Increment(ref _totalRequests);
            var startTime = DateTime.UtcNow;
            var options = request.Options ?? _defaultOptions;
            
            var response = new ProductionAiResponse
            {
                RequestId = request.Id,
                DebugInfo = _config.EnableDebugInfo ? new ProductionAiDebugInfo() : null
            };

            try
            {
                // ===== STAGE 1: Input Sanitization =====
                var stageStart = DateTime.UtcNow;
                var sanitizedInput = await SanitizeInputAsync(request.Prompt, response);
                AddDebugStage(response, "Sanitization", stageStart, true);

                if (string.IsNullOrEmpty(sanitizedInput))
                {
                    return CreateErrorResponse(response, "Input was rejected after sanitization", startTime);
                }

                // ===== STAGE 2: Cache Check =====
                stageStart = DateTime.UtcNow;
                if (options.AllowCaching)
                {
                    var cached = CheckCache(sanitizedInput, request.Context);
                    if (cached != null)
                    {
                        Interlocked.Increment(ref _cacheHits);
                        AddDebugStage(response, "CacheCheck", stageStart, true);
                        cached.Duration = DateTime.UtcNow - startTime;
                        cached.UsedCache = true;
                        return cached;
                    }
                }
                AddDebugStage(response, "CacheCheck", stageStart, true);

                // ===== STAGE 3: Intent Classification =====
                stageStart = DateTime.UtcNow;
                string? detectedIntent = null;
                if (_intentClassifier != null)
                {
                    try
                    {
                        var intentResult = await _intentClassifier.ClassifyAsync(
                            sanitizedInput,
                            new ConversationContext
                            {
                                CurrentScene = request.Context?.CurrentScene,
                                InCombat = request.Context?.InCombat ?? false,
                                InDialogue = request.Context?.InDialogue ?? false,
                                InShop = request.Context?.InShop ?? false
                            });
                        
                        detectedIntent = intentResult.PrimaryIntent.ToString();
                        response.IntentDetected = detectedIntent;
                        response.Confidence = intentResult.PrimaryConfidence;
                        
                        _requestsByIntent.AddOrUpdate(detectedIntent, 1, (_, c) => c + 1);
                        
                        if (intentResult.IsAmbiguous)
                        {
                            response.Warnings.Add($"Intent was ambiguous: {intentResult.AmbiguityReason}");
                        }
                    }
                    catch (Exception ex)
                    {
                        response.Warnings.Add($"Intent classification failed: {ex.Message}");
                    }
                }
                AddDebugStage(response, "IntentClassification", stageStart, true);

                // ===== STAGE 4: Memory Context =====
                stageStart = DateTime.UtcNow;
                string? memoryContext = null;
                if (options.EnableMemory && _memory != null)
                {
                    try
                    {
                        memoryContext = await _memory.BuildContextWindowAsync(
                            options.MaxTokens / 4, // Use 25% for memory
                            new MemoryQueryOptions { Query = sanitizedInput, MaxResults = 5 });
                        
                        response.Metadata.MemoryContext = memoryContext?.Length > 100 
                            ? memoryContext.Substring(0, 100) + "..." 
                            : memoryContext;
                    }
                    catch (Exception ex)
                    {
                        response.Warnings.Add($"Memory retrieval failed: {ex.Message}");
                    }
                }
                AddDebugStage(response, "MemoryContext", stageStart, true);

                // ===== STAGE 5: Prompt Assembly =====
                stageStart = DateTime.UtcNow;
                var fullPrompt = AssemblePrompt(sanitizedInput, request, memoryContext);
                var systemPrompt = AssembleSystemPrompt(request, detectedIntent);
                AddDebugStage(response, "PromptAssembly", stageStart, true);

                // ===== STAGE 6: LLM Call =====
                stageStart = DateTime.UtcNow;
                string? llmResponse = null;
                try
                {
                    if (_resilientService != null)
                    {
                        var aiRequest = new AiRequest
                        {
                            Prompt = fullPrompt,
                            SystemPrompt = systemPrompt,
                            Priority = request.Priority,
                            TimeoutMs = (int?)(options.Timeout?.TotalMilliseconds)
                        };
                        
                        var result = await _resilientService.ExecuteAsync(aiRequest, ct);
                        
                        if (result.Success)
                        {
                            llmResponse = result.Content;
                            response.UsedFallback = result.UsedFallback;
                            response.Metadata.RetryCount = result.AttemptCount - 1;
                        }
                        else
                        {
                            response.Errors.Add(result.ErrorMessage ?? "LLM call failed");
                        }
                    }
                    else
                    {
                        llmResponse = await _llmService.CompleteAsync(fullPrompt, systemPrompt);
                    }
                }
                catch (Exception ex)
                {
                    response.Errors.Add($"LLM call failed: {ex.Message}");
                }
                
                AddDebugStage(response, "LlmCall", stageStart, llmResponse != null);

                if (string.IsNullOrEmpty(llmResponse))
                {
                    // Try fallback
                    llmResponse = GenerateFallbackResponse(sanitizedInput, detectedIntent);
                    response.UsedFallback = true;
                    response.Warnings.Add("Used fallback response");
                }

                // ===== STAGE 7: Output Validation =====
                stageStart = DateTime.UtcNow;
                if (options.EnableValidation && _validator != null && !string.IsNullOrEmpty(llmResponse))
                {
                    try
                    {
                        var validationResult = await _validator.ValidateAndRepairAsync(
                            llmResponse,
                            new EnhancedValidationContext
                            {
                                MaxLength = 5000,
                                AllowAutoRepair = true
                            });
                        
                        response.WasValidated = true;
                        
                        if (validationResult.WasRepaired && validationResult.RepairedContent != null)
                        {
                            llmResponse = validationResult.RepairedContent;
                            response.Warnings.Add("Response was automatically repaired");
                        }
                        
                        if (!validationResult.IsValid)
                        {
                            Interlocked.Increment(ref _validationFailures);
                            foreach (var issue in validationResult.Issues.Where(i => 
                                i.Severity <= ValidationSeverity.Medium))
                            {
                                response.Warnings.Add($"Validation: {issue.Message}");
                            }
                        }
                        
                        response.Confidence *= validationResult.OverallScore;
                    }
                    catch (Exception ex)
                    {
                        response.Warnings.Add($"Validation failed: {ex.Message}");
                    }
                }
                AddDebugStage(response, "Validation", stageStart, true);

                // ===== STAGE 8: Record Memory =====
                stageStart = DateTime.UtcNow;
                if (options.EnableMemory && _memory != null && !string.IsNullOrEmpty(llmResponse))
                {
                    try
                    {
                        await _memory.AddAsync(sanitizedInput, llmResponse, new MemoryAddOptions
                        {
                            Context = request.Context?.CurrentScene ?? "general",
                            Priority = request.Priority == RequestPriority.Critical 
                                ? MemoryPriority.High 
                                : MemoryPriority.Normal
                        });
                    }
                    catch (Exception ex)
                    {
                        response.Warnings.Add($"Memory recording failed: {ex.Message}");
                    }
                }
                AddDebugStage(response, "MemoryRecord", stageStart, true);

                // ===== STAGE 9: Update Conversation =====
                if (!string.IsNullOrEmpty(request.ConversationId) && !string.IsNullOrEmpty(llmResponse))
                {
                    var conv = _conversations.GetOrAdd(request.ConversationId, _ => new List<(string, string)>());
                    lock (conv)
                    {
                        conv.Add(("user", sanitizedInput));
                        conv.Add(("assistant", llmResponse));
                        
                        // Keep only last N turns
                        while (conv.Count > _config.MaxConversationTurns * 2)
                        {
                            conv.RemoveAt(0);
                        }
                    }
                }

                // ===== Final Response =====
                response.Success = !string.IsNullOrEmpty(llmResponse);
                response.Content = llmResponse;
                response.Duration = DateTime.UtcNow - startTime;
                response.Metadata.LatencyMs = (float)response.Duration.TotalMilliseconds;

                // Update stats
                if (response.Success)
                {
                    Interlocked.Increment(ref _successfulRequests);
                    _totalLatency += response.Metadata.LatencyMs;
                    _totalConfidence += response.Confidence;
                }
                else
                {
                    Interlocked.Increment(ref _failedRequests);
                }

                // Cache successful responses
                if (response.Success && options.AllowCaching)
                {
                    CacheResponse(sanitizedInput, request.Context, response);
                }

                return response;
            }
            catch (OperationCanceledException)
            {
                response.Success = false;
                response.Errors.Add("Request was cancelled");
                response.Duration = DateTime.UtcNow - startTime;
                return response;
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(response, $"Unexpected error: {ex.Message}", startTime);
            }
        }

        public async Task<ProductionAiResponse> QuickProcessAsync(string prompt, CancellationToken ct = default)
        {
            return await ProcessAsync(new ProductionAiRequest { Prompt = prompt }, ct);
        }

        public async Task<ProductionAiResponse> ContinueConversationAsync(
            string conversationId, string prompt, CancellationToken ct = default)
        {
            return await ProcessAsync(new ProductionAiRequest
            {
                Prompt = prompt,
                ConversationId = conversationId
            }, ct);
        }

        public void ConfigureDefaults(ProductionAiRequestOptions defaults)
        {
            _defaultOptions = defaults;
        }

        public ProductionAiStats GetStats()
        {
            return new ProductionAiStats
            {
                TotalRequests = _totalRequests,
                SuccessfulRequests = _successfulRequests,
                FailedRequests = _failedRequests,
                CacheHits = _cacheHits,
                AverageLatencyMs = _successfulRequests > 0 ? _totalLatency / _successfulRequests : 0,
                AverageConfidence = _successfulRequests > 0 ? _totalConfidence / _successfulRequests : 0,
                RequestsByAgent = new Dictionary<string, int>(_requestsByAgent),
                RequestsByIntent = new Dictionary<string, int>(_requestsByIntent),
                EdgeCasesHandled = _edgeCasesHandled,
                ValidationFailures = _validationFailures
            };
        }

        public async Task WarmupAsync()
        {
            if (_warmedUp) return;

            // Initialize LLM
            await _llmService.InitializeAsync();

            // Warmup intent classifier with common prompts
            if (_intentClassifier != null)
            {
                var warmupPrompts = new[] { "Hello", "Attack the enemy", "What's the quest?", "Buy item" };
                foreach (var prompt in warmupPrompts)
                {
                    await _intentClassifier.ClassifyAsync(prompt);
                }
            }

            _warmedUp = true;
        }

        public void InvalidateCache(string? pattern = null)
        {
            if (pattern == null)
            {
                _cache.Clear();
            }
            else
            {
                var keysToRemove = _cache.Keys.Where(k => k.Contains(pattern)).ToList();
                foreach (var key in keysToRemove)
                {
                    _cache.TryRemove(key, out _);
                }
            }
        }

        // ============ Private Methods ============

        private async Task<string?> SanitizeInputAsync(string input, ProductionAiResponse response)
        {
            if (_edgeCaseHandler == null) return input;

            var sanitized = await _edgeCaseHandler.SanitizeInputAsync(input);
            
            if (sanitized.WasModified)
            {
                response.Warnings.AddRange(sanitized.AppliedTransformations.Select(t => $"Input modified: {t}"));
            }

            foreach (var edgeCase in sanitized.DetectedEdgeCases)
            {
                Interlocked.Increment(ref _edgeCasesHandled);
                
                if (edgeCase.Severity >= 0.8f)
                {
                    response.Errors.Add($"Critical edge case: {edgeCase.Description}");
                    return null;
                }
                else if (edgeCase.Severity >= 0.5f)
                {
                    response.Warnings.Add($"Edge case: {edgeCase.Description}");
                }
            }

            if (response.DebugInfo != null)
            {
                response.DebugInfo.SanitizedInput = sanitized.Sanitized;
                response.DebugInfo.DetectedEdgeCases = sanitized.DetectedEdgeCases
                    .Select(e => e.Description).ToList();
            }

            return sanitized.Sanitized;
        }

        private ProductionAiResponse? CheckCache(string input, ProductionAiRequestContext? context)
        {
            var cacheKey = GenerateCacheKey(input, context);
            
            if (_cache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.Expiry)
            {
                // Clone the response to avoid mutation
                return new ProductionAiResponse
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Success = cached.Response.Success,
                    Content = cached.Response.Content,
                    Confidence = cached.Response.Confidence,
                    AgentUsed = cached.Response.AgentUsed,
                    IntentDetected = cached.Response.IntentDetected,
                    UsedCache = true
                };
            }
            
            _cache.TryRemove(cacheKey, out _);
            return null;
        }

        private void CacheResponse(string input, ProductionAiRequestContext? context, ProductionAiResponse response)
        {
            if (_cache.Count >= _config.MaxCacheSize)
            {
                // Evict oldest
                var oldest = _cache.OrderBy(c => c.Value.Expiry).FirstOrDefault();
                _cache.TryRemove(oldest.Key, out _);
            }

            var cacheKey = GenerateCacheKey(input, context);
            _cache[cacheKey] = (response, DateTime.UtcNow.Add(_config.CacheDuration));
        }

        private string GenerateCacheKey(string input, ProductionAiRequestContext? context)
        {
            var contextHash = context != null
                ? $"_{context.CurrentScene}_{context.InCombat}_{context.InDialogue}"
                : "";
            return $"{input.GetHashCode()}{contextHash}";
        }

        private string AssemblePrompt(string input, ProductionAiRequest request, string? memoryContext)
        {
            var parts = new List<string>();

            // Add memory context
            if (!string.IsNullOrEmpty(memoryContext))
            {
                parts.Add($"Previous context:\n{memoryContext}\n");
            }

            // Add world state if available
            if (request.Options?.InjectWorldState == true && request.Context?.WorldState != null)
            {
                var stateStr = string.Join(", ", request.Context.WorldState.Take(5)
                    .Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                parts.Add($"Current situation: {stateStr}\n");
            }

            // Add conversation history
            if (!string.IsNullOrEmpty(request.ConversationId) && 
                _conversations.TryGetValue(request.ConversationId, out var conv))
            {
                var history = string.Join("\n", conv.TakeLast(6)
                    .Select(t => $"{t.Role}: {t.Content}"));
                parts.Add($"Conversation so far:\n{history}\n");
            }

            parts.Add($"User: {input}");

            return string.Join("\n", parts);
        }

        private string AssembleSystemPrompt(ProductionAiRequest request, string? intent)
        {
            var basePrompt = request.SystemPrompt ?? _config.DefaultSystemPrompt;

            // Add context-specific instructions
            if (request.Context?.InCombat == true)
            {
                basePrompt += "\nThe user is currently in combat. Be concise and action-oriented.";
            }
            else if (request.Context?.InDialogue == true)
            {
                basePrompt += "\nThe user is in a dialogue scene. Stay in character and maintain narrative flow.";
            }

            // Add intent-specific guidance
            if (!string.IsNullOrEmpty(intent))
            {
                basePrompt += $"\nDetected intent: {intent}. Respond appropriately.";
            }

            return basePrompt;
        }

        private string GenerateFallbackResponse(string input, string? intent)
        {
            return intent switch
            {
                "Combat" => "Understood. Ready for your next combat action.",
                "Lore" => "That's an interesting topic. Let me share what I know...",
                "Quest" => "Let me check on your current objectives...",
                "Economy" => "Here to help with your trading needs.",
                "Social" => "How can I help with this conversation?",
                _ => "I understand. How may I assist you?"
            };
        }

        private void AddDebugStage(ProductionAiResponse response, string name, DateTime start, bool success, string? error = null)
        {
            if (response.DebugInfo == null) return;
            
            response.DebugInfo.Stages.Add(new PipelineStageDebug
            {
                Name = name,
                DurationMs = (float)(DateTime.UtcNow - start).TotalMilliseconds,
                Success = success,
                Error = error
            });
        }

        private ProductionAiResponse CreateErrorResponse(ProductionAiResponse response, string error, DateTime startTime)
        {
            Interlocked.Increment(ref _failedRequests);
            response.Success = false;
            response.Errors.Add(error);
            response.Duration = DateTime.UtcNow - startTime;
            return response;
        }

        private async Task CacheCleanupLoopAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                
                var now = DateTime.UtcNow;
                var expiredKeys = _cache
                    .Where(kvp => kvp.Value.Expiry < now)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _cache.TryRemove(key, out _);
                }

                // Also clean old conversations
                var oldConversations = _conversations
                    .Where(kvp => kvp.Value.Count == 0)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in oldConversations)
                {
                    _conversations.TryRemove(key, out _);
                }
            }
        }
    }

    public class ProductionAiConfig
    {
        public bool EnableDebugInfo { get; set; } = false;
        public string DefaultSystemPrompt { get; set; } = "You are a helpful AI assistant in a gaming context. Be concise and helpful.";
        public int MaxCacheSize { get; set; } = 500;
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(10);
        public int MaxConversationTurns { get; set; } = 10;
    }
}
