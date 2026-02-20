namespace SaveState.Infrastructure.Ai.Knowledge;

using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;
using SaveState.Core.Ai.Knowledge;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

public class SemanticKnowledgeClient
{
    private readonly ILlmProvider _embeddingProvider;
    private readonly IKnowledgeStore _store;
    private readonly ILogger<SemanticKnowledgeClient> _logger;
    private readonly ITimeProvider _timeProvider;

    public SemanticKnowledgeClient(
        ILlmProvider embeddingProvider,
        IKnowledgeStore store,
        ILogger<SemanticKnowledgeClient> logger,
        ITimeProvider timeProvider)
    {
        _embeddingProvider = embeddingProvider;
        _store = store;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task IndexDocumentAsync(string id, string content, CancellationToken ct)
    {
        try
        {
            var embeddingResult = await _embeddingProvider.GenerateEmbeddingsAsync(
                new EmbeddingRequest(content, "text-embedding-ada-002"), ct).ConfigureAwait(false);

            if (embeddingResult.IsFailure || embeddingResult.Value is null)
            {
                _logger.LogError("Failed to generate embeddings for document {Id}: {Error}", id, embeddingResult.Error);
                throw new InvalidOperationException($"Embedding generation failed: {embeddingResult.Error}");
            }

            await _store.UpsertAsync(id, embeddingResult.Value.Embedding, content, new { Source = "Manual", IndexedAt = _timeProvider.UtcNow }, ct).ConfigureAwait(false);

            _logger.LogInformation("Indexed document {Id} with {DimensionCount} dimensions", id, embeddingResult.Value.Embedding.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index document {Id}", id);
            throw;
        }
    }

    public async Task<Result<string>> GetRelevantContextAsync(string query, CancellationToken ct)
    {
        try
        {
            var embeddingResult = await _embeddingProvider.GenerateEmbeddingsAsync(
                new EmbeddingRequest(query, "text-embedding-ada-002"), ct).ConfigureAwait(false);

            if (embeddingResult.IsFailure || embeddingResult.Value is null)
            {
                _logger.LogError("Failed to generate embeddings for query: {Error}", embeddingResult.Error);
                return Result.Failure<string>(
                    $"Failed to generate embeddings for query: {embeddingResult.Error}",
                    ErrorType.External);
            }

            var hits = await _store.SearchAsync(embeddingResult.Value.Embedding, 3, 0.75f, ct).ConfigureAwait(false);

            var context = string.Join("\n---\n", hits.Select(h => h.Content));

            return Result.Success(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve relevant context for query");
            return Result.Failure<string>(
                $"Failed to retrieve relevant context for query: {ex.Message}",
                ErrorType.Internal);
        }
    }

    public async Task<IReadOnlyList<KnowledgeHit>> SearchAsync(string query, int maxResults = 5, CancellationToken ct = default)
    {
        try
        {
            var embeddingResult = await _embeddingProvider.GenerateEmbeddingsAsync(
                new EmbeddingRequest(query, "text-embedding-ada-002"), ct).ConfigureAwait(false);

            if (embeddingResult.IsFailure || embeddingResult.Value is null)
            {
                _logger.LogError("Failed to generate embeddings for search query: {Error}", embeddingResult.Error);
                return Array.Empty<KnowledgeHit>();
            }

            return await _store.SearchAsync(embeddingResult.Value.Embedding, maxResults, 0.5f, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search knowledge base");
            return Array.Empty<KnowledgeHit>();
        }
    }
}
