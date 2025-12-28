using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.Production;
using SaveState.Core.Services.Ai.Orchestration;


namespace SaveState.Core.Services.Ai
{
    /// <summary>
    /// Lightweight Facade for the Production AI subsystem.
    /// Orchestrates the split components: Pipeline, Cache, Conversation, and Stats.
    /// </summary>
    public class ProductionAiService : IProductionAiService
    {
        private readonly IAiRequestPipeline _pipeline;
        private readonly IAiResponseCache _cache;
        private readonly IAiStatisticsCollector _statsCollector;
        private readonly ILlmService _llmService;
        private readonly IEnhancedIntentClassifier? _intentClassifier;
        private readonly CancellationTokenSource _cleanupCts = new();
        private ProductionAiRequestOptions _defaultOptions = new();

        public ProductionAiService(
            IAiRequestPipeline pipeline,
            IAiResponseCache cache,
            IAiStatisticsCollector statsCollector,
            ILlmService llmService,
            IEnhancedIntentClassifier? intentClassifier = null)
        {
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _statsCollector = statsCollector ?? throw new ArgumentNullException(nameof(statsCollector));
            _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
            _intentClassifier = intentClassifier;

            // Start background cache cleanup
            _ = _cache.StartCleanupAsync(_cleanupCts.Token);
        }

        public async Task<ProductionAiResponse> ProcessAsync(ProductionAiRequest request, CancellationToken ct = default)
        {
            if (request.Options == null)
            {
                request.Options = _defaultOptions;
            }
            return await _pipeline.ProcessAsync(request, ct);
        }

        public async Task<ProductionAiResponse> QuickProcessAsync(string prompt, CancellationToken ct = default)
        {
            return await ProcessAsync(new ProductionAiRequest { Prompt = prompt }, ct);
        }

        public async Task<ProductionAiResponse> ContinueConversationAsync(string conversationId, string prompt, CancellationToken ct = default)
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
            return _statsCollector.GetStats();
        }

        public async Task WarmupAsync()
        {
            await _llmService.InitializeAsync();

            if (_intentClassifier != null)
            {
                var warmupPrompts = new[] { "Hello", "Attack the enemy", "What's the quest?", "Buy item" };
                foreach (var prompt in warmupPrompts)
                {
                    try { await _intentClassifier.ClassifyAsync(prompt); } catch { /* Ignore warmup failures */ }
                }
            }
        }

        public void InvalidateCache(string? pattern = null)
        {
            _cache.Invalidate(pattern);
        }

        public void Dispose()
        {
            _cleanupCts.Cancel();
            _cleanupCts.Dispose();
        }
    }
}
