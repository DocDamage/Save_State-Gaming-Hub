namespace SaveState.Core.Interfaces;

/// <summary>
/// Vector store for semantic search using embeddings
/// </summary>
public interface IVectorStoreService
{
    /// <summary>
    /// Index a document with its embedding
    /// </summary>
    Task IndexDocumentAsync(string id, string content, string category, float[] embedding);

    /// <summary>
    /// Search for similar documents
    /// </summary>
    Task<List<RetrievalResult>> SearchAsync(float[] queryEmbedding, int topK = 5, string? category = null);

    /// <summary>
    /// Remove a document from the index
    /// </summary>
    Task DeleteAsync(string id);

    /// <summary>
    /// Get total document count
    /// </summary>
    Task<int> GetDocumentCountAsync();
}

/// <summary>
/// Result from vector similarity search
/// </summary>
public record RetrievalResult
{
    public string Id { get; init; } = "";
    public string Content { get; init; } = "";
    public string Category { get; init; } = "";
    public double SimilarityScore { get; init; }
}
