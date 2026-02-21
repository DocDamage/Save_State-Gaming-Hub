using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.Search.Models;
using SaveState.Core.Search.Services;

namespace SaveState.Infrastructure.Search.Providers;

/// <summary>
/// Search provider for games in the library.
/// </summary>
public sealed class GameSearchProvider : ISearchProvider
{
    private readonly IGameRepository _gameRepository;
    private readonly IOpenAiEmbeddingClient _embeddingClient;
    private readonly ILogger<GameSearchProvider> _logger;

    public GameSearchProvider(
        IGameRepository gameRepository,
        IOpenAiEmbeddingClient embeddingClient,
        ILogger<GameSearchProvider> logger)
    {
        _gameRepository = gameRepository;
        _embeddingClient = embeddingClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public SearchScope Scope => SearchScope.Games;

    /// <inheritdoc />
    public int Priority => 100; // High priority for games

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken ct = default)
    {
        try
        {
            var games = await _gameRepository.GetAllAsync(ct);
            var queryLower = query.Query.ToLowerInvariant();
            var queryTerms = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var results = games
                .Select(g => new { Game = g, Score = CalculateRelevance(g, queryTerms, queryLower) })
                .Where(x => x.Score > 0.2f)
                .OrderByDescending(x => x.Score)
                .Take(query.MaxResults)
                .Select(x => CreateSearchResult(x.Game, x.Score, queryTerms))
                .ToList();

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search games");
            return new List<SearchResult>();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchIndexEntry>> GetAllIndexableAsync(
        CancellationToken ct = default)
    {
        try
        {
            var games = await _gameRepository.GetAllAsync(ct);
            var entries = new List<SearchIndexEntry>();

            foreach (var game in games)
            {
                var content = BuildIndexContent(game);
                var embeddingResult = await _embeddingClient.GenerateEmbeddingAsync(content, ct);

                if (embeddingResult.IsSuccess)
                {
                    entries.Add(new SearchIndexEntry
                    {
                        Id = $"game:{game.Id}",
                        Type = "Game",
                        Title = game.Title,
                        Content = content,
                        Embedding = embeddingResult.Value!,
                        Tags = game.Tags.ToList(),
                        LastUpdated = game.UpdatedAt ?? game.CreatedAt
                    });
                }
            }

            return entries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get indexable games");
            return new List<SearchIndexEntry>();
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetSuggestionsAsync(
        string partialQuery,
        int maxSuggestions = 5,
        CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<string>>(new List<string>());
    }

    private static float CalculateRelevance(Game game, string[] queryTerms, string fullQuery)
    {
        var score = 0f;
        var titleLower = game.Title.ToLowerInvariant();

        // Title match (highest weight)
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
                    score += 0.5f;
                }
            }
        }

        // Genre match
        foreach (var term in queryTerms)
        {
            if (game.Genres.Any(g => g.Name.ToLowerInvariant().Contains(term)))
            {
                score += 0.3f;
            }
        }

        // Tag match
        foreach (var term in queryTerms)
        {
            if (game.Tags.Any(t => t.ToLowerInvariant().Contains(term)))
            {
                score += 0.2f;
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

    private static SearchResult CreateSearchResult(Game game, float score, string[] queryTerms)
    {
        var subtitle = BuildSubtitle(game);
        var highlights = GetHighlights(game, queryTerms);

        return new SearchResult
        {
            Id = $"game:{game.Id}",
            Title = game.Title,
            Subtitle = subtitle,
            Type = SearchResultType.Game,
            Icon = "🎮",
            RelevanceScore = score,
            Highlights = highlights,
            Action = async () =>
            {
                // Navigation will be handled by the view model
                return await Task.FromResult(Result.Success());
            },
            Shortcut = null
        };
    }

    private static string BuildSubtitle(Game game)
    {
        var parts = new List<string>();

        if (game.Genres.Any())
        {
            parts.Add(string.Join(", ", game.Genres.Select(g => g.Name)));
        }

        if (game.Platform != null)
        {
            parts.Add(game.Platform.Name);
        }

        if (game.Status.ToString() != "NotInstalled")
        {
            parts.Add(game.Status.ToString());
        }

        return parts.Count > 0 ? string.Join(" • ", parts) : "Game";
    }

    private static string BuildIndexContent(Game game)
    {
        var parts = new List<string> { game.Title };

        if (!string.IsNullOrEmpty(game.Description))
        {
            parts.Add(game.Description);
        }

        if (game.Genres.Any())
        {
            parts.Add(string.Join(" ", game.Genres.Select(g => g.Name)));
        }

        if (game.Tags.Any())
        {
            parts.Add(string.Join(" ", game.Tags));
        }

        return string.Join(" ", parts);
    }

    private static IReadOnlyList<string> GetHighlights(Game game, string[] queryTerms)
    {
        var highlights = new List<string>();

        if (!string.IsNullOrEmpty(game.Description))
        {
            var descLower = game.Description.ToLowerInvariant();
            foreach (var term in queryTerms)
            {
                var index = descLower.IndexOf(term, StringComparison.Ordinal);
                if (index >= 0)
                {
                    var start = Math.Max(0, index - 30);
                    var length = Math.Min(100, game.Description.Length - start);
                    highlights.Add(game.Description.Substring(start, length));
                    break;
                }
            }
        }

        return highlights;
    }
}
