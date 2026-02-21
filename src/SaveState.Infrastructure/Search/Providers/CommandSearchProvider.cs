using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Search.Models;
using SaveState.Core.Search.Services;
using System.Collections.Concurrent;

namespace SaveState.Infrastructure.Search.Providers;

/// <summary>
/// Search provider for application commands with keyboard shortcuts.
/// </summary>
public sealed class CommandSearchProvider : ISearchProvider
{
    private readonly ILogger<CommandSearchProvider> _logger;
    private readonly ConcurrentDictionary<string, SearchableCommand> _commands;

    public CommandSearchProvider(ILogger<CommandSearchProvider> logger)
    {
        _logger = logger;
        _commands = new ConcurrentDictionary<string, SearchableCommand>();
        InitializeDefaultCommands();
    }

    /// <inheritdoc />
    public SearchScope Scope => SearchScope.Commands;

    /// <inheritdoc />
    public int Priority => 95; // Highest priority for commands

    /// <inheritdoc />
    public Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken ct = default)
    {
        var queryLower = query.Query.ToLowerInvariant();
        var queryTerms = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var results = _commands.Values
            .Select(c => new { Command = c, Score = CalculateRelevance(c, queryTerms, queryLower) })
            .Where(x => x.Score > 0.3f)
            .OrderByDescending(x => x.Score)
            .Take(query.MaxResults)
            .Select(x => CreateSearchResult(x.Command, x.Score))
            .ToList();

        return Task.FromResult<IReadOnlyList<SearchResult>>(results);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SearchIndexEntry>> GetAllIndexableAsync(
        CancellationToken ct = default)
    {
        var entries = _commands.Values.Select(c => new SearchIndexEntry
        {
            Id = $"cmd:{c.Id}",
            Type = "Command",
            Title = c.Title,
            Content = $"{c.Title} {c.Description} {c.Shortcut} {string.Join(" ", c.Keywords)}",
            Embedding = new List<float>(),
            Tags = new List<string> { c.Category },
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
        var suggestions = _commands.Values
            .Where(c => c.Title.StartsWith(partialQuery, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Title)
            .Take(maxSuggestions)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(suggestions);
    }

    /// <summary>
    /// Registers a new command.
    /// </summary>
    public void RegisterCommand(SearchableCommand command)
    {
        _commands[command.Id] = command;
    }

    private static float CalculateRelevance(SearchableCommand command, string[] queryTerms, string fullQuery)
    {
        var score = 0f;
        var titleLower = command.Title.ToLowerInvariant();
        var shortcutLower = (command.Shortcut ?? "").ToLowerInvariant();

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
            if (command.Keywords.Any(k => k.ToLowerInvariant().Contains(term)))
            {
                score += 0.4f;
            }
        }

        // Shortcut match
        foreach (var term in queryTerms)
        {
            if (shortcutLower.Contains(term))
            {
                score += 0.3f;
            }
        }

        return Math.Min(score, 1.0f);
    }

    private static SearchResult CreateSearchResult(SearchableCommand command, float score)
    {
        return new SearchResult
        {
            Id = $"cmd:{command.Id}",
            Title = command.Title,
            Subtitle = $"{command.Category} • {command.Description}",
            Type = SearchResultType.Command,
            Icon = "⌨️",
            RelevanceScore = score,
            Highlights = new List<string> { $"Shortcut: {command.Shortcut}" },
            Action = async () =>
            {
                command.Execute?.Invoke();
                return await Task.FromResult(Result.Success());
            },
            Shortcut = command.Shortcut
        };
    }

    private void InitializeDefaultCommands()
    {
        // Navigation Commands
        RegisterCommand(new SearchableCommand
        {
            Id = "nav.library",
            Title = "Go to Library",
            Description = "Navigate to game library",
            Category = "Navigation",
            Keywords = new[] { "library", "games", "navigate", "go" },
            Shortcut = "Ctrl+1"
        });

        RegisterCommand(new SearchableCommand
        {
            Id = "nav.dashboard",
            Title = "Go to Dashboard",
            Description = "Navigate to dashboard",
            Category = "Navigation",
            Keywords = new[] { "dashboard", "home", "navigate", "go" },
            Shortcut = "Ctrl+2"
        });

        RegisterCommand(new SearchableCommand
        {
            Id = "nav.mugen",
            Title = "Go to MUGEN",
            Description = "Navigate to MUGEN hub",
            Category = "Navigation",
            Keywords = new[] { "mugen", "fighting", "navigate", "go" },
            Shortcut = "Ctrl+3"
        });

        RegisterCommand(new SearchableCommand
        {
            Id = "nav.settings",
            Title = "Go to Settings",
            Description = "Open settings page",
            Category = "Navigation",
            Keywords = new[] { "settings", "preferences", "options", "navigate" },
            Shortcut = "Ctrl+,"
        });

        // Search Commands
        RegisterCommand(new SearchableCommand
        {
            Id = "search.universal",
            Title = "Universal Search",
            Description = "Open universal search",
            Category = "Search",
            Keywords = new[] { "search", "find", "universal", "command palette" },
            Shortcut = "Ctrl+Shift+P"
        });

        RegisterCommand(new SearchableCommand
        {
            Id = "search.quick",
            Title = "Quick Search",
            Description = "Open quick game search",
            Category = "Search",
            Keywords = new[] { "search", "quick", "find", "game" },
            Shortcut = "Ctrl+P"
        });

        // View Commands
        RegisterCommand(new SearchableCommand
        {
            Id = "view.grid",
            Title = "Grid View",
            Description = "Switch to grid view",
            Category = "View",
            Keywords = new[] { "view", "grid", "layout", "display" },
            Shortcut = "Ctrl+Shift+1"
        });

        RegisterCommand(new SearchableCommand
        {
            Id = "view.list",
            Title = "List View",
            Description = "Switch to list view",
            Category = "View",
            Keywords = new[] { "view", "list", "layout", "display" },
            Shortcut = "Ctrl+Shift+2"
        });

        RegisterCommand(new SearchableCommand
        {
            Id = "view.fullscreen",
            Title = "Toggle Fullscreen",
            Description = "Toggle fullscreen mode",
            Category = "View",
            Keywords = new[] { "fullscreen", "window", "mode", "display" },
            Shortcut = "F11"
        });

        // Game Commands
        RegisterCommand(new SearchableCommand
        {
            Id = "game.launch",
            Title = "Launch Game",
            Description = "Launch selected game",
            Category = "Game",
            Keywords = new[] { "launch", "play", "start", "game", "run" },
            Shortcut = "Enter"
        });

        RegisterCommand(new SearchableCommand
        {
            Id = "game.favorite",
            Title = "Toggle Favorite",
            Description = "Add/remove game from favorites",
            Category = "Game",
            Keywords = new[] { "favorite", "star", "bookmark", "like" },
            Shortcut = "Ctrl+D"
        });

        // Save State Commands
        RegisterCommand(new SearchableCommand
        {
            Id = "savestate.create",
            Title = "Create Save State",
            Description = "Create new save state",
            Category = "Save State",
            Keywords = new[] { "save", "state", "create", "snapshot", "backup" },
            Shortcut = "F5"
        });

        RegisterCommand(new SearchableCommand
        {
            Id = "savestate.restore",
            Title = "Restore Save State",
            Description = "Restore previous save state",
            Category = "Save State",
            Keywords = new[] { "save", "state", "restore", "load", "previous" },
            Shortcut = "F9"
        });

        // Cloud Commands
        RegisterCommand(new SearchableCommand
        {
            Id = "cloud.sync",
            Title = "Sync to Cloud",
            Description = "Synchronize saves to cloud",
            Category = "Cloud",
            Keywords = new[] { "cloud", "sync", "upload", "backup" },
            Shortcut = "Ctrl+Shift+S"
        });

        RegisterCommand(new SearchableCommand
        {
            Id = "cloud.download",
            Title = "Download from Cloud",
            Description = "Download saves from cloud",
            Category = "Cloud",
            Keywords = new[] { "cloud", "download", "restore", "sync" },
            Shortcut = "Ctrl+Shift+D"
        });

        // Window Commands
        RegisterCommand(new SearchableCommand
        {
            Id = "window.minimize",
            Title = "Minimize Window",
            Description = "Minimize application window",
            Category = "Window",
            Keywords = new[] { "minimize", "window", "hide" },
            Shortcut = "Ctrl+M"
        });

        RegisterCommand(new SearchableCommand
        {
            Id = "window.close",
            Title = "Close Window",
            Description = "Close current window",
            Category = "Window",
            Keywords = new[] { "close", "window", "exit" },
            Shortcut = "Ctrl+W"
        });

        // Application Commands
        RegisterCommand(new SearchableCommand
        {
            Id = "app.quit",
            Title = "Quit Application",
            Description = "Exit SaveState",
            Category = "Application",
            Keywords = new[] { "quit", "exit", "close", "shutdown" },
            Shortcut = "Alt+F4"
        });

        RegisterCommand(new SearchableCommand
        {
            Id = "app.about",
            Title = "About",
            Description = "Show about dialog",
            Category = "Application",
            Keywords = new[] { "about", "version", "info", "credits" },
            Shortcut = null
        });
    }
}

/// <summary>
/// Represents a searchable command.
/// </summary>
public class SearchableCommand
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public required string[] Keywords { get; init; }
    public string? Shortcut { get; init; }
    public Action? Execute { get; init; }
}
