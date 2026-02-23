using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Services.Keyboard;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// ViewModel for the shortcut editor view.
/// </summary>
public partial class ShortcutEditorViewModel : ObservableObject
{
    private readonly IKeyboardNavigationService _keyboardService;
    private readonly ILogger<ShortcutEditorViewModel> _logger;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ShortcutGroup> _groupedShortcuts = new();

    [ObservableProperty]
    private ShortcutDefinition? _currentlyEditing;

    [ObservableProperty]
    private bool _isRecordingShortcut;

    public ShortcutEditorViewModel(
        IKeyboardNavigationService keyboardService,
        ILogger<ShortcutEditorViewModel> logger)
    {
        _keyboardService = keyboardService;
        _logger = logger;

        LoadShortcuts();
    }

    private void LoadShortcuts()
    {
        var definitions = _keyboardService.GetShortcutDefinitions();
        var groups = definitions
            .Where(d => string.IsNullOrEmpty(SearchText) || 
                       d.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                       d.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            .GroupBy(d => d.Category)
            .Select(g => new ShortcutGroup
            {
                Category = g.Key,
                Shortcuts = new ObservableCollection<ShortcutDefinition>(g)
            });

        GroupedShortcuts = new ObservableCollection<ShortcutGroup>(groups);
    }

    partial void OnSearchTextChanged(string value)
    {
        LoadShortcuts();
    }

    [RelayCommand]
    private void ChangeShortcut(ShortcutDefinition shortcut)
    {
        CurrentlyEditing = shortcut;
        IsRecordingShortcut = true;
        _logger.LogInformation("Recording new shortcut for {Action}", shortcut.Id);
    }

    [RelayCommand]
    private void ResetShortcut(ShortcutDefinition shortcut)
    {
        _keyboardService.ResetShortcut(shortcut.Id);
        LoadShortcuts();
        _logger.LogInformation("Reset shortcut for {Action}", shortcut.Id);
    }

    [RelayCommand]
    private async Task ResetAllAsync()
    {
        await _keyboardService.ResetShortcutsAsync();
        LoadShortcuts();
        _logger.LogInformation("Reset all shortcuts");
    }

    [RelayCommand]
    private Task SaveAsync()
    {
        // Shortcuts are saved automatically when modified via SetShortcutAsync
        _logger.LogInformation("Saved shortcut changes");
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Cancel()
    {
        // Reload original shortcuts
        LoadShortcuts();
        _logger.LogInformation("Cancelled shortcut changes");
    }

    [RelayCommand]
    private async Task Export()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "SaveState_Shortcuts.json");
        
        await _keyboardService.ExportShortcutsAsync(path);
        _logger.LogInformation("Exported shortcuts to {Path}", path);
    }

    [RelayCommand]
    private async Task Import()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "SaveState_Shortcuts.json");
        
        if (File.Exists(path))
        {
            await _keyboardService.ImportShortcutsAsync(path);
            LoadShortcuts();
            _logger.LogInformation("Imported shortcuts from {Path}", path);
        }
    }

    /// <summary>
    /// Called when a key is pressed while recording a shortcut.
    /// </summary>
    public async Task OnKeyPressed(Key key, KeyModifiers modifiers)
    {
        if (!IsRecordingShortcut || CurrentlyEditing == null) return;

        // Ignore modifier-only keys
        if (key is Key.LeftCtrl or Key.RightCtrl or 
            Key.LeftShift or Key.RightShift or 
            Key.LeftAlt or Key.RightAlt)
        {
            return;
        }

        var newHotkey = new Hotkey
        {
            Key = key,
            Modifiers = modifiers,
            ActionId = CurrentlyEditing.Id,
            Category = CurrentlyEditing.Category,
            Description = CurrentlyEditing.Description
        };

        await _keyboardService.SetShortcutAsync(CurrentlyEditing.Id, newHotkey);
        
        IsRecordingShortcut = false;
        CurrentlyEditing = null;
        LoadShortcuts();
    }
}

/// <summary>
/// Represents a group of shortcuts by category.
/// </summary>
public class ShortcutGroup
{
    public string Category { get; set; } = string.Empty;
    public ObservableCollection<ShortcutDefinition> Shortcuts { get; set; } = new();
}
