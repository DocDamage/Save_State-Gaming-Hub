using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Presentation.Services;
using SaveState.Presentation.Services.QuickActions;
using Splat;

namespace SaveState.Presentation.ViewModels.QuickActions;

/// <summary>
/// View model for the floating quick action bar.
/// </summary>
public partial class QuickActionBarViewModel : ObservableObject
{
    private readonly IQuickActionService _quickActionService;
    private readonly IOverlayService _overlayService;
    private readonly INavigationService? _navigationService;
    private readonly IDialogService? _dialogService;
    private readonly ILogger<QuickActionBarViewModel>? _logger;

    [ObservableProperty]
    private Game? _selectedGame;

    [ObservableProperty]
    private bool _isCollapsed;

    [ObservableProperty]
    private double _collapseArrowRotation;

    /// <summary>
    /// Gets whether a game can be launched.
    /// </summary>
    public bool CanLaunchGame => SelectedGame != null;

    /// <summary>
    /// Gets whether quick save is available.
    /// </summary>
    public bool CanQuickSave => SelectedGame != null;

    /// <summary>
    /// Initializes a new instance of the QuickActionBarViewModel class.
    /// </summary>
    public QuickActionBarViewModel()
    {
        _quickActionService = Locator.Current.GetRequiredService<IQuickActionService>();
        _overlayService = Locator.Current.GetRequiredService<IOverlayService>();
        _navigationService = Locator.Current.GetService<INavigationService>();
        _dialogService = Locator.Current.GetService<IDialogService>();
        _logger = Locator.Current.GetService<ILoggerFactory>()?.CreateLogger<QuickActionBarViewModel>();

        // Set up property change handlers
        PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(SelectedGame))
            {
                OnPropertyChanged(nameof(CanLaunchGame));
                OnPropertyChanged(nameof(CanQuickSave));
            }
            else if (e.PropertyName == nameof(IsCollapsed))
            {
                CollapseArrowRotation = IsCollapsed ? 180 : 0;
            }
        };
    }

    /// <summary>
    /// Launches the selected game.
    /// </summary>
    [RelayCommand]
    private async Task LaunchGameAsync()
    {
        if (SelectedGame == null)
        {
            return;
        }

        try
        {
            _logger?.LogInformation("Launching game from quick action bar: {GameTitle}", SelectedGame.Title);

            // Execute via quick action service
            await _quickActionService.ExecuteActionAsync(
                QuickActionIds.GameLaunch,
                QuickActionContext.ForGame(SelectedGame));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to launch game from quick action bar");
        }
    }

    /// <summary>
    /// Performs a quick save.
    /// </summary>
    [RelayCommand]
    private async Task QuickSaveAsync()
    {
        if (SelectedGame == null)
        {
            return;
        }

        try
        {
            _logger?.LogInformation("Quick save from action bar for game: {GameTitle}", SelectedGame.Title);

            await _quickActionService.ExecuteActionAsync(
                QuickActionIds.SaveStateQuickSave,
                QuickActionContext.ForGame(SelectedGame));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to quick save from action bar");
        }
    }

    /// <summary>
    /// Takes a screenshot.
    /// </summary>
    [RelayCommand]
    private async Task TakeScreenshotAsync()
    {
        try
        {
            _logger?.LogInformation("Taking screenshot from quick action bar");

            var context = SelectedGame != null
                ? QuickActionContext.ForGame(SelectedGame)
                : QuickActionContext.Empty;

            await _quickActionService.ExecuteActionAsync(QuickActionIds.ScreenshotTake, context);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to take screenshot from action bar");
        }
    }

    /// <summary>
    /// Toggles recording.
    /// </summary>
    [RelayCommand]
    private async Task ToggleRecordingAsync()
    {
        try
        {
            _logger?.LogInformation("Toggle recording from quick action bar");

            var context = SelectedGame != null
                ? QuickActionContext.ForGame(SelectedGame)
                : QuickActionContext.Empty;

            await _quickActionService.ExecuteActionAsync(QuickActionIds.RecordingStart, context);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to toggle recording from action bar");
        }
    }

    /// <summary>
    /// Opens the tools menu.
    /// </summary>
    [RelayCommand]
    private async Task OpenToolsAsync()
    {
        try
        {
            _logger?.LogInformation("Opening tools from quick action bar");

            // Navigate to tools view
            _navigationService?.NavigateToAsync("Tools");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open tools from action bar");
        }
    }

    /// <summary>
    /// Opens the help window.
    /// </summary>
    [RelayCommand]
    private async Task OpenHelpAsync()
    {
        try
        {
            _logger?.LogInformation("Opening help from quick action bar");

            // Show keyboard shortcuts help
            await ShowKeyboardShortcutsHelpAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open help from action bar");
        }
    }

    /// <summary>
    /// Toggles the collapsed state of the bar.
    /// </summary>
    [RelayCommand]
    private void ToggleCollapsed()
    {
        IsCollapsed = !IsCollapsed;
        _logger?.LogDebug("Quick action bar collapsed: {IsCollapsed}", IsCollapsed);
    }

    private async Task ShowKeyboardShortcutsHelpAsync()
    {
        var helpWindow = new KeyboardShortcutsHelp
        {
            DataContext = new KeyboardShortcutsHelpViewModel()
        };

        // Get the main window as owner
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            helpWindow.ShowDialog(desktop.MainWindow!);
        }
        else
        {
            helpWindow.Show();
        }

        await Task.CompletedTask;
    }
}
