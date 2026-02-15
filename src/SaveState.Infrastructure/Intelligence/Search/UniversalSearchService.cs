using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Intelligence.Search.Services;

namespace SaveState.Infrastructure.Intelligence.Search;

/// <summary>
/// Universal search service providing semantic search across games, save states, settings, and commands.
/// </summary>
public sealed class UniversalSearchService : IUniversalSearchService
{
    private readonly ILogger<UniversalSearchService> _logger;
    private readonly Dictionary<Guid, GameSearchIndex> _gameIndex = new();
    private readonly Dictionary<Guid, ContentIndex> _contentIndex = new();
    private readonly Dictionary<string, SearchableAction> _actionIndex = new();
    private readonly Dictionary<string, int> _searchTrends = new();

    public UniversalSearchService(ILogger<UniversalSearchService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<UniversalSearchResults>> SearchAsync(
        SearchQuery query,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Universal search: '{Query}'", query.Query);

        try
        {
            // Track search trend
            TrackSearch(query.Query);

            // Search across all categories
            var gameResultsTask = SearchGamesInternalAsync(query, ct);
            var actionResultsTask = SearchActionsInternalAsync(query.Query, ct);
            var contentResultsTask = SearchContentInternalAsync(query, ct);

            await Task.WhenAll(gameResultsTask, actionResultsTask, contentResultsTask);

            var games = await gameResultsTask;
            var actions = await actionResultsTask;
            var content = await contentResultsTask;

            // For save states, we'll include empty for now
            var saveStates = new List<SaveStateSearchResult>();

            var totalResults = games.Count + saveStates.Count + actions.Count + content.Count;

            var results = new UniversalSearchResults(
                Query: query.Query,
                Games: games,
                SaveStates: saveStates,
                Actions: actions,
                Content: content,
                TotalResults: totalResults,
                SearchDuration: stopwatch.Elapsed);

            _logger.LogInformation(
                "Search completed in {ElapsedMs}ms with {TotalResults} results",
                stopwatch.ElapsedMilliseconds, totalResults);

            return Result.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Universal search failed for query: '{Query}'", query.Query);
            return Result.Failure<UniversalSearchResults>(
                "Search failed", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SemanticGameResult>>> SearchGamesAsync(
        string query,
        GameSearchOptions? options = null,
        CancellationToken ct = default)
    {
        var results = await SearchGamesInternalAsync(
            new SearchQuery(query, options?.MaxResults ?? 20),
            ct);

        var semanticResults = results.Select(r => new SemanticGameResult(
            GameId: Guid.Parse(r.Id),
            Title: r.Title,
            Description: r.Description,
            SemanticScore: r.RelevanceScore,
            TextMatchScore: r.RelevanceScore * 0.8f,
            CombinedScore: r.RelevanceScore,
            MatchedConcepts: r.MatchedTerms,
            Explanation: $"Matched based on {r.MatchReason}")).ToList();

        return Result.Success<IReadOnlyList<SemanticGameResult>>(semanticResults);
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ActionSearchResult>>> SearchActionsAsync(
        string query,
        CancellationToken ct = default)
    {
        return SearchActionsInternalAsync(query, ct)
            .ContinueWith(t => Result.Success<IReadOnlyList<ActionSearchResult>>(t.Result));
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ContentSearchResult>>> SearchContentAsync(
        string query,
        IReadOnlyList<ContentType>? contentTypes = null,
        CancellationToken ct = default)
    {
        return SearchContentInternalAsync(
            new SearchQuery(query, 20, null, null),
            ct)
            .ContinueWith(t => Result.Success<IReadOnlyList<ContentSearchResult>>(t.Result));
    }

    /// <inheritdoc />
    public Task<Result> IndexGameAsync(
        GameSearchIndex game,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Indexing game: {GameTitle} ({GameId})", game.Title, game.Id);
        _gameIndex[game.Id] = game;
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> IndexContentAsync(
        ContentIndex content,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Indexing content: {ContentTitle} ({ContentId})", content.Title, content.Id);
        _contentIndex[content.Id] = content;
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> RegisterActionAsync(
        SearchableAction action,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Registering action: {ActionTitle} ({ActionId})", action.Title, action.Id);
        _actionIndex[action.Id] = action;
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<SearchSuggestion>>> GetSuggestionsAsync(
        string partialQuery,
        int maxSuggestions = 5,
        CancellationToken ct = default)
    {
        var suggestions = new List<SearchSuggestion>();
        var queryLower = partialQuery.ToLowerInvariant();

        // Get suggestions from indexed games
        var gameSuggestions = _gameIndex.Values
            .Where(g => g.Title.ToLowerInvariant().Contains(queryLower))
            .Take(maxSuggestions / 2)
            .Select(g => new SearchSuggestion(
                g.Title,
                SearchCategory.Games,
                "🎮",
                CalculateSuggestionConfidence(partialQuery, g.Title)));

        suggestions.AddRange(gameSuggestions);

        // Get suggestions from actions
        var actionSuggestions = _actionIndex.Values
            .Where(a => a.Title.ToLowerInvariant().Contains(queryLower) ||
                       a.Keywords.Any(k => k.ToLowerInvariant().Contains(queryLower)))
            .Take(maxSuggestions / 2)
            .Select(a => new SearchSuggestion(
                a.Title,
                SearchCategory.Actions,
                a.Icon ?? "⚙️",
                CalculateSuggestionConfidence(partialQuery, a.Title)));

        suggestions.AddRange(actionSuggestions);

        // Sort by confidence
        suggestions = suggestions
            .OrderByDescending(s => s.Confidence)
            .Take(maxSuggestions)
            .ToList();

        return Task.FromResult(Result.Success<IReadOnlyList<SearchSuggestion>>(suggestions));
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<TrendingSearch>>> GetTrendingSearchesAsync(
        int count = 10,
        CancellationToken ct = default)
    {
        var trending = _searchTrends
            .OrderByDescending(kv => kv.Value)
            .Take(count)
            .Select(kv => new TrendingSearch(
                Query: kv.Key,
                SearchCount: kv.Value,
                LastSearched: DateTime.UtcNow,
                PrimaryCategory: SearchCategory.Games))
            .ToList();

        return Task.FromResult(Result.Success<IReadOnlyList<TrendingSearch>>(trending));
    }

    // Private helper methods

    private Task<List<GameSearchResult>> SearchGamesInternalAsync(
        SearchQuery query,
        CancellationToken ct)
    {
        var results = new List<GameSearchResult>();
        var queryLower = query.Query.ToLowerInvariant();
        var queryTerms = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var game in _gameIndex.Values)
        {
            var relevanceScore = CalculateGameRelevance(queryTerms, game, queryLower);

            if (relevanceScore >= (query.Filters?.MinRelevanceScore ?? 0.3f))
            {
                var matchReason = DetermineMatchReason(queryLower, game);
                var matchedTerms = GetMatchedTerms(queryTerms, game);

                results.Add(new GameSearchResult(
                    game.Id.ToString(),
                    game.Title,
                    game.Description,
                    relevanceScore,
                    null,
                    game.Genres,
                    matchedTerms,
                    matchReason));
            }
        }

        results = results
            .OrderByDescending(r => r.RelevanceScore)
            .Take(query.MaxResults)
            .ToList();

        return Task.FromResult(results);
    }

    private Task<List<ActionSearchResult>> SearchActionsInternalAsync(
        string query,
        CancellationToken ct)
    {
        var results = new List<ActionSearchResult>();
        var queryLower = query.ToLowerInvariant();
        var queryTerms = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var action in _actionIndex.Values)
        {
            var relevanceScore = CalculateActionRelevance(queryTerms, action, queryLower);

            if (relevanceScore > 0.3f)
            {
                results.Add(new ActionSearchResult(
                    action.Id,
                    action.Title,
                    action.Description,
                    relevanceScore,
                    action.ActionType,
                    action.Category,
                    action.Icon,
                    action.Keywords,
                    null));
            }
        }

        results = results
            .OrderByDescending(r => r.RelevanceScore)
            .Take(10)
            .ToList();

        return Task.FromResult(results);
    }

    private Task<List<ContentSearchResult>> SearchContentInternalAsync(
        SearchQuery query,
        CancellationToken ct)
    {
        var results = new List<ContentSearchResult>();
        var queryLower = query.Query.ToLowerInvariant();
        var queryTerms = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var content in _contentIndex.Values)
        {
            var relevanceScore = CalculateContentRelevance(queryTerms, content, queryLower);

            if (relevanceScore > 0.3f)
            {
                var snippets = GenerateSnippets(content.Content, queryTerms);
                var matchedTerms = GetContentMatchedTerms(queryTerms, content);

                results.Add(new ContentSearchResult(
                    content.Id.ToString(),
                    content.Title,
                    content.Content,
                    relevanceScore,
                    content.Type,
                    content.SourceId,
                    content.SourceType,
                    content.CreatedAt,
                    content.AuthorId,
                    matchedTerms,
                    snippets));
            }
        }

        results = results
            .OrderByDescending(r => r.RelevanceScore)
            .Take(query.MaxResults)
            .ToList();

        return Task.FromResult(results);
    }

    private float CalculateGameRelevance(string[] queryTerms, GameSearchIndex game, string fullQuery)
    {
        var score = 0f;

        // Title match (highest weight)
        if (game.Title.ToLowerInvariant().Contains(fullQuery))
        {
            score += 1.0f;
        }
        else
        {
            foreach (var term in queryTerms)
            {
                if (game.Title.ToLowerInvariant().Contains(term))
                {
                    score += 0.4f;
                }
            }
        }

        // Genre match
        foreach (var term in queryTerms)
        {
            if (game.Genres.Any(g => g.ToLowerInvariant().Contains(term)))
            {
                score += 0.2f;
            }
        }

        // Tag match
        foreach (var term in queryTerms)
        {
            if (game.Tags.Any(t => t.ToLowerInvariant().Contains(term)))
            {
                score += 0.15f;
            }
        }

        // Description match
        if (!string.IsNullOrEmpty(game.Description))
        {
            var descLower = game.Description.ToLowerInvariant();
            foreach (var term in queryTerms)
            {
                if (descLower.Contains(term))
                {
                    score += 0.1f;
                }
            }
        }

        return Math.Min(score, 1.0f);
    }

    private float CalculateActionRelevance(string[] queryTerms, SearchableAction action, string fullQuery)
    {
        var score = 0f;

        // Title match
        if (action.Title.ToLowerInvariant().Contains(fullQuery))
        {
            score += 1.0f;
        }
        else
        {
            foreach (var term in queryTerms)
            {
                if (action.Title.ToLowerInvariant().Contains(term))
                {
                    score += 0.5f;
                }
            }
        }

        // Keyword match
        foreach (var term in queryTerms)
        {
            if (action.Keywords.Any(k => k.ToLowerInvariant().Contains(term)))
            {
                score += 0.3f;
            }
        }

        // Category match
        if (!string.IsNullOrEmpty(action.Category))
        {
            foreach (var term in queryTerms)
            {
                if (action.Category.ToLowerInvariant().Contains(term))
                {
                    score += 0.2f;
                }
            }
        }

        return Math.Min(score, 1.0f);
    }

    private float CalculateContentRelevance(string[] queryTerms, ContentIndex content, string fullQuery)
    {
        var score = 0f;
        var contentLower = content.Content.ToLowerInvariant();
        var titleLower = content.Title.ToLowerInvariant();

        // Title match
        if (titleLower.Contains(fullQuery))
        {
            score += 1.0f;
        }
        else
        {
            foreach (var term in queryTerms)
            {
                if (titleLower.Contains(term))
                {
                    score += 0.4f;
                }
            }
        }

        // Content match
        foreach (var term in queryTerms)
        {
            if (contentLower.Contains(term))
            {
                score += 0.15f;
            }
        }

        return Math.Min(score, 1.0f);
    }

    private GameMatchReason DetermineMatchReason(string query, GameSearchIndex game)
    {
        if (game.Title.ToLowerInvariant().Contains(query))
            return GameMatchReason.TitleMatch;

        if (game.Genres.Any(g => query.Contains(g.ToLowerInvariant())))
            return GameMatchReason.GenreMatch;

        if (game.Tags.Any(t => query.Contains(t.ToLowerInvariant())))
            return GameMatchReason.TagMatch;

        if (!string.IsNullOrEmpty(game.Description) &&
            game.Description.ToLowerInvariant().Contains(query))
            return GameMatchReason.DescriptionMatch;

        return GameMatchReason.SemanticMatch;
    }

    private List<string> GetMatchedTerms(string[] queryTerms, GameSearchIndex game)
    {
        var matched = new List<string>();
        var gameText = $"{game.Title} {string.Join(" ", game.Genres)} {string.Join(" ", game.Tags)}".ToLowerInvariant();

        foreach (var term in queryTerms)
        {
            if (gameText.Contains(term))
            {
                matched.Add(term);
            }
        }

        return matched;
    }

    private List<string> GetContentMatchedTerms(string[] queryTerms, ContentIndex content)
    {
        var matched = new List<string>();
        var contentLower = content.Content.ToLowerInvariant();

        foreach (var term in queryTerms)
        {
            if (contentLower.Contains(term))
            {
                matched.Add(term);
            }
        }

        return matched;
    }

    private List<ContentSnippet> GenerateSnippets(string content, string[] queryTerms)
    {
        var snippets = new List<ContentSnippet>();
        var contentLower = content.ToLowerInvariant();

        foreach (var term in queryTerms)
        {
            var index = contentLower.IndexOf(term, StringComparison.Ordinal);
            if (index >= 0)
            {
                var startIndex = Math.Max(0, index - 50);
                var length = Math.Min(150, content.Length - startIndex);

                snippets.Add(new ContentSnippet(
                    Text: content.Substring(startIndex, length),
                    StartIndex: startIndex,
                    Length: length,
                    IsMatch: true));
            }
        }

        if (!snippets.Any())
        {
            // Add default snippet
            snippets.Add(new ContentSnippet(
                Text: content[..Math.Min(150, content.Length)],
                StartIndex: 0,
                Length: Math.Min(150, content.Length),
                IsMatch: false));
        }

        return snippets;
    }

    private float CalculateSuggestionConfidence(string query, string match)
    {
        var queryLower = query.ToLowerInvariant();
        var matchLower = match.ToLowerInvariant();

        if (matchLower.StartsWith(queryLower))
            return 0.9f;

        if (matchLower.Contains(queryLower))
            return 0.7f;

        return 0.5f;
    }

    private void TrackSearch(string query)
    {
        var queryLower = query.ToLowerInvariant();
        if (_searchTrends.ContainsKey(queryLower))
        {
            _searchTrends[queryLower]++;
        }
        else
        {
            _searchTrends[queryLower] = 1;
        }
    }
}
