namespace SaveState.Core.Ai.Services;

public interface IAiOrchestrator
{
    Task<AiResponse> ProcessRequestAsync(AiRequest request, CancellationToken ct = default);
    IReadOnlyList<string> GetAvailableProviders();
    Task<bool> IsProviderHealthyAsync(string providerName, CancellationToken ct = default);
    (long Requests, long Hits, double HitRate) GetCacheStatistics();
}

public record AiRequest(
    AiRequestType Type,
    string? Prompt = null,
    IReadOnlyList<ChatMessage>? Messages = null,
    string? Model = null,
    string? PreferredProvider = null,
    int? MaxTokens = null,
    float? Temperature = null,
    bool AllowCache = true);

public record AiResponse(
    string Content,
    string FinishReason,
    TokenUsage TokenUsage,
    string Model,
    string Provider,
    bool IsSuccessful = true,
    string? Error = null);

public enum AiRequestType { Completion, Chat, Embedding }
