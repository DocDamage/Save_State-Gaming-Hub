using SaveState.Core.Common;
using SaveState.Core.Search.Models;

namespace SaveState.Core.Search.Services;

/// <summary>
/// Universal search service providing semantic and content-aware search across the entire application.
/// </summary>
public interface IUniversalSearchService
{
    /// <summary>
    /// Performs a comprehensive search across all indexed content.
    /// </summary>
    /// <param name="query">The search query configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Search results organized by relevance.</returns>
    Task<Result<IReadOnlyList<SearchResult>>> SearchAsync(
        SearchQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Performs an instant search with simplified parameters for quick lookups.
    /// </summary>
    /// <param name="query">The search text.</param>
    /// <param name="scope">The search scope.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Quick search results limited to 10 items.</returns>
    Task<Result<IReadOnlyList<SearchResult>>> SearchInstantAsync(
        string query,
        SearchScope scope = SearchScope.All,
        CancellationToken ct = default);

    /// <summary>
    /// Indexes an item for search.
    /// </summary>
    /// <param name="entry">The item to index.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the indexing operation.</returns>
    Task<Result> IndexAsync(
        SearchIndexEntry entry,
        CancellationToken ct = default);

    /// <summary>
    /// Removes an item from the search index.
    /// </summary>
    /// <param name="id">The ID of the item to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the removal operation.</returns>
    Task<Result> RemoveFromIndexAsync(
        string id,
        CancellationToken ct = default);

    /// <summary>
    /// Rebuilds the entire search index from all registered providers.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the rebuild operation.</returns>
    Task<Result> RebuildIndexAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets search suggestions based on a partial query.
    /// </summary>
    /// <param name="partialQuery">The partial query text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of search suggestions.</returns>
    Task<Result<IReadOnlyList<string>>> GetSuggestionsAsync(
        string partialQuery,
        CancellationToken ct = default);

    /// <summary>
    /// Gets grouped search results organized by category.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Grouped search results.</returns>
    Task<Result<GroupedSearchResults>> SearchGroupedAsync(
        SearchQuery query,
        CancellationToken ct = default);
}

/// <summary>
/// Interface for embedding generation client used in semantic search.
/// </summary>
public interface IOpenAiEmbeddingClient
{
    /// <summary>
    /// Generates an embedding vector for the given text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The embedding vector.</returns>
    Task<Result<IReadOnlyList<float>>> GenerateEmbeddingAsync(
        string text,
        CancellationToken ct = default);

    /// <summary>
    /// Generates embedding vectors for multiple texts in batch.
    /// </summary>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The embedding vectors.</returns>
    Task<Result<IReadOnlyList<IReadOnlyList<float>>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default);
}
