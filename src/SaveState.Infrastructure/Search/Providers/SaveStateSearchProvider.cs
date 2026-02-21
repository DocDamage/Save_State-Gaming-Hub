using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.SaveStates;
using SaveState.Core.Search.Models;
using SaveState.Core.Search.Services;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;

namespace SaveState.Infrastructure.Search.Providers;

/// <summary>
/// Search provider for save states.
/// </summary>
public sealed class SaveStateSearchProvider : ISearchProvider
{
    private readonly ISaveStateRepository _saveStateRepository;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<SaveStateSearchProvider> _logger;

    public SaveStateSearchProvider(
        ISaveStateRepository saveStateRepository,
        IGameRepository gameRepository,
        ILogger<SaveStateSearchProvider> logger)
    {
        _saveStateRepository = saveStateRepository;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public SearchScope Scope => SearchScope.Saves;

    /// <inheritdoc />
    public int Priority => 70;

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken ct = default)
    {
        try
        {
            // Get all games first to find their save states
            var games = await _gameRepository.GetAllAsync(ct);
            var allSaveStates = new List<(SaveStateEntity SaveState, string GameTitle)>();

            foreach (var game in games.Take(20)) // Limit to prevent too many queries
            {
                var saveStates = await _saveStateRepository.GetByGameIdAsync(game.Id, ct);
                foreach (var saveState in saveStates.Take(10))
                {
                    allSaveStates.Add((saveState, game.Title));
                }
            }

            var queryLower = query.Query.ToLowerInvariant();
            var queryTerms = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var results = allSaveStates
                .Select(s => new
                {
                    s.SaveState,
                    s.GameTitle,
                    Score = CalculateRelevance(s.SaveState, s.GameTitle, queryTerms, queryLower)
                })
                .Where(x => x.Score > 0.2f)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.SaveState.CreatedAt)
                .Take(query.MaxResults)
                .Select(x => CreateSearchResult(x.SaveState, x.GameTitle, x.Score))
                .ToList();

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search save states");
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

            foreach (var game in games.Take(20))
            {
                var saveStates = await _saveStateRepository.GetByGameIdAsync(game.Id, ct);
                foreach (var saveState in saveStates.Take(10))
                {
                    var description = saveState.Description ?? $"Save at {saveState.PlaytimeAtSave:hh\\:mm\\:ss}";
                    entries.Add(new SearchIndexEntry
                    {
                        Id = $"savestate:{saveState.Id}",
                        Type = "SaveState",
                        Title = $"Save for {game.Title}",
                        Content = description,
                        Embedding = new List<float>(),
                        Tags = new List<string>(),
                        LastUpdated = saveState.CreatedAt
                    });
                }
            }

            return entries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get indexable save states");
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

    private static float CalculateRelevance(SaveStateEntity saveState, string gameTitle, string[] queryTerms, string fullQuery)
    {
        var score = 0f;
        var gameTitleLower = gameTitle.ToLowerInvariant();
        var descLower = (saveState.Description ?? "").ToLowerInvariant();
        var locationLower = (saveState.GameLocation ?? "").ToLowerInvariant();

        // Game title match (highest weight)
        if (gameTitleLower.Contains(fullQuery))
        {
            score += 1.0f;
        }
        else
        {
            foreach (var term in queryTerms)
            {
                if (gameTitleLower.Contains(term))
                {
                    score += 0.5f;
                }
            }
        }

        // Description match
        if (!string.IsNullOrEmpty(saveState.Description))
        {
            foreach (var term in queryTerms)
            {
                if (descLower.Contains(term))
                {
                    score += 0.3f;
                }
            }
        }

        // Location match
        foreach (var term in queryTerms)
        {
            if (locationLower.Contains(term))
            {
                score += 0.2f;
            }
        }

        // Branch name match
        if (!string.IsNullOrEmpty(saveState.BranchName))
        {
            var branchLower = saveState.BranchName.ToLowerInvariant();
            foreach (var term in queryTerms)
            {
                if (branchLower.Contains(term))
                {
                    score += 0.25f;
                }
            }
        }

        return Math.Min(score, 1.0f);
    }

    private static SearchResult CreateSearchResult(SaveStateEntity saveState, string gameTitle, float score)
    {
        var subtitle = BuildSubtitle(saveState, gameTitle);

        return new SearchResult
        {
            Id = $"savestate:{saveState.Id}",
            Title = saveState.Description ?? $"Save for {gameTitle}",
            Subtitle = subtitle,
            Type = SearchResultType.SaveState,
            Icon = saveState.IsAutoSave ? "🤖" : "💾",
            RelevanceScore = score,
            Highlights = new List<string> { $"Playtime: {saveState.PlaytimeAtSave:hh\\:mm\\:ss}" },
            Action = async () =>
            {
                // Restore save state action
                return await Task.FromResult(Result.Success());
            },
            Shortcut = null
        };
    }

    private static string BuildSubtitle(SaveStateEntity saveState, string gameTitle)
    {
        var parts = new List<string> { gameTitle };

        var timeAgo = GetTimeAgo(saveState.CreatedAt);
        parts.Add(timeAgo);

        if (!string.IsNullOrEmpty(saveState.BranchName))
        {
            parts.Add($"Branch: {saveState.BranchName}");
        }

        if (saveState.IsAutoSave)
        {
            parts.Add("Auto");
        }

        if (saveState.IsFavorite)
        {
            parts.Add("★");
        }

        return string.Join(" • ", parts);
    }

    private static string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime;

        if (span.TotalMinutes < 1)
            return "Just now";
        if (span.TotalHours < 1)
            return $"{span.Minutes}m ago";
        if (span.TotalDays < 1)
            return $"{span.Hours}h ago";
        if (span.TotalDays < 7)
            return $"{span.Days}d ago";

        return dateTime.ToString("MMM dd");
    }
}
