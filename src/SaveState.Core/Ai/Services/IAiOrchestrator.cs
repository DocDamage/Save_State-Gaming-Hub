using SaveState.Core.Common;

namespace SaveState.Core.Ai.Services;

public interface IAiOrchestrator
{
    Task<AiResponse> ProcessRequestAsync(AiRequest request, CancellationToken ct = default);
    Task<AiResponse> ProcessRequestWithContextAsync(
        string sessionId,
        AiRequest request,
        CancellationToken ct = default);
    Task<Result<AiResponse>> ExecutePromptAsync(
        string sessionId,
        string prompt,
        CancellationToken ct = default);
    Task<Result<string>> GenerateTextAsync(string prompt, CancellationToken ct = default);
    IReadOnlyList<string> GetAvailableProviders();
    Task<bool> IsProviderHealthyAsync(string providerName, CancellationToken ct = default);
    (long Requests, long Hits, double HitRate) GetCacheStatistics();
    Task<bool> ClearConversationAsync(string sessionId, CancellationToken ct = default);
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
