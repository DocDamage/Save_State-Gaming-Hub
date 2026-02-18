namespace SaveState.Application.Mugen.Services.UiUxEnhancement.Engines;

using Microsoft.Extensions.Logging;

/// <summary>
/// Engine responsible for generating menus and navigation graphs.
/// </summary>
public class MenuEngine
{
    private readonly ILogger<MenuEngine>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MenuEngine"/> class.
    /// </summary>
    public MenuEngine()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MenuEngine"/> class with a logger.
    /// </summary>
    public MenuEngine(ILogger<MenuEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates menu definitions based on enabled features.
    /// </summary>
    /// <param name="enabledFeatures">List of enabled feature identifiers.</param>
    /// <returns>List of menu definitions.</returns>
    public IReadOnlyList<Menu> GenerateMenus(IEnumerable<string> enabledFeatures)
    {
        var menus = new List<Menu>();
        var featuresList = enabledFeatures.ToList();

        _logger?.LogDebug("Generating menus for {Count} features", featuresList.Count);

        // Always include main menu
        menus.Add(CreateMainMenu());

        // Add feature-specific menus
        foreach (var feature in featuresList)
        {
            var featureMenus = GenerateMenusForFeature(feature);
            menus.AddRange(featureMenus);
        }

        // Always include settings menu
        menus.Add(CreateSettingsMenu());

        _logger?.LogInformation("Generated {Count} menus", menus.Count);
        return menus;
    }

    /// <summary>
    /// Builds a navigation graph connecting all enabled features.
    /// </summary>
    /// <param name="enabledFeatures">List of enabled feature identifiers.</param>
    /// <returns>Navigation graph containing nodes and shortcuts.</returns>
    public NavigationGraph BuildNavigationGraph(IEnumerable<string> enabledFeatures)
    {
        var featuresList = enabledFeatures.ToList();
        var nodes = new List<NavigationNode>();
        var shortcuts = new Dictionary<string, KeyboardShortcut>();

        _logger?.LogDebug("Building navigation graph for {Count} features", featuresList.Count);

        // Root node (main menu)
        nodes.Add(new NavigationNode
        {
            Feature = "main",
            Connections = featuresList.Concat(new[] { "settings" }).ToList()
        });

        // Feature nodes with connections
        foreach (var feature in featuresList)
        {
            var connections = new List<string> { "main" };

            // Connect to related features
            connections.AddRange(GetRelatedFeatures(feature, featuresList));

            nodes.Add(new NavigationNode
            {
                Feature = feature,
                Connections = connections
            });
        }

        // Settings node
        nodes.Add(new NavigationNode
        {
            Feature = "settings",
            Connections = new[] { "main", "audio", "video", "controls" }.ToList()
        });

        // Add keyboard shortcuts
        shortcuts["main"] = new KeyboardShortcut { Key = "Esc", Modifier = null };
        shortcuts["settings"] = new KeyboardShortcut { Key = "F1", Modifier = null };
        shortcuts["back"] = new KeyboardShortcut { Key = "Backspace", Modifier = null };

        var graph = new NavigationGraph
        {
            Nodes = nodes,
            Shortcuts = shortcuts
        };

        _logger?.LogInformation("Built navigation graph with {Count} nodes", nodes.Count);
        return graph;
    }

    private Menu CreateMainMenu()
    {
        return new Menu
        {
            Id = "main",
            Title = "Main Menu",
            Items = new List<MenuItem>
            {
                new() { Id = "arcade", Label = "Arcade Mode", Type = "mode" },
                new() { Id = "versus", Label = "Versus Mode", Type = "mode" },
                new() { Id = "training", Label = "Training Mode", Type = "mode" },
                new() { Id = "online", Label = "Online Play", Type = "mode" },
                new() { Id = "gallery", Label = "Gallery", Type = "submenu" },
                new() { Id = "options", Label = "Options", Type = "submenu" },
                new() { Id = "quit", Label = "Quit Game", Type = "action" }
            }
        };
    }

    private Menu CreateSettingsMenu()
    {
        return new Menu
        {
            Id = "settings",
            Title = "Settings",
            Items = new List<MenuItem>
            {
                new() { Id = "video", Label = "Video Settings", Type = "submenu" },
                new() { Id = "audio", Label = "Audio Settings", Type = "submenu" },
                new() { Id = "controls", Label = "Control Settings", Type = "submenu" },
                new() { Id = "gameplay", Label = "Gameplay Settings", Type = "submenu" },
                new() { Id = "accessibility", Label = "Accessibility", Type = "submenu" },
                new() { Id = "back", Label = "Back", Type = "back" }
            }
        };
    }

    private IEnumerable<Menu> GenerateMenusForFeature(string feature)
    {
        var menus = new List<Menu>();

        switch (feature.ToLowerInvariant())
        {
            case "character_select":
                menus.Add(new Menu
                {
                    Id = "character_select",
                    Title = "Select Character",
                    Items = new List<MenuItem>
                    {
                        new() { Id = "p1_select", Label = "Player 1 Select", Type = "selector" },
                        new() { Id = "p2_select", Label = "Player 2 Select", Type = "selector" },
                        new() { Id = "random", Label = "Random Select", Type = "action" },
                        new() { Id = "color", Label = "Color Select", Type = "selector" },
                        new() { Id = "confirm", Label = "Confirm", Type = "action" },
                        new() { Id = "back", Label = "Back", Type = "back" }
                    }
                });
                break;

            case "stage_select":
                menus.Add(new Menu
                {
                    Id = "stage_select",
                    Title = "Select Stage",
                    Items = new List<MenuItem>
                    {
                        new() { Id = "stage_grid", Label = "Stage Grid", Type = "grid" },
                        new() { Id = "random", Label = "Random Stage", Type = "action" },
                        new() { Id = "music", Label = "Music Select", Type = "selector" },
                        new() { Id = "confirm", Label = "Confirm", Type = "action" },
                        new() { Id = "back", Label = "Back", Type = "back" }
                    }
                });
                break;

            case "training_mode":
                menus.Add(new Menu
                {
                    Id = "training_settings",
                    Title = "Training Settings",
                    Items = new List<MenuItem>
                    {
                        new() { Id = "dummy_setting", Label = "Dummy Setting", Type = "selector" },
                        new() { Id = "meter_setting", Label = "Meter Settings", Type = "selector" },
                        new() { Id = "display_data", Label = "Display Data", Type = "toggle" },
                        new() { Id = "hitboxes", Label = "Show Hitboxes", Type = "toggle" },
                        new() { Id = "frame_data", Label = "Frame Data", Type = "toggle" },
                        new() { Id = "back", Label = "Back", Type = "back" }
                    }
                });
                break;

            case "online_lobby":
                menus.Add(new Menu
                {
                    Id = "online_lobby",
                    Title = "Online Lobby",
                    Items = new List<MenuItem>
                    {
                        new() { Id = "quick_match", Label = "Quick Match", Type = "action" },
                        new() { Id = "ranked", Label = "Ranked Match", Type = "action" },
                        new() { Id = "custom", Label = "Custom Lobby", Type = "action" },
                        new() { Id = "friends", Label = "Friends List", Type = "submenu" },
                        new() { Id = "leaderboard", Label = "Leaderboard", Type = "submenu" },
                        new() { Id = "back", Label = "Back", Type = "back" }
                    }
                });
                break;

            case "replay_theater":
                menus.Add(new Menu
                {
                    Id = "replay_menu",
                    Title = "Replay Theater",
                    Items = new List<MenuItem>
                    {
                        new() { Id = "local_replays", Label = "Local Replays", Type = "list" },
                        new() { Id = "online_replays", Label = "Online Replays", Type = "list" },
                        new() { Id = "saved", Label = "Saved Replays", Type = "list" },
                        new() { Id = "back", Label = "Back", Type = "back" }
                    }
                });
                break;

            case "shop":
                menus.Add(new Menu
                {
                    Id = "shop",
                    Title = "In-Game Shop",
                    Items = new List<MenuItem>
                    {
                        new() { Id = "characters", Label = "Characters", Type = "grid" },
                        new() { Id = "stages", Label = "Stages", Type = "grid" },
                        new() { Id = "colors", Label = "Color Palettes", Type = "grid" },
                        new() { Id = "bundles", Label = "Bundles", Type = "grid" },
                        new() { Id = "currency", Label = "Currency: 0000", Type = "display" },
                        new() { Id = "back", Label = "Back", Type = "back" }
                    }
                });
                break;
        }

        return menus;
    }

    private IEnumerable<string> GetRelatedFeatures(string feature, List<string> allFeatures)
    {
        var relatedMap = new Dictionary<string, string[]>
        {
            ["character_select"] = new[] { "stage_select" },
            ["stage_select"] = new[] { "character_select" },
            ["training_mode"] = new[] { "character_select", "stage_select" },
            ["online_lobby"] = new[] { "character_select", "ranked_match" },
            ["replay_theater"] = new[] { "gallery" },
            ["shop"] = new[] { "character_select" }
        };

        if (relatedMap.TryGetValue(feature.ToLowerInvariant(), out var related))
        {
            return related.Where(allFeatures.Contains);
        }

        return Enumerable.Empty<string>();
    }
}
