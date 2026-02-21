using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Search.Models;
using SaveState.Core.Search.Services;
using System.Collections.Concurrent;

namespace SaveState.Infrastructure.Search.Providers;

/// <summary>
/// Search provider for application actions and commands.
/// </summary>
public sealed class ActionSearchProvider : ISearchProvider
{
    private readonly ILogger<ActionSearchProvider> _logger;
    private readonly ConcurrentDictionary<string, SearchableAction> _actions;

    public ActionSearchProvider(ILogger<ActionSearchProvider> logger)
    {
        _logger = logger;
        _actions = new ConcurrentDictionary<string, SearchableAction>();
        InitializeDefaultActions();
    }

    /// <inheritdoc />
    public SearchScope Scope => SearchScope.Actions;

    /// <inheritdoc />
    public int Priority => 90; // High priority for quick actions

    /// <inheritdoc />
    public Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken ct = default)
    {
        var queryLower = query.Query.ToLowerInvariant();
        var queryTerms = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var results = _actions.Values
            .Select(a => new { Action = a, Score = CalculateRelevance(a, queryTerms, queryLower) })
            .Where(x => x.Score > 0.3f)
            .OrderByDescending(x => x.Score)
            .Take(query.MaxResults)
            .Select(x => CreateSearchResult(x.Action, x.Score))
            .ToList();

        return Task.FromResult<IReadOnlyList<SearchResult>>(results);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SearchIndexEntry>> GetAllIndexableAsync(
        CancellationToken ct = default)
    {
        var entries = _actions.Values.Select(a => new SearchIndexEntry
        {
            Id = $"action:{a.Id}",
            Type = "Action",
            Title = a.Title,
            Content = $"{a.Title} {a.Description} {string.Join(" ", a.Keywords)}",
            Embedding = new List<float>(),
            Tags = new List<string> { a.Category },
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
        var suggestions = _actions.Values
            .Where(a => a.Title.StartsWith(partialQuery, StringComparison.OrdinalIgnoreCase) ||
                       a.Keywords.Any(k => k.StartsWith(partialQuery, StringComparison.OrdinalIgnoreCase)))
            .Select(a => a.Title)
            .Take(maxSuggestions)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(suggestions);
    }

    /// <summary>
    /// Registers a new searchable action.
    /// </summary>
    public void RegisterAction(SearchableAction action)
    {
        _actions[action.Id] = action;
        _logger.LogDebug("Registered action: {ActionId}", action.Id);
    }

    /// <summary>
    /// Unregisters an action.
    /// </summary>
    public void UnregisterAction(string actionId)
    {
        _actions.TryRemove(actionId, out _);
    }

    private static float CalculateRelevance(SearchableAction action, string[] queryTerms, string fullQuery)
    {
        var score = 0f;
        var titleLower = action.Title.ToLowerInvariant();
        var descLower = action.Description.ToLowerInvariant();

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
                    score += 0.6f;
                }
            }
        }

        // Keyword match
        foreach (var term in queryTerms)
        {
            if (action.Keywords.Any(k => k.ToLowerInvariant().Contains(term)))
            {
                score += 0.4f;
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
        if (!string.IsNullOrEmpty(action.Category))
        {
            var categoryLower = action.Category.ToLowerInvariant();
            foreach (var term in queryTerms)
            {
                if (categoryLower.Contains(term))
                {
                    score += 0.15f;
                }
            }
        }

        return Math.Min(score, 1.0f);
    }

    private static SearchResult CreateSearchResult(SearchableAction action, float score)
    {
        return new SearchResult
        {
            Id = $"action:{action.Id}",
            Title = action.Title,
            Subtitle = $"{action.Category} • {action.Description}",
            Type = SearchResultType.Action,
            Icon = action.Icon,
            RelevanceScore = score,
            Highlights = new List<string> { action.Description },
            Action = action.Execute,
            Shortcut = action.Shortcut
        };
    }

    private void InitializeDefaultActions()
    {
        // Library Actions
        RegisterAction(new SearchableAction
        {
            Id = "library.add-game",
            Title = "Add Game",
            Description = "Add a new game to your library",
            Category = "Library",
            Icon = "➕",
            Keywords = new[] { "add", "game", "import", "new", "install" },
            Execute = async () => await Task.FromResult(Result.Success()),
            Shortcut = "Ctrl+N"
        });

        RegisterAction(new SearchableAction
        {
            Id = "library.scan",
            Title = "Scan for Games",
            Description = "Scan library folders for new games",
            Category = "Library",
            Icon = "🔍",
            Keywords = new[] { "scan", "find", "detect", "import", "library" },
            Execute = async () => await Task.FromResult(Result.Success()),
            Shortcut = "Ctrl+R"
        });

        RegisterAction(new SearchableAction
        {
            Id = "library.import-steam",
            Title = "Import Steam Games",
            Description = "Import games from Steam library",
            Category = "Library",
            Icon = "🎮",
            Keywords = new[] { "steam", "import", "valve", "games", "library" },
            Execute = async () => await Task.FromResult(Result.Success())
        });

        // Save State Actions
        RegisterAction(new SearchableAction
        {
            Id = "savestate.create",
            Title = "Create Save State",
            Description = "Create a new save state for current game",
            Category = "Save States",
            Icon = "💾",
            Keywords = new[] { "save", "state", "create", "backup", "snapshot" },
            Execute = async () => await Task.FromResult(Result.Success()),
            Shortcut = "F5"
        });

        RegisterAction(new SearchableAction
        {
            Id = "savestate.restore",
            Title = "Restore Save State",
            Description = "Restore a previous save state",
            Category = "Save States",
            Icon = "📂",
            Keywords = new[] { "restore", "load", "save", "state", "previous" },
            Execute = async () => await Task.FromResult(Result.Success()),
            Shortcut = "F9"
        });

        // Cloud Actions
        RegisterAction(new SearchableAction
        {
            Id = "cloud.sync",
            Title = "Sync to Cloud",
            Description = "Synchronize save states to cloud storage",
            Category = "Cloud",
            Icon = "☁️",
            Keywords = new[] { "cloud", "sync", "backup", "upload", "save" },
            Execute = async () => await Task.FromResult(Result.Success()),
            Shortcut = "Ctrl+Shift+S"
        });

        // Tools Actions
        RegisterAction(new SearchableAction
        {
            Id = "tools.memory",
            Title = "Memory Scanner",
            Description = "Open the game memory scanner",
            Category = "Tools",
            Icon = "🔬",
            Keywords = new[] { "memory", "scan", "cheat", "trainer", "hack" },
            Execute = async () => await Task.FromResult(Result.Success())
        });

        RegisterAction(new SearchableAction
        {
            Id = "tools.macro",
            Title = "Macro Recorder",
            Description = "Record and play input macros",
            Category = "Tools",
            Icon = "⏺️",
            Keywords = new[] { "macro", "record", "automation", "input", "script" },
            Execute = async () => await Task.FromResult(Result.Success())
        });

        // MUGEN Actions
        RegisterAction(new SearchableAction
        {
            Id = "mugen.launch",
            Title = "Launch MUGEN",
            Description = "Launch MUGEN fighting game engine",
            Category = "MUGEN",
            Icon = "👊",
            Keywords = new[] { "mugen", "fighting", "launch", "game", "engine" },
            Execute = async () => await Task.FromResult(Result.Success())
        });

        RegisterAction(new SearchableAction
        {
            Id = "mugen.characters",
            Title = "Manage Characters",
            Description = "Manage MUGEN characters",
            Category = "MUGEN",
            Icon = "🥷",
            Keywords = new[] { "mugen", "character", "manage", "add", "remove" },
            Execute = async () => await Task.FromResult(Result.Success())
        });

        // View Actions
        RegisterAction(new SearchableAction
        {
            Id = "view.fullscreen",
            Title = "Toggle Fullscreen",
            Description = "Toggle fullscreen mode",
            Category = "View",
            Icon = "🖥️",
            Keywords = new[] { "fullscreen", "window", "mode", "display" },
            Execute = async () => await Task.FromResult(Result.Success()),
            Shortcut = "F11"
        });

        RegisterAction(new SearchableAction
        {
            Id = "view.bigpicture",
            Title = "Big Picture Mode",
            Description = "Enter Big Picture mode for TV gaming",
            Category = "View",
            Icon = "📺",
            Keywords = new[] { "big picture", "tv", "console", "mode", "fullscreen" },
            Execute = async () => await Task.FromResult(Result.Success())
        });

        // Help Actions
        RegisterAction(new SearchableAction
        {
            Id = "help.documentation",
            Title = "Documentation",
            Description = "Open help documentation",
            Category = "Help",
            Icon = "📖",
            Keywords = new[] { "help", "documentation", "guide", "manual", "docs" },
            Execute = async () => await Task.FromResult(Result.Success()),
            Shortcut = "F1"
        });

        RegisterAction(new SearchableAction
        {
            Id = "app.settings",
            Title = "Settings",
            Description = "Open application settings",
            Category = "Application",
            Icon = "⚙️",
            Keywords = new[] { "settings", "preferences", "options", "configuration" },
            Execute = async () => await Task.FromResult(Result.Success()),
            Shortcut = "Ctrl+,"
        });

        RegisterAction(new SearchableAction
        {
            Id = "app.quit",
            Title = "Quit",
            Description = "Exit the application",
            Category = "Application",
            Icon = "🚪",
            Keywords = new[] { "quit", "exit", "close", "shutdown" },
            Execute = async () => await Task.FromResult(Result.Success()),
            Shortcut = "Alt+F4"
        });
    }
}
