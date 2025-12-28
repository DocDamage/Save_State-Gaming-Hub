using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai
{
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
        public AiRequestMetadata Metadata { get; set; } = new();
    }

    public class ProductionAiRequestContext
    {
        public string? CurrentScene { get; set; }
        public bool InCombat { get; set; }
        public bool InDialogue { get; set; }
        public bool InShop { get; set; }
        public string? ActiveQuest { get; set; }
        public List<string>? RecentEvents { get; set; }
        public WorldStateData? WorldState { get; set; }
        public PlayerStateData? PlayerState { get; set; }
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
        public AdditionalResponseMetadata? Additional { get; set; }
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

    public class ProductionAiConfig
    {
        public bool EnableDebugInfo { get; set; } = true;
        public int MaxCacheSize { get; set; } = 1000;
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(1);
        public int MaxConversationTurns { get; set; } = 10;
        public string DefaultSystemPrompt { get; set; } = "You are a helpful gaming assistant.";
    }
}
