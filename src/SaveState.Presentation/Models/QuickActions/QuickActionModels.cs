using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaveState.Presentation.Models.QuickActions;

public enum QuickActionCategory
{
    Game,
    SaveState,
    Screenshot,
    Recording,
    Social,
    Settings,
    Tools,
    Help,
    Navigation,
    Edit,
    View,
    File
}

public record QuickAction
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Description { get; set; }
    public string? KeyboardShortcut { get; set; }
    public QuickActionCategory Category { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public int Priority { get; set; }
    public Func<Task>? ExecuteAsync { get; set; }
    public string? ConfirmationMessage { get; set; }
    public List<string> Tags { get; set; } = new();
}

public record QuickActionGroup
{
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public QuickActionCategory Category { get; set; }
    public int Priority { get; set; }
    public bool IsExpanded { get; set; } = true;
    public List<QuickAction> Actions { get; set; } = new();
}

public record QuickActionContext
{
    public object? SelectedItem { get; set; }
    public string? CurrentView { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
    public object? SelectedGame { get; set; }
    public object? SelectedSaveState { get; set; }

    /// <summary>
    /// Gets an empty context.
    /// </summary>
    public static QuickActionContext Empty => new();

    /// <summary>
    /// Creates a context for a specific game.
    /// </summary>
    public static QuickActionContext ForGame(object game)
    {
        return new QuickActionContext { SelectedItem = game, SelectedGame = game };
    }
}
