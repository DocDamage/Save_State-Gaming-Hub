using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using System.IO;
using System.Text.Json;

namespace SaveState.Presentation.Services.Keyboard;

/// <summary>
/// Implementation of keyboard navigation service.
/// </summary>
public class KeyboardNavigationService : IKeyboardNavigationService
{
    private readonly ILogger<KeyboardNavigationService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<Hotkey, Func<Task>> _hotkeys = new();
    private readonly List<Control> _navigationRoots = new();
    private readonly Dictionary<string, ShortcutDefinition> _shortcuts = new();
    private readonly Dictionary<string, Hotkey> _customShortcuts = new();
    
    private Rectangle? _focusVisual;
    private Window? _focusVisualWindow;
    private bool _isFocusVisualEnabled = true;
    private string _shortcutsFilePath;

    public bool IsFocusVisualEnabled
    {
        get => _isFocusVisualEnabled;
        set
        {
            _isFocusVisualEnabled = value;
            if (!value)
            {
                HideFocusVisual();
            }
        }
    }

    public KeyboardNavigationService(ILogger<KeyboardNavigationService> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _shortcutsFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SaveStateReborn",
            "keyboard-shortcuts.json");
        
        InitializeDefaultShortcuts();
        _ = LoadShortcutsAsync();
    }

    #region Navigation

    public void RegisterNavigationRoot(Control root)
    {
        if (!_navigationRoots.Contains(root))
        {
            _navigationRoots.Add(root);
            _logger.LogDebug("Registered navigation root: {Type}", root.GetType().Name);
        }
    }

    public void UnregisterNavigationRoot(Control root)
    {
        _navigationRoots.Remove(root);
        _logger.LogDebug("Unregistered navigation root: {Type}", root.GetType().Name);
    }

    public Task NavigateAsync(NavigationDirection direction)
    {
        var window = GetActiveWindow();
        if (window == null) return Task.CompletedTask;

        var focused = window.FocusManager?.GetFocusedElement() as Control;
        if (focused == null) return Task.CompletedTask;

        Control? nextElement = direction switch
        {
            NavigationDirection.Up => FindElementInDirection(focused, 0, -1),
            NavigationDirection.Down => FindElementInDirection(focused, 0, 1),
            NavigationDirection.Left => FindElementInDirection(focused, -1, 0),
            NavigationDirection.Right => FindElementInDirection(focused, 1, 0),
            NavigationDirection.Next => FindTabbableElement(focused, 1),
            NavigationDirection.Previous => FindTabbableElement(focused, -1),
            NavigationDirection.First => GetAllFocusableElements().FirstOrDefault(),
            NavigationDirection.Last => GetAllFocusableElements().LastOrDefault(),
            _ => null
        };

        if (nextElement != null && nextElement.Focusable)
        {
            nextElement.Focus();
            ShowFocusVisual(nextElement);
            _logger.LogDebug("Navigated {Direction} to {ElementType}", direction, nextElement.GetType().Name);
        }

        return Task.CompletedTask;
    }

    public Task ActivateCurrentAsync()
    {
        var window = GetActiveWindow();
        if (window == null) return Task.CompletedTask;

        var focused = window.FocusManager?.GetFocusedElement() as Control;
        if (focused is Button button)
        {
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
        else if (focused is CheckBox checkBox)
        {
            checkBox.IsChecked = !checkBox.IsChecked;
        }
        else if (focused is MenuItem menuItem)
        {
            menuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }

        return Task.CompletedTask;
    }

    public Task GoBackAsync()
    {
        // This would integrate with the navigation service
        _logger.LogDebug("Go back requested");
        return Task.CompletedTask;
    }

    #endregion

    #region Hotkeys

    public void RegisterHotkey(Hotkey hotkey, Func<Task> handler)
    {
        _hotkeys[hotkey] = handler;
        _logger.LogDebug("Registered hotkey: {Hotkey}", hotkey);
    }

    public void UnregisterHotkey(Hotkey hotkey)
    {
        _hotkeys.Remove(hotkey);
        _logger.LogDebug("Unregistered hotkey: {Hotkey}", hotkey);
    }

    public async Task<bool> HandleKeyAsync(Key key, KeyModifiers modifiers)
    {
        var hotkey = new Hotkey { Key = key, Modifiers = modifiers };
        
        // Check for exact match
        foreach (var kvp in _hotkeys)
        {
            if (kvp.Key.Key == key && kvp.Key.Modifiers == modifiers)
            {
                try
                {
                    await kvp.Value();
                    _logger.LogDebug("Executed hotkey: {Hotkey}", hotkey);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing hotkey: {Hotkey}", hotkey);
                }
            }
        }

        // Check shortcuts
        foreach (var shortcut in _shortcuts.Values)
        {
            var currentHotkey = shortcut.CurrentHotkey;
            if (currentHotkey.Key == key && currentHotkey.Modifiers == modifiers)
            {
                _logger.LogDebug("Shortcut triggered: {Shortcut}", shortcut.DisplayName);
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Shortcut Editor

    public Task<Dictionary<string, Hotkey>> GetShortcutsAsync()
    {
        var result = _shortcuts.ToDictionary(
            s => s.Key,
            s => s.Value.CurrentHotkey);
        return Task.FromResult(result);
    }

    public Task SetShortcutAsync(string actionId, Hotkey hotkey)
    {
        if (_shortcuts.TryGetValue(actionId, out var shortcut))
        {
            shortcut.CustomHotkey = hotkey;
            _customShortcuts[actionId] = hotkey;
            _logger.LogInformation("Set shortcut {ActionId} to {Hotkey}", actionId, hotkey);
        }
        
        return Task.CompletedTask;
    }

    public Task ResetShortcutsAsync()
    {
        foreach (var shortcut in _shortcuts.Values)
        {
            shortcut.CustomHotkey = null;
        }
        _customShortcuts.Clear();
        _logger.LogInformation("Reset all shortcuts to defaults");
        return Task.CompletedTask;
    }

    public async Task ExportShortcutsAsync(string path)
    {
        try
        {
            var data = new ShortcutExportData
            {
                Version = 1,
                ExportedAt = _timeProvider.UtcNow,
                Shortcuts = _shortcuts.ToDictionary(
                    s => s.Key,
                    s => new ShortcutData
                    {
                        DefaultHotkey = s.Value.DefaultHotkey,
                        CustomHotkey = s.Value.CustomHotkey
                    })
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(path, json);
            _logger.LogInformation("Exported shortcuts to {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export shortcuts");
        }
    }

    public async Task ImportShortcutsAsync(string path)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path);
            var data = JsonSerializer.Deserialize<ShortcutExportData>(json);

            if (data?.Shortcuts != null)
            {
                foreach (var kvp in data.Shortcuts)
                {
                    if (_shortcuts.TryGetValue(kvp.Key, out var shortcut))
                    {
                        shortcut.CustomHotkey = kvp.Value.CustomHotkey;
                        if (kvp.Value.CustomHotkey != null)
                        {
                            _customShortcuts[kvp.Key] = kvp.Value.CustomHotkey;
                        }
                    }
                }
                
                await SaveShortcutsAsync();
                _logger.LogInformation("Imported shortcuts from {Path}", path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import shortcuts");
        }
    }

    public IReadOnlyList<ShortcutDefinition> GetShortcutDefinitions()
    {
        return _shortcuts.Values.ToList().AsReadOnly();
    }

    public void ResetShortcut(string actionId)
    {
        if (_shortcuts.TryGetValue(actionId, out var shortcut))
        {
            shortcut.CustomHotkey = null;
            _customShortcuts.Remove(actionId);
            _logger.LogInformation("Reset shortcut: {ActionId}", actionId);
        }
    }

    #endregion

    #region Focus Visualization

    public void ShowFocusVisual(Control element)
    {
        if (!_isFocusVisualEnabled || element == null) return;

        HideFocusVisual();

        var window = GetActiveWindow();
        if (window == null) return;

        _focusVisualWindow = window;

        // Create focus rectangle
        _focusVisual = new Rectangle
        {
            Stroke = new SolidColorBrush(Colors.LimeGreen),
            StrokeThickness = 3,
            Fill = null,
            IsHitTestVisible = false,
            Opacity = 0.8
        };

        // Position the focus visual
        var position = GetAbsolutePosition(element);
        var bounds = element.Bounds;

        Canvas.SetLeft(_focusVisual, position.X - 2);
        Canvas.SetTop(_focusVisual, position.Y - 2);
        _focusVisual.Width = bounds.Width + 4;
        _focusVisual.Height = bounds.Height + 4;

        // Add to adorner layer or overlay
        if (window.Content is Panel rootPanel)
        {
            // Create overlay canvas if needed
            var overlay = rootPanel.Children.OfType<Canvas>().FirstOrDefault(c => c.Name == "FocusOverlay");
            if (overlay == null)
            {
                overlay = new Canvas
                {
                    Name = "FocusOverlay",
                    Background = null,
                    IsHitTestVisible = false
                };
                
                // Make overlay fill the window
                overlay.HorizontalAlignment = HorizontalAlignment.Stretch;
                overlay.VerticalAlignment = VerticalAlignment.Stretch;
                
                rootPanel.Children.Add(overlay);
            }
            
            overlay.Children.Add(_focusVisual);
        }
    }

    public void HideFocusVisual()
    {
        if (_focusVisual != null && _focusVisualWindow?.Content is Panel rootPanel)
        {
            var overlay = rootPanel.Children.OfType<Canvas>().FirstOrDefault(c => c.Name == "FocusOverlay");
            overlay?.Children.Remove(_focusVisual);
            _focusVisual = null;
        }
    }

    #endregion

    #region Helper Methods

    private void InitializeDefaultShortcuts()
    {
        // Global shortcuts
        AddShortcut("global.quicksearch", "Global", "Quick Search", "Open the quick search overlay", 
            Key.K, KeyModifiers.Control);
        AddShortcut("global.commandpalette", "Global", "Command Palette", "Open the command palette", 
            Key.P, KeyModifiers.Control | KeyModifiers.Shift);
        AddShortcut("global.screenshot", "Global", "Screenshot", "Take a screenshot", 
            Key.PrintScreen);

        // Navigation shortcuts
        AddShortcut("nav.back", "Navigation", "Go Back", "Navigate to the previous page", 
            Key.Left, KeyModifiers.Alt);
        AddShortcut("nav.forward", "Navigation", "Go Forward", "Navigate to the next page", 
            Key.Right, KeyModifiers.Alt);
        AddShortcut("nav.search", "Navigation", "Focus Search", "Focus the search box", 
            Key.F, KeyModifiers.Control);

        // Game Library shortcuts
        AddShortcut("library.launch", "Game Library", "Launch Selected", "Launch the selected game", 
            Key.Enter);
        AddShortcut("library.launch.bigpicture", "Game Library", "Launch Big Picture", "Launch in big picture mode", 
            Key.Enter, KeyModifiers.Control);
        AddShortcut("library.favorite", "Game Library", "Toggle Favorite", "Toggle favorite status", 
            Key.D, KeyModifiers.Control);
        AddShortcut("library.edit", "Game Library", "Edit Game Details", "Edit game details", 
            Key.E, KeyModifiers.Control);
        AddShortcut("library.delete", "Game Library", "Delete Selected", "Delete the selected game", 
            Key.Delete);

        // Save State shortcuts
        AddShortcut("savestate.quicksave", "Save States", "Quick Save", "Create a quick save", 
            Key.F5);
        AddShortcut("savestate.quickload", "Save States", "Quick Load", "Load the quick save", 
            Key.F9);
        AddShortcut("savestate.create", "Save States", "Create Save State", "Create a new save state", 
            Key.S, KeyModifiers.Control);
        AddShortcut("savestate.load", "Save States", "Load Save State", "Load a save state", 
            Key.L, KeyModifiers.Control);
    }

    private void AddShortcut(string id, string category, string displayName, string description, Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        _shortcuts[id] = new ShortcutDefinition
        {
            Id = id,
            Category = category,
            DisplayName = displayName,
            Description = description,
            DefaultHotkey = new Hotkey
            {
                Key = key,
                Modifiers = modifiers,
                ActionId = id,
                Category = category,
                Description = description
            }
        };
    }

    private async Task LoadShortcutsAsync()
    {
        try
        {
            if (!File.Exists(_shortcutsFilePath))
            {
                return;
            }

            var json = await File.ReadAllTextAsync(_shortcutsFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, HotkeyData>>(json);

            if (data != null)
            {
                foreach (var kvp in data)
                {
                    if (_shortcuts.TryGetValue(kvp.Key, out var shortcut) && kvp.Value.CustomKey != null)
                    {
                        shortcut.CustomHotkey = new Hotkey
                        {
                            Key = Enum.Parse<Key>(kvp.Value.CustomKey),
                            Modifiers = (KeyModifiers)kvp.Value.CustomModifiers,
                            ActionId = kvp.Key,
                            Category = shortcut.Category,
                            Description = shortcut.Description
                        };
                    }
                }
            }

            _logger.LogInformation("Loaded custom keyboard shortcuts");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load keyboard shortcuts");
        }
    }

    private async Task SaveShortcutsAsync()
    {
        try
        {
            var data = _shortcuts
                .Where(s => s.Value.CustomHotkey != null)
                .ToDictionary(
                    s => s.Key,
                    s => new HotkeyData
                    {
                        DefaultKey = s.Value.DefaultHotkey.Key.ToString(),
                        DefaultModifiers = (int)s.Value.DefaultHotkey.Modifiers,
                        CustomKey = s.Value.CustomHotkey?.Key.ToString(),
                        CustomModifiers = (int)(s.Value.CustomHotkey?.Modifiers ?? KeyModifiers.None)
                    });

            var directory = System.IO.Path.GetDirectoryName(_shortcutsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_shortcutsFilePath, json);
            _logger.LogInformation("Saved custom keyboard shortcuts");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save keyboard shortcuts");
        }
    }

    private Window? GetActiveWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    private Control? FindElementInDirection(Control current, int xDirection, int yDirection)
    {
        var currentPos = GetAbsolutePosition(current);
        var currentBounds = current.Bounds;
        var currentCenter = new Point(
            currentPos.X + currentBounds.Width / 2,
            currentPos.Y + currentBounds.Height / 2);

        Control? bestMatch = null;
        double bestScore = double.MaxValue;

        foreach (var element in GetAllFocusableElements())
        {
            if (element == current) continue;

            var elementPos = GetAbsolutePosition(element);
            var elementBounds = element.Bounds;
            var elementCenter = new Point(
                elementPos.X + elementBounds.Width / 2,
                elementPos.Y + elementBounds.Height / 2);

            var dx = elementCenter.X - currentCenter.X;
            var dy = elementCenter.Y - currentCenter.Y;

            // Check if element is in the correct direction
            bool inDirection = (xDirection > 0 && dx > 0) ||
                              (xDirection < 0 && dx < 0) ||
                              (yDirection > 0 && dy > 0) ||
                              (yDirection < 0 && dy < 0);

            if (!inDirection) continue;

            // Calculate distance score
            double distance = Math.Sqrt(dx * dx + dy * dy);
            double anglePenalty = CalculateAnglePenalty(dx, dy, xDirection, yDirection);
            double score = distance + anglePenalty;

            if (score < bestScore)
            {
                bestScore = score;
                bestMatch = element;
            }
        }

        return bestMatch;
    }

    private double CalculateAnglePenalty(double dx, double dy, int xDirection, int yDirection)
    {
        // Penalize elements that are significantly off-axis
        if (xDirection != 0)
        {
            var angle = Math.Abs(Math.Atan2(dy, dx));
            return angle * 100; // Penalty increases with angle
        }
        if (yDirection != 0)
        {
            var angle = Math.Abs(Math.Atan2(dx, dy));
            return angle * 100;
        }
        return 0;
    }

    private Control? FindTabbableElement(Control current, int direction)
    {
        var elements = GetAllFocusableElements().ToList();
        var currentIndex = elements.IndexOf(current);
        
        if (currentIndex < 0) return null;

        var nextIndex = currentIndex + direction;
        if (nextIndex < 0) nextIndex = elements.Count - 1;
        if (nextIndex >= elements.Count) nextIndex = 0;

        return elements.ElementAtOrDefault(nextIndex);
    }

    private IEnumerable<Control> GetAllFocusableElements()
    {
        var window = GetActiveWindow();
        if (window == null) return Enumerable.Empty<Control>();

        return window.GetVisualDescendants()
            .OfType<Control>()
            .Where(c => c.Focusable && c.IsVisible && c.IsEnabled)
            .OrderBy(c => c.TabIndex)
            .ThenBy(GetVisualOrder);
    }

    private int GetVisualOrder(Control control)
    {
        // Simple visual ordering based on position
        var pos = GetAbsolutePosition(control);
        return (int)(pos.Y * 10000 + pos.X);
    }

    private Point GetAbsolutePosition(Control control)
    {
        var position = new Point(0, 0);
        var current = control;

        while (current != null)
        {
            position = new Point(
                position.X + current.Bounds.X,
                position.Y + current.Bounds.Y);
            current = current.Parent as Control;
        }

        return position;
    }

    #endregion

    #region Data Classes

    private class HotkeyData
    {
        public string DefaultKey { get; set; } = string.Empty;
        public int DefaultModifiers { get; set; }
        public string? CustomKey { get; set; }
        public int CustomModifiers { get; set; }
    }

    private class ShortcutExportData
    {
        public int Version { get; set; }
        public DateTime ExportedAt { get; set; }
        public Dictionary<string, ShortcutData> Shortcuts { get; set; } = new();
    }

    private class ShortcutData
    {
        public Hotkey DefaultHotkey { get; set; } = new();
        public Hotkey? CustomHotkey { get; set; }
    }

    #endregion
}
