using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Search.Models;
using SaveState.Core.Search.Services;

namespace SaveState.Infrastructure.Search.Services;

/// <summary>
/// Implementation of the universal search service with semantic and text-based search capabilities.
/// </summary>
public sealed class UniversalSearchService : IUniversalSearchService
{
    private readonly IOpenAiEmbeddingClient _embeddingClient;
    private readonly ILogger<UniversalSearchService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, SearchIndexEntry> _index;
    private readonly IEnumerable<ISearchProvider> _searchProviders;
    private readonly SearchOptions _defaultOptions;

    public UniversalSearchService(
        IOpenAiEmbeddingClient embeddingClient,
        IEnumerable<ISearchProvider> searchProviders,
        ITimeProvider timeProvider,
        ILogger<UniversalSearchService> logger)
    {
        _embeddingClient = embeddingClient;
        _searchProviders = searchProviders.OrderByDescending(p => p.Priority);
        _timeProvider = timeProvider;
        _logger = logger;
        _index = new ConcurrentDictionary<string, SearchIndexEntry>();
        _defaultOptions = new SearchOptions();
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SearchResult>>> SearchAsync(
        SearchQuery query,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Universal search: '{Query}' (Scope: {Scope})", query.Query, query.Scope);

        try
        {
            var results = new List<SearchResult>();

            // Semantic search using embeddings
            var semanticResults = await SearchSemanticAsync(query, ct);
            results.AddRange(semanticResults);

            // Provider-specific search
            foreach (var provider in _searchProviders)
            {
                if (ShouldUseProvider(query.Scope, provider.Scope))
                {
                    try
                    {
                        var providerResults = await provider.SearchAsync(query, ct);
                        results.AddRange(providerResults);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Search provider {Provider} failed for query: {Query}",
                            provider.GetType().Name, query.Query);
                    }
                }
            }

            // Apply filters
            results = ApplyFilters(results, query.Filters);

            // Sort by relevance and deduplicate
            var finalResults = results
                .GroupBy(r => r.Id)
                .Select(g => g.OrderByDescending(r => r.RelevanceScore).First())
                .OrderByDescending(r => r.RelevanceScore)
                .Take(query.MaxResults)
                .ToList();

            _logger.LogInformation(
                "Search completed in {ElapsedMs}ms with {ResultCount} results",
                stopwatch.ElapsedMilliseconds, finalResults.Count);

            return Result<IReadOnlyList<SearchResult>>.Success(finalResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for query: {Query}", query.Query);
            return Result<IReadOnlyList<SearchResult>>.Failure("Search failed", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SearchResult>>> SearchInstantAsync(
        string query,
        SearchScope scope = SearchScope.All,
        CancellationToken ct = default)
    {
        var searchQuery = new SearchQuery
        {
            Query = query,
            Scope = scope,
            Filters = new List<SearchFilter>(),
            MaxResults = 10
        };

        return await SearchAsync(searchQuery, ct);
    }

    /// <inheritdoc />
    public async Task<Result<GroupedSearchResults>> SearchGroupedAsync(
        SearchQuery query,
        CancellationToken ct = default)
    {
        var result = await SearchAsync(query, ct);

        if (result.IsFailure)
        {
            return Result<GroupedSearchResults>.Failure(result.Error!, result.ErrorType);
        }

        var stopwatch = Stopwatch.StartNew();
        var groups = result.Value!
            .GroupBy(r => r.Type)
            .Select(g => new SearchResultGroup
            {
                Type = g.Key,
                Title = GetGroupTitle(g.Key),
                Results = g.ToList()
            })
            .OrderBy(g => (int)g.Type)
            .ToList();

        var groupedResults = new GroupedSearchResults
        {
            Query = query.Query,
            Groups = groups,
            TotalResults = result.Value!.Count,
            SearchDuration = stopwatch.Elapsed
        };

        return Result<GroupedSearchResults>.Success(groupedResults);
    }

    /// <inheritdoc />
    public Task<Result> IndexAsync(
        SearchIndexEntry entry,
        CancellationToken ct = default)
    {
        _index[entry.Id] = entry;
        _logger.LogDebug("Indexed entry: {Id} ({Type})", entry.Id, entry.Type);
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> RemoveFromIndexAsync(
        string id,
        CancellationToken ct = default)
    {
        _index.TryRemove(id, out _);
        _logger.LogDebug("Removed entry from index: {Id}", id);
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public async Task<Result> RebuildIndexAsync(
        CancellationToken ct = default)
    {
        _logger.LogInformation("Rebuilding search index...");
        _index.Clear();

        var totalIndexed = 0;
        foreach (var provider in _searchProviders)
        {
            try
            {
                var entries = await provider.GetAllIndexableAsync(ct);
                foreach (var entry in entries)
                {
                    await IndexAsync(entry, ct);
                    totalIndexed++;
                }
                _logger.LogDebug("Indexed {Count} entries from {Provider}",
                    entries.Count, provider.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to index entries from {Provider}",
                    provider.GetType().Name);
            }
        }

        _logger.LogInformation("Search index rebuilt with {Count} entries", totalIndexed);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<string>>> GetSuggestionsAsync(
        string partialQuery,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(partialQuery) || partialQuery.Length < 2)
        {
            return Result<IReadOnlyList<string>>.Success(new List<string>());
        }

        var suggestions = new List<string>();

        // Get suggestions from index
        var indexSuggestions = _index.Values
            .Where(e => e.Title.StartsWith(partialQuery, StringComparison.OrdinalIgnoreCase) ||
                       e.Title.Contains(partialQuery, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.Title.StartsWith(partialQuery, StringComparison.OrdinalIgnoreCase))
            .ThenBy(e => e.Title)
            .Select(e => e.Title)
            .Take(3);

        suggestions.AddRange(indexSuggestions);

        // Get suggestions from providers
        foreach (var provider in _searchProviders.Take(3))
        {
            try
            {
                var providerSuggestions = await provider.GetSuggestionsAsync(partialQuery, 2, ct);
                suggestions.AddRange(providerSuggestions.Take(2));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get suggestions from {Provider}",
                    provider.GetType().Name);
            }
        }

        var distinctSuggestions = suggestions
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        return Result<IReadOnlyList<string>>.Success(distinctSuggestions);
    }

    private async Task<IReadOnlyList<SearchResult>> SearchSemanticAsync(
        SearchQuery query,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Query))
        {
            return new List<SearchResult>();
        }

        // Generate embedding for query
        var embeddingResult = await _embeddingClient.GenerateEmbeddingAsync(query.Query, ct);
        if (embeddingResult.IsFailure)
        {
            _logger.LogWarning("Failed to generate embedding for query: {Error}",
                embeddingResult.Error);
            return new List<SearchResult>();
        }

        var queryEmbedding = embeddingResult.Value!;
        var results = new List<SearchResult>();
        var threshold = _defaultOptions.MinRelevanceScore;

        // Compare with indexed entries
        foreach (var entry in _index.Values)
        {
            var similarity = CalculateCosineSimilarity(queryEmbedding, entry.Embedding);
            if (similarity > threshold)
            {
                results.Add(new SearchResult
                {
                    Id = entry.Id,
                    Title = entry.Title,
                    Subtitle = entry.Content.Length > 100
                        ? entry.Content[..100] + "..."
                        : entry.Content,
                    Type = Enum.Parse<SearchResultType>(entry.Type),
                    Icon = GetIconForType(entry.Type),
                    RelevanceScore = similarity,
                    Highlights = new List<string> { entry.Content },
                    Action = null,
                    Shortcut = null
                });
            }
        }

        return results;
    }

    private static float CalculateCosineSimilarity(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        if (a.Count != b.Count)
        {
            return 0f;
        }

        float dotProduct = 0;
        float normA = 0;
        float normB = 0;

        for (int i = 0; i < a.Count; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0)
        {
            return 0f;
        }

        return dotProduct / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }

    private static string GetIconForType(string type)
    {
        return type switch
        {
            "Game" => "🎮",
            "SaveState" => "💾",
            "Setting" => "⚙️",
            "Action" => "▶️",
            "Command" => "⌨️",
            "Guide" => "📖",
            "Achievement" => "🏆",
            _ => "📄"
        };
    }

    private static bool ShouldUseProvider(SearchScope queryScope, SearchScope providerScope)
    {
        return queryScope == SearchScope.All || queryScope == providerScope;
    }

    private static List<SearchResult> ApplyFilters(
        List<SearchResult> results,
        IReadOnlyList<SearchFilter> filters)
    {
        if (filters.Count == 0)
        {
            return results;
        }

        foreach (var filter in filters)
        {
            results = results.Where(r => MatchesFilter(r, filter)).ToList();
        }

        return results;
    }

    private static bool MatchesFilter(SearchResult result, SearchFilter filter)
    {
        return filter.Operator.ToLowerInvariant() switch
        {
            "eq" => result.Type.ToString() == filter.Value.ToString(),
            "contains" => result.Title.Contains(filter.Value.ToString() ?? "", StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    private static string GetGroupTitle(SearchResultType type)
    {
        return type switch
        {
            SearchResultType.Game => "Games",
            SearchResultType.SaveState => "Save States",
            SearchResultType.Setting => "Settings",
            SearchResultType.Action => "Actions",
            SearchResultType.Command => "Commands",
            SearchResultType.Guide => "Guides",
            SearchResultType.Achievement => "Achievements",
            _ => "Other"
        };
    }
}
