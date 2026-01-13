using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SaveState.Presentation.Services;

/// <summary>
/// Registry of all available application tabs.
/// </summary>
public static class TabRegistry
{
    /// <summary>
    /// Gets the dictionary of all registered tabs.
    /// </summary>
    public static readonly Dictionary<string, TabDefinition> Tabs = new()
    {
        ["Dashboard"] = new("Dashboard", "🏠", typeof(ViewModels.Shell.DashboardViewModel), Key.D1, KeyModifiers.Control),
        ["Library"] = new("Library", "📚", typeof(ViewModels.GameLibraryViewModel), Key.D2, KeyModifiers.Control),
        ["ROMs"] = new("ROMs", "🎮", typeof(ViewModels.Shell.RomManagementViewModel), Key.D3, KeyModifiers.Control),
        ["MUGEN"] = new("MUGEN", "🥊", typeof(ViewModels.Shell.MugenViewModel), Key.D4, KeyModifiers.Control),
        ["Analytics"] = new("Analytics", "📊", typeof(ViewModels.Shell.AnalyticsViewModel), Key.D5, KeyModifiers.Control),
        ["Social"] = new("Social", "👥", typeof(ViewModels.Shell.SocialViewModel), Key.D6, KeyModifiers.Control),
        ["Cloud"] = new("Cloud", "☁️", typeof(ViewModels.Shell.CloudSyncViewModel), Key.D7, KeyModifiers.Control),
        ["Automation"] = new("Automation", "🎬", typeof(ViewModels.Shell.AutomationViewModel), Key.D8, KeyModifiers.Control),
        ["Tools"] = new("Tools", "🛠️", typeof(ViewModels.Shell.ToolsViewModel), Key.D9, KeyModifiers.Control),
        ["Terminal"] = new("Terminal", "💻", typeof(ViewModels.Shell.TerminalViewModel), Key.D0, KeyModifiers.Control),
        ["Settings"] = new("Settings", "⚙️", typeof(ViewModels.SettingsViewModel), Key.F10, KeyModifiers.None),
    };

    /// <summary>
    /// Gets all tab definitions in display order.
    /// </summary>
    public static IEnumerable<TabDefinition> GetAllTabs()
    {
        yield return Tabs["Dashboard"];
        yield return Tabs["Library"];
        yield return Tabs["ROMs"];
        yield return Tabs["MUGEN"];
        yield return Tabs["Analytics"];
        yield return Tabs["Social"];
        yield return Tabs["Cloud"];
        yield return Tabs["Automation"];
        yield return Tabs["Tools"];
        yield return Tabs["Terminal"];
        yield return Tabs["Settings"];
    }

    /// <summary>
    /// Gets a tab definition by name.
    /// </summary>
    /// <param name="tabName">The name of the tab.</param>
    /// <returns>The tab definition, or null if not found.</returns>
    public static TabDefinition? GetTab(string tabName)
    {
        return Tabs.TryGetValue(tabName, out var tab) ? tab : null;
    }

    /// <summary>
    /// Gets the tab name for a given shortcut key combination.
    /// </summary>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="modifiers">The key modifiers.</param>
    /// <returns>The tab name, or null if no tab matches the shortcut.</returns>
    public static string? GetTabForShortcut(Key key, KeyModifiers modifiers)
    {
        foreach (var (name, tab) in Tabs)
        {
            if (tab.Shortcut == key && tab.Modifiers == modifiers)
            {
                return name;
            }
        }
        return null;
    }
}

/// <summary>
/// Defines a tab in the application.
/// </summary>
public record TabDefinition(
    string Name,
    string Icon,
    Type ViewModelType,
    Key Shortcut,
    KeyModifiers Modifiers)
{
    /// <summary>
    /// Gets the display label for the tab.
    /// </summary>
    public string Label => Name;

    /// <summary>
    /// Gets the tooltip text for the tab.
    /// </summary>
    public string TooltipText => $"{Name} (Ctrl+{Shortcut.ToString().TrimStart('D')})";
}
