using Avalonia.Controls;
using Avalonia.Input;

namespace SaveState.Presentation.Services.Keyboard;

/// <summary>
/// Service for managing keyboard navigation throughout the application.
/// </summary>
public interface IKeyboardNavigationService
{
    // Navigation
    void RegisterNavigationRoot(Control root);
    void UnregisterNavigationRoot(Control root);
    Task NavigateAsync(NavigationDirection direction);
    Task ActivateCurrentAsync();
    Task GoBackAsync();
    
    // Hotkeys
    void RegisterHotkey(Hotkey hotkey, Func<Task> handler);
    void UnregisterHotkey(Hotkey hotkey);
    Task<bool> HandleKeyAsync(Key key, KeyModifiers modifiers);
    
    // Shortcut Editor
    Task<Dictionary<string, Hotkey>> GetShortcutsAsync();
    Task SetShortcutAsync(string actionId, Hotkey hotkey);
    Task ResetShortcutsAsync();
    Task ExportShortcutsAsync(string path);
    Task ImportShortcutsAsync(string path);
    
    // Focus visualization
    bool IsFocusVisualEnabled { get; set; }
    void ShowFocusVisual(Control element);
    void HideFocusVisual();
}

/// <summary>
/// Represents a keyboard hotkey combination.
/// </summary>
public record Hotkey
{
    public Key Key { get; init; }
    public KeyModifiers Modifiers { get; init; }
    public string? DisplayName { get; init; }
    public string? ActionId { get; init; }
    public string? Category { get; init; }
    public string? Description { get; init; }
    
    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(KeyModifiers.Meta)) parts.Add("Win");
        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }

    public string ToDisplayString()
    {
        return DisplayName ?? ToString();
    }
}

/// <summary>
/// Represents a shortcut definition with metadata.
/// </summary>
public class ShortcutDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Hotkey DefaultHotkey { get; set; } = new();
    public Hotkey? CustomHotkey { get; set; }
    public bool IsCustomized => CustomHotkey != null;
    
    public Hotkey CurrentHotkey => CustomHotkey ?? DefaultHotkey;
}

/// <summary>
/// Navigation directions for keyboard navigation.
/// </summary>
public enum NavigationDirection
{
    Up,
    Down,
    Left,
    Right,
    Next,
    Previous,
    First,
    Last,
    PageUp,
    PageDown
}
