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
    Help
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
}

public record QuickActionGroup
{
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public List<QuickAction> Actions { get; set; } = new();
}

public record QuickActionContext
{
    public object? SelectedItem { get; set; }
    public string? CurrentView { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}
