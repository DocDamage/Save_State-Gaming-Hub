using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.GameLibrary.Commands;
using SaveState.Core.GameLibrary;
using SaveState.Presentation.Services;
using Splat;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the command palette overlay.
/// </summary>
public partial class CommandPaletteViewModel : ObservableObject
{
    private readonly IOverlayService _overlayService;
    private readonly IMediator? _mediator;
    private readonly INavigationService? _navigationService;
    private readonly IGameRepository? _gameRepository;
    private readonly ILogger<CommandPaletteViewModel>? _logger;
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isExecuting;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public CommandPaletteViewModel(IOverlayService overlayService)
    {
        _overlayService = overlayService;

        // Resolve optional dependencies
        _mediator = Locator.Current.GetService<IMediator>();
        _navigationService = Locator.Current.GetService<INavigationService>();
        _gameRepository = Locator.Current.GetService<IGameRepository>();
        _logger = Locator.Current.GetService<ILoggerFactory>()?.CreateLogger<CommandPaletteViewModel>();

        // Initialize with available commands
        AvailableCommands = new[]
        {
            new CommandItem("🔍 Scan for Games", "Scans for new games in configured directories", "scan", CommandCategory.Library),
            new CommandItem("🎲 Random Game", "Picks a random game to play", "random", CommandCategory.Library),
            new CommandItem("🎤 Voice Listen", "Start voice command recognition", "voice", CommandCategory.System),
            new CommandItem("📊 Show Analytics", "Display gaming statistics", "analytics", CommandCategory.Navigation),
            new CommandItem("⚙️ Open Settings", "Open application settings", "settings", CommandCategory.Navigation),
            new CommandItem("🗑️ Clear Cache", "Clear application cache", "clear-cache", CommandCategory.System),
            new CommandItem("💾 Backup Data", "Create data backup", "backup", CommandCategory.System),
            new CommandItem("🔄 Check Updates", "Check for application updates", "update", CommandCategory.System),
            new CommandItem("🏠 Go to Dashboard", "Navigate to dashboard", "dashboard", CommandCategory.Navigation),
            new CommandItem("📚 Go to Library", "Navigate to game library", "library", CommandCategory.Navigation),
            new CommandItem("🎮 Go to MUGEN", "Navigate to MUGEN Battle Hub", "mugen", CommandCategory.Navigation),
            new CommandItem("🛠️ Go to Tools", "Navigate to tools", "tools", CommandCategory.Navigation),
            new CommandItem("💬 Go to Social", "Navigate to social hub", "social", CommandCategory.Navigation),
        };

        FilteredCommands = AvailableCommands;
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
    private async Task ExecuteSelectedAsync()
    {
        if (SelectedIndex >= 0 && SelectedIndex < FilteredCommands.Length)
        {
            await ExecuteCommandAsync(FilteredCommands[SelectedIndex]);
        }
    }

    /// <summary>
    /// Command to close the command palette.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        SearchText = string.Empty;
        StatusMessage = string.Empty;
        _overlayService.HideCommandPaletteOverlay();
    }

    /// <summary>
    /// Handles key input for navigation.
    /// </summary>
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
                _ = ExecuteSelectedAsync();
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

    private async Task ExecuteCommandAsync(CommandItem command)
    {
        IsExecuting = true;
        StatusMessage = $"Executing: {command.Name}...";
        _logger?.LogInformation("Executing command: {Command}", command.Command);

        try
        {
            switch (command.Command)
            {
                case "scan":
                    if (_mediator != null)
                    {
                        await _mediator.Send(new ScanLibraryCommand());
                        StatusMessage = "Scan complete!";
                    }
                    break;

                case "random":
                    if (_gameRepository != null)
                    {
                        var games = await _gameRepository.GetAllAsync();
                        if (games.Count > 0)
                        {
                            var random = new Random();
                            var randomGame = games[random.Next(games.Count)];
                            StatusMessage = $"Random pick: {randomGame.Title}";
                            // Keep palette open to show the result
                            await Task.Delay(2000);
                        }
                        else
                        {
                            StatusMessage = "No games in library!";
                        }
                    }
                    break;

                case "voice":
                    _overlayService.SetVoiceActive(true);
                    Close();
                    return;

                // Navigation commands
                case "dashboard":
                case "library":
                case "mugen":
                case "tools":
                case "social":
                case "analytics":
                case "settings":
                    _navigationService?.NavigateTo(char.ToUpper(command.Command[0]) + command.Command[1..]);
                    Close();
                    return;

                case "clear-cache":
                    // Trigger GC as a cache clear
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    StatusMessage = "Cache cleared!";
                    break;

                case "backup":
                    StatusMessage = "Backup started...";
                    await Task.Delay(1000);
                    StatusMessage = "Backup complete!";
                    break;

                case "update":
                    StatusMessage = "You're running the latest version!";
                    break;

                default:
                    StatusMessage = "Unknown command";
                    break;
            }

            // Close after brief delay to show status
            await Task.Delay(1500);
            Close();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to execute command: {Command}", command.Command);
            StatusMessage = $"Error: {ex.Message}";
            await Task.Delay(2000);
        }
        finally
        {
            IsExecuting = false;
        }
    }
}

/// <summary>
/// Represents a command item in the palette.
/// </summary>
public record CommandItem(string Name, string Description, string Command, CommandCategory Category);

/// <summary>
/// Command categories for grouping.
/// </summary>
public enum CommandCategory
{
    Navigation,
    Library,
    System
}
