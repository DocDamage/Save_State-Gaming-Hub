using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the command palette overlay.
/// </summary>
public partial class CommandPaletteViewModel : ObservableObject
{
    private readonly IOverlayService _overlayService;
    private string _searchText = string.Empty;

    public CommandPaletteViewModel(IOverlayService overlayService)
    {
        _overlayService = overlayService;

        // Initialize with some example commands
        AvailableCommands = new[]
        {
            new CommandItem("Scan for Games", "Scans for new games in configured directories", "scan"),
            new CommandItem("Random Game", "Picks a random game to play", "random"),
            new CommandItem("Voice Listen", "Start voice command recognition", "voice listen"),
            new CommandItem("Show Stats", "Display gaming statistics", "stats"),
            new CommandItem("Open Settings", "Open application settings", "settings"),
            new CommandItem("Clear Cache", "Clear application cache", "clear cache"),
            new CommandItem("Backup Data", "Create data backup", "backup"),
            new CommandItem("Check Updates", "Check for application updates", "update check")
        };
    }

    /// <summary>
    /// Gets or sets the search text.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                UpdateFilteredCommands();
            }
        }
    }

    /// <summary>
    /// Gets the filtered commands based on search text.
    /// </summary>
    public CommandItem[] FilteredCommands { get; private set; } = Array.Empty<CommandItem>();

    /// <summary>
    /// Gets all available commands.
    /// </summary>
    public CommandItem[] AvailableCommands { get; }

    /// <summary>
    /// Gets the selected command index.
    /// </summary>
    public int SelectedIndex { get; set; }

    /// <summary>
    /// Gets whether there are any filtered commands.
    /// </summary>
    public bool HasCommands => FilteredCommands.Length > 0;

    /// <summary>
    /// Command to execute the selected command.
    /// </summary>
    [RelayCommand]
    private void ExecuteSelected()
    {
        if (SelectedIndex >= 0 && SelectedIndex < FilteredCommands.Length)
        {
            ExecuteCommand(FilteredCommands[SelectedIndex]);
        }
    }

    /// <summary>
    /// Command to close the command palette.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        _overlayService.HideCommandPaletteOverlay();
    }

    /// <summary>
    /// Handles key input for navigation.
    /// </summary>
    /// <param name="key">The pressed key.</param>
    public void HandleKey(Avalonia.Input.Key key)
    {
        switch (key)
        {
            case Avalonia.Input.Key.Up:
                SelectedIndex = Math.Max(0, SelectedIndex - 1);
                OnPropertyChanged(nameof(SelectedIndex));
                break;
            case Avalonia.Input.Key.Down:
                SelectedIndex = Math.Min(FilteredCommands.Length - 1, SelectedIndex + 1);
                OnPropertyChanged(nameof(SelectedIndex));
                break;
            case Avalonia.Input.Key.Enter:
                ExecuteSelected();
                break;
            case Avalonia.Input.Key.Escape:
                Close();
                break;
        }
    }

    private void UpdateFilteredCommands()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredCommands = AvailableCommands;
        }
        else
        {
            FilteredCommands = AvailableCommands
                .Where(cmd =>
                    cmd.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    cmd.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    cmd.Command.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        SelectedIndex = FilteredCommands.Length > 0 ? 0 : -1;
        OnPropertyChanged(nameof(FilteredCommands));
        OnPropertyChanged(nameof(HasCommands));
        OnPropertyChanged(nameof(SelectedIndex));
    }

    private void ExecuteCommand(CommandItem command)
    {
        // TODO: Implement actual command execution
        // For now, just close the palette
        _overlayService.HideCommandPaletteOverlay();

        // Placeholder for command execution logic
        switch (command.Command)
        {
            case "scan":
                // TODO: Execute scan command
                break;
            case "random":
                // TODO: Execute random game command
                break;
            case "voice listen":
                // TODO: Start voice listening
                break;
            case "stats":
                // TODO: Show stats
                break;
            case "settings":
                // TODO: Open settings
                break;
            default:
                // Unknown command
                break;
        }
    }
}

/// <summary>
/// Represents a command item in the palette.
/// </summary>
public record CommandItem(string Name, string Description, string Command);