using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Search.Services;

namespace SaveState.Infrastructure.Search.Services;

/// <summary>
/// Implementation of the OpenAI embedding client using the LLM provider.
/// </summary>
public sealed class OpenAiEmbeddingClient : IOpenAiEmbeddingClient
{
    private readonly ILlmProvider _llmProvider;
    private readonly ILogger<OpenAiEmbeddingClient> _logger;
    private const string EmbeddingModel = "text-embedding-ada-002";

    public OpenAiEmbeddingClient(
        ILlmProvider llmProvider,
        ILogger<OpenAiEmbeddingClient> logger)
    {
        _llmProvider = llmProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<float>>> GenerateEmbeddingAsync(
        string text,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Result<IReadOnlyList<float>>.Failure("Text cannot be empty");
            }

            var request = new EmbeddingRequest(text, EmbeddingModel);
            var result = await _llmProvider.GenerateEmbeddingsAsync(request, ct);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to generate embedding: {Error}", result.Error);
                return Result<IReadOnlyList<float>>.Failure(
                    result.Error ?? "Embedding generation failed",
                    result.ErrorType);
            }

            var embedding = result.Value?.Embedding;
            if (embedding == null || embedding.Length == 0)
            {
                return Result<IReadOnlyList<float>>.Failure("Empty embedding returned");
            }

            return Result<IReadOnlyList<float>>.Success(embedding);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding for text");
            return Result<IReadOnlyList<float>>.Failure(
                $"Embedding generation error: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<IReadOnlyList<float>>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default)
    {
        try
        {
            if (texts == null || texts.Count == 0)
            {
                return Result<IReadOnlyList<IReadOnlyList<float>>>.Failure("Texts cannot be empty");
            }

            var results = new List<IReadOnlyList<float>>(texts.Count);

            // Process in batches to avoid overwhelming the API
            const int batchSize = 10;
            for (int i = 0; i < texts.Count; i += batchSize)
            {
                var batch = texts.Skip(i).Take(batchSize).ToList();
                var batchResults = await ProcessBatchAsync(batch, ct);
                results.AddRange(batchResults);
            }

            return Result<IReadOnlyList<IReadOnlyList<float>>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embeddings for batch");
            return Result<IReadOnlyList<IReadOnlyList<float>>>.Failure(
                $"Batch embedding generation error: {ex.Message}",
                ErrorType.Internal);
        }
    }

    private async Task<IReadOnlyList<IReadOnlyList<float>>> ProcessBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct)
    {
        var tasks = texts.Select(text => GenerateEmbeddingAsync(text, ct));
        var results = await Task.WhenAll(tasks);

        return results
            .Where(r => r.IsSuccess)
            .Select(r => r.Value!)
            .ToList();
    }
}

/// <summary>
/// Fallback embedding client that generates simple deterministic embeddings for development/testing.
/// </summary>
public sealed class LocalEmbeddingClient : IOpenAiEmbeddingClient
{
    private readonly ILogger<LocalEmbeddingClient> _logger;
    private const int EmbeddingDimension = 1536; // Same as ada-002

    public LocalEmbeddingClient(ILogger<LocalEmbeddingClient> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<float>>> GenerateEmbeddingAsync(
        string text,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Task.FromResult(Result<IReadOnlyList<float>>.Failure("Text cannot be empty"));
            }

            // Generate deterministic pseudo-random embedding based on text hash
            var embedding = GenerateDeterministicEmbedding(text);

            return Task.FromResult(Result<IReadOnlyList<float>>.Success(embedding));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating local embedding");
            return Task.FromResult(Result<IReadOnlyList<float>>.Failure(
                $"Local embedding error: {ex.Message}",
                ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<IReadOnlyList<float>>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default)
    {
        var results = texts.Select(text => GenerateDeterministicEmbedding(text)).ToList();
        return Task.FromResult(Result<IReadOnlyList<IReadOnlyList<float>>>.Success(
            (IReadOnlyList<IReadOnlyList<float>>)results));
    }

    private static IReadOnlyList<float> GenerateDeterministicEmbedding(string text)
    {
        var embedding = new float[EmbeddingDimension];
        var hash = GetDeterministicHashCode(text);
        var random = new Random(hash);

        for (int i = 0; i < EmbeddingDimension; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2 - 1);
        }

        // Normalize
        var norm = MathF.Sqrt(embedding.Sum(x => x * x));
        if (norm > 0)
        {
            for (int i = 0; i < EmbeddingDimension; i++)
            {
                embedding[i] /= norm;
            }
        }

        return embedding;
    }

    private static int GetDeterministicHashCode(string text)
    {
        unchecked
        {
            int hash = 23;
            foreach (char c in text)
            {
                hash = hash * 31 + c;
            }
            return hash;
        }
    }
}
