using SaveState.Core.Search.Models;

namespace SaveState.Core.Search.Services;

/// <summary>
/// Interface for search providers that supply results for specific content types.
/// </summary>
public interface ISearchProvider
{
    /// <summary>
    /// Gets the scope of content this provider handles.
    /// </summary>
    SearchScope Scope { get; }

    /// <summary>
    /// Gets the priority of this provider (higher values are processed first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Searches for content matching the query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Matching search results.</returns>
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all indexable content from this provider.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All indexable entries.</returns>
    Task<IReadOnlyList<SearchIndexEntry>> GetAllIndexableAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets suggestions based on a partial query for this provider's scope.
    /// </summary>
    /// <param name="partialQuery">The partial query.</param>
    /// <param name="maxSuggestions">Maximum number of suggestions.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Search suggestions.</returns>
    Task<IReadOnlyList<string>> GetSuggestionsAsync(
        string partialQuery,
        int maxSuggestions = 5,
        CancellationToken ct = default);
}
