using SaveState.Core.Ai.Services;
using SaveState.Core.Common;

namespace SaveState.Tests.Fakes;

public class FakeGroqProvider : ILlmProvider
{
    public string ProviderName => "Groq (Fake)";
    public bool IsAvailable => true;
    public IReadOnlyDictionary<string, ModelInfo> AvailableModels { get; } = new Dictionary<string, ModelInfo>
    {
        ["mixtral-8x7b-32768"] = new("Mixtral 8x7B", 32768, 0.00000027m)
    };

    public Task<Result<CompletionResult>> CompleteAsync(CompletionRequest request, CancellationToken ct)
        => Task.FromResult(Result.Success<CompletionResult>(new CompletionResult(
            $"Fake Groq completion for: {request.Prompt.Substring(0, Math.Min(50, request.Prompt.Length))}...",
            "stop",
            new TokenUsage(10, 20, 30),
            "mixtral-8x7b-32768")));

    public Task<Result<ChatResult>> ChatAsync(ChatRequest request, CancellationToken ct)
        => Task.FromResult(Result.Success<ChatResult>(new ChatResult(
            $"Fake Groq chat response to: {request.Messages.Last().Content.Substring(0, Math.Min(50, request.Messages.Last().Content.Length))}...",
            "stop",
            new TokenUsage(15, 25, 40),
            "mixtral-8x7b-32768")));

    public Task<Result<EmbeddingResult>> GenerateEmbeddingsAsync(EmbeddingRequest request, CancellationToken ct)
        => Task.FromResult(Result.Failure<EmbeddingResult>("Embeddings not supported by Groq provider", ErrorType.Internal));
}

