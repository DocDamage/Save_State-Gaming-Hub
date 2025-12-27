namespace SaveState.Core.Interfaces;

/// <summary>
/// Service for generating text embeddings for semantic search
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generate embedding vector for a single text
    /// </summary>
    Task<float[]> GetEmbeddingAsync(string text);

    /// <summary>
    /// Generate embeddings for multiple texts in batch
    /// </summary>
    Task<List<float[]>> GetEmbeddingsAsync(IEnumerable<string> texts);

    /// <summary>
    /// Check if the embedding service is properly configured
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Embedding dimension for the current model
    /// </summary>
    int EmbeddingDimension { get; }
}
