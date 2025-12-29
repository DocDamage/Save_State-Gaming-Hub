using SaveState.Core.Common;

namespace SaveState.Core.Ai.Services;

public interface ILlmProvider
{
    string ProviderName { get; }
    bool IsAvailable { get; }
    IReadOnlyDictionary<string, ModelInfo> AvailableModels { get; }

    Task<Result<CompletionResult>> CompleteAsync(CompletionRequest request, CancellationToken ct = default);
    Task<Result<ChatResult>> ChatAsync(ChatRequest request, CancellationToken ct = default);
    Task<Result<EmbeddingResult>> GenerateEmbeddingsAsync(EmbeddingRequest request, CancellationToken ct = default);
}

public record CompletionRequest(string Prompt, string Model, int MaxTokens = 1000, float Temperature = 0.7f);
public record CompletionResult(string Text, string FinishReason, TokenUsage Usage, string Model);
public record ChatRequest(IReadOnlyList<ChatMessage> Messages, string Model, int MaxTokens = 1000);
public record ChatResult(string Content, string FinishReason, TokenUsage Usage, string Model);
public record EmbeddingRequest(string Text, string Model);
public record EmbeddingResult(float[] Embedding, string Model);
public record TokenUsage(int PromptTokens, int CompletionTokens, int TotalTokens);
public record ChatMessage(string Role, string Content);
public record ModelInfo(string Name, int MaxTokens, decimal CostPerToken);
