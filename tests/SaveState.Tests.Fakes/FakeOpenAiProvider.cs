using SaveState.Core.Ai.Services;
using SaveState.Core.Common;

namespace SaveState.Tests.Fakes;

public class FakeOpenAiProvider : ILlmProvider
{
    public string ProviderName => "OpenAI (Fake)";
    public bool IsAvailable => true;
    public IReadOnlyDictionary<string, ModelInfo> AvailableModels { get; } = new Dictionary<string, ModelInfo>
    {
        ["gpt-4"] = new("GPT-4", 8192, 0.00003m)
    };

    public Task<Result<CompletionResult>> CompleteAsync(CompletionRequest request, CancellationToken ct)
        => Task.FromResult(Result<CompletionResult>.Success(new CompletionResult(
            $"Fake completion for: {request.Prompt.Substring(0, Math.Min(50, request.Prompt.Length))}...",
            "stop",
            new TokenUsage(10, 20, 30),
            "gpt-4-fake")));

    public Task<Result<ChatResult>> ChatAsync(ChatRequest request, CancellationToken ct)
        => Task.FromResult(Result<ChatResult>.Success(new ChatResult(
            $"Fake chat response to: {request.Messages.Last().Content.Substring(0, Math.Min(50, request.Messages.Last().Content.Length))}...",
            "stop",
            new TokenUsage(15, 25, 40),
            "gpt-4-fake")));

    public Task<Result<EmbeddingResult>> GenerateEmbeddingsAsync(EmbeddingRequest request, CancellationToken ct)
        => Task.FromResult(Result<EmbeddingResult>.Success(new EmbeddingResult(new float[1536], "text-embedding-ada-002-fake")));
}
