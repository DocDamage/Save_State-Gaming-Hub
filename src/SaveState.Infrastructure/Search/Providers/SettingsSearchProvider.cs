using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Search.Models;
using SaveState.Core.Search.Services;
using System.Collections.Concurrent;

namespace SaveState.Infrastructure.Search.Providers;

/// <summary>
/// Search provider for application settings and preferences.
/// </summary>
public sealed class SettingsSearchProvider : ISearchProvider
{
    private readonly ILogger<SettingsSearchProvider> _logger;
    private readonly ConcurrentDictionary<string, SearchableSetting> _settings;

    public SettingsSearchProvider(ILogger<SettingsSearchProvider> logger)
    {
        _logger = logger;
        _settings = new ConcurrentDictionary<string, SearchableSetting>();
        InitializeDefaultSettings();
    }

    /// <inheritdoc />
    public SearchScope Scope => SearchScope.Settings;

    /// <inheritdoc />
    public int Priority => 80;

    /// <inheritdoc />
    public Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken ct = default)
    {
        var queryLower = query.Query.ToLowerInvariant();
        var queryTerms = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var results = _settings.Values
            .Select(s => new { Setting = s, Score = CalculateRelevance(s, queryTerms, queryLower) })
            .Where(x => x.Score > 0.3f)
            .OrderByDescending(x => x.Score)
            .Take(query.MaxResults)
            .Select(x => CreateSearchResult(x.Setting, x.Score))
            .ToList();

        return Task.FromResult<IReadOnlyList<SearchResult>>(results);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SearchIndexEntry>> GetAllIndexableAsync(
        CancellationToken ct = default)
    {
        var entries = _settings.Values.Select(s => new SearchIndexEntry
        {
            Id = $"setting:{s.Id}",
            Type = "Setting",
            Title = s.Title,
            Content = $"{s.Title} {s.Description} {s.Category}",
            Embedding = new List<float>(), // Would be populated with actual embeddings
            Tags = new List<string> { s.Category },
            LastUpdated = DateTime.UtcNow
        }).ToList();

        return Task.FromResult<IReadOnlyList<SearchIndexEntry>>(entries);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetSuggestionsAsync(
        string partialQuery,
        int maxSuggestions = 5,
        CancellationToken ct = default)
    {
        var suggestions = _settings.Values
            .Where(s => s.Title.StartsWith(partialQuery, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Title)
            .Take(maxSuggestions)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(suggestions);
    }

    /// <summary>
    /// Registers a new setting for search.
    /// </summary>
    public void RegisterSetting(SearchableSetting setting)
    {
        _settings[setting.Id] = setting;
    }

    private static float CalculateRelevance(SearchableSetting setting, string[] queryTerms, string fullQuery)
    {
        var score = 0f;
        var titleLower = setting.Title.ToLowerInvariant();
        var descLower = setting.Description.ToLowerInvariant();
        var categoryLower = setting.Category.ToLowerInvariant();

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

        // Keyword match
        foreach (var term in queryTerms)
        {
            if (setting.Keywords.Any(k => k.ToLowerInvariant().Contains(term)))
            {
                score += 0.3f;
            }
        }

        // Description match
        foreach (var term in queryTerms)
        {
            if (descLower.Contains(term))
            {
                score += 0.2f;
            }
        }

        // Category match
        foreach (var term in queryTerms)
        {
            if (categoryLower.Contains(term))
            {
                score += 0.15f;
            }
        }

        return Math.Min(score, 1.0f);
    }

    private static SearchResult CreateSearchResult(SearchableSetting setting, float score)
    {
        return new SearchResult
        {
            Id = $"setting:{setting.Id}",
            Title = setting.Title,
            Subtitle = $"{setting.Category} • {setting.Description}",
            Type = SearchResultType.Setting,
            Icon = "⚙️",
            RelevanceScore = score,
            Highlights = new List<string> { setting.Description },
            Action = async () =>
            {
                setting.OnClick?.Invoke();
                return await Task.FromResult(Result.Success());
            },
            Shortcut = null
        };
    }

    private void InitializeDefaultSettings()
    {
        // General Settings
        RegisterSetting(new SearchableSetting
        {
            Id = "general.language",
            Title = "Language",
            Description = "Change application language",
            Category = "General",
            Keywords = new[] { "language", "locale", "region", "translation" },
            OnClick = () => { }
        });

        RegisterSetting(new SearchableSetting
        {
            Id = "general.theme",
            Title = "Theme",
            Description = "Change application theme",
            Category = "General",
            Keywords = new[] { "theme", "dark", "light", "appearance", "color" },
            OnClick = () => { }
        });

        // Library Settings
        RegisterSetting(new SearchableSetting
        {
            Id = "library.paths",
            Title = "Library Paths",
            Description = "Manage game library folders",
            Category = "Library",
            Keywords = new[] { "library", "path", "folder", "directory", "scan" },
            OnClick = () => { }
        });

        RegisterSetting(new SearchableSetting
        {
            Id = "library.platforms",
            Title = "Platform Integration",
            Description = "Configure Steam, GOG, Epic integration",
            Category = "Library",
            Keywords = new[] { "steam", "gog", "epic", "platform", "integration", "launcher" },
            OnClick = () => { }
        });

        // Cloud Sync Settings
        RegisterSetting(new SearchableSetting
        {
            Id = "cloud.providers",
            Title = "Cloud Providers",
            Description = "Configure cloud storage providers",
            Category = "Cloud Sync",
            Keywords = new[] { "cloud", "sync", "backup", "google drive", "onedrive", "dropbox" },
            OnClick = () => { }
        });

        // AI Settings
        RegisterSetting(new SearchableSetting
        {
            Id = "ai.configuration",
            Title = "AI Configuration",
            Description = "Configure AI assistant and features",
            Category = "AI",
            Keywords = new[] { "ai", "assistant", "openai", "gpt", "chat" },
            OnClick = () => { }
        });

        // Save State Settings
        RegisterSetting(new SearchableSetting
        {
            Id = "savestate.autosave",
            Title = "Auto Save",
            Description = "Configure automatic save state creation",
            Category = "Save States",
            Keywords = new[] { "autosave", "automatic", "interval", "backup" },
            OnClick = () => { }
        });

        // MUGEN Settings
        RegisterSetting(new SearchableSetting
        {
            Id = "mugen.paths",
            Title = "MUGEN Configuration",
            Description = "Configure MUGEN/IKEMEN paths and settings",
            Category = "MUGEN",
            Keywords = new[] { "mugen", "ikemen", "fighting", "engine", "characters" },
            OnClick = () => { }
        });
    }
}

/// <summary>
/// Represents a searchable setting.
/// </summary>
public class SearchableSetting
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public required string[] Keywords { get; init; }
    public Action? OnClick { get; init; }
}
