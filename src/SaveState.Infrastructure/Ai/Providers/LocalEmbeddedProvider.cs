using SaveState.Core.Ai.Services;
using SaveState.Core.Common;

namespace SaveState.Infrastructure.Ai.Providers;

/// <summary>
/// Provider for local embedded LLMs with RAG and BMAD integration.
/// </summary>
public class LocalEmbeddedProvider : ILlmProvider
{
    public string ProviderName => "Embedded (Local)";
    public bool IsAvailable => true;
    public IReadOnlyDictionary<string, ModelInfo> AvailableModels { get; }

    public LocalEmbeddedProvider()
    {
        AvailableModels = new Dictionary<string, ModelInfo>
        {
            ["phi-3-mini"] = new("Phi-3 Mini (BMAD Optimized)", 4096, 0m),
            ["mistral-tiny"] = new("Mistral Tiny (RAG Ready)", 2048, 0m),
            ["llama-3-8b"] = new("Llama-3 8B (Local)", 8192, 0m),
            ["savestate-base"] = new("SaveState Base (Core Logic)", 1024, 0m)
        };
    }

    public async Task<Result<CompletionResult>> CompleteAsync(CompletionRequest request, CancellationToken ct = default)
    {
        await Task.Delay(150, ct);
        var content = $"[Local {request.Model}] Simulated response for: {request.Prompt}";
        return Result.Success<CompletionResult>(new CompletionResult(content, "stop", new TokenUsage(10, 20, 30), request.Model));
    }

    public async Task<Result<ChatResult>> ChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        await Task.Delay(300, ct);

        // Find the context message if it exists (Orchestrator prepends it as a system message)
        var contextMsg = request.Messages.FirstOrDefault(m => m.Role == "system" && m.Content.Contains("CONTEXT INFORMATION"))?.Content;
        var lastUserMsg = request.Messages.LastOrDefault(m => m.Role == "user")?.Content ?? "";

        string content;
        if (!string.IsNullOrEmpty(contextMsg))
        {
            content = $"[Local {request.Model}] Based on your local knowledge base and recent memory:\n\n{contextMsg.Replace("CONTEXT INFORMATION (RAG/BMAD):\n", "")}\n\nProcessed query: \"{lastUserMsg}\"";
        }
        else
        {
            content = $"[Local {request.Model}] Hello! I am your local AI. I am currently running without specific RAG context for this query, but I'm ready to help. (Hint: Try asking about your MUGEN library or recent game sessions).";
        }

        return Result.Success<ChatResult>(new ChatResult(content, "stop", new TokenUsage(100, 150, 250), request.Model));
    }

    public Task<Result<EmbeddingResult>> GenerateEmbeddingsAsync(EmbeddingRequest request, CancellationToken ct = default)
    {
        // Simulate embedding generation (1536 dimensions for compatibility with ada-002)
        var mockEmbedding = new float[1536];
        var seed = request.Text.GetHashCode();
        var rnd = new Random(seed);
        for (int i = 0; i < mockEmbedding.Length; i++)
        {
            mockEmbedding[i] = (float)rnd.NextDouble();
        }

        return Task.FromResult(Result.Success<EmbeddingResult>(new EmbeddingResult(mockEmbedding, "local-embed-v1")));
    }
}

