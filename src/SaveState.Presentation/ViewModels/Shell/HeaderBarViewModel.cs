using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the header bar containing navigation and global controls.
/// </summary>
public partial class HeaderBarViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IOverlayService _overlayService;

    private string _searchText = string.Empty;
    private bool _hasNotifications;

    public HeaderBarViewModel(
        INavigationService navigationService,
        IOverlayService overlayService)
    {
        _navigationService = navigationService;
        _overlayService = overlayService;

        // Initialize tab buttons
        TabButtons = new ObservableCollection<TabButtonViewModel>(
            TabRegistry.GetAllTabs().Select(tab =>
                new TabButtonViewModel(tab, _navigationService)));

        // Subscribe to navigation changes
        _navigationService.Navigated += OnNavigated;

        // Update initial active tab
        UpdateActiveTab(_navigationService.CurrentTab);
    }

    /// <summary>
    /// Gets the collection of tab buttons.
    /// </summary>
    public ObservableCollection<TabButtonViewModel> TabButtons { get; }

    /// <summary>
    /// Gets or sets the search text.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    /// <summary>
    /// Gets whether there are unread notifications.
    /// </summary>
    public bool HasNotifications
    {
        get => _hasNotifications;
        set => SetProperty(ref _hasNotifications, value);
    }

    /// <summary>
    /// Executes the current search or opens appropriate overlay.
    /// </summary>
    public void ExecuteSearch()
    {
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            // Command palette: starts with ">"
            if (SearchText.TrimStart().StartsWith(">"))
            {
                _overlayService.ShowCommandPaletteOverlay();
            }
            // AI Assistant: starts with "@"
            else if (SearchText.TrimStart().StartsWith("@"))
            {
                _overlayService.ShowAiAssistantOverlay();
            }
            // Otherwise: Quick search
            else
            {
                _overlayService.ShowQuickSearchOverlay();
            }
            SearchText = string.Empty;
        }
        else
        {
            // Empty search opens quick search
            _overlayService.ShowQuickSearchOverlay();
        }
    }

    /// <summary>
    /// Clears the search.
    /// </summary>
    public void ClearSearch()
    {
        SearchText = string.Empty;
    }

    /// <summary>
    /// Command to toggle the notifications panel.
    /// </summary>
    [RelayCommand]
    private void ToggleNotifications()
    {
        _overlayService.ToggleNotificationsOverlay();
    }

    /// <summary>
    /// Command to open settings.
    /// </summary>
    [RelayCommand]
    private async Task OpenSettings()
    {
        await _navigationService.NavigateTo("Settings");
    }

    /// <summary>
    /// Command to open user profile.
    /// </summary>
    [RelayCommand]
    private void OpenUserProfile()
    {
        _overlayService.ToggleUserProfileOverlay();
    }

    private void OnNavigated(object? sender, NavigationEventArgs e)
    {
        UpdateActiveTab(e.Entry.Tab);
    }

    private void UpdateActiveTab(string activeTabName)
    {
        foreach (var tabButton in TabButtons)
        {
            tabButton.IsActive = tabButton.Name == activeTabName;
        }
    }
}

/// <summary>
/// View model for individual tab buttons.
/// </summary>
public partial class TabButtonViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private bool _isActive;

    public TabButtonViewModel(TabDefinition tabDefinition, INavigationService navigationService)
    {
        _navigationService = navigationService;

        Name = tabDefinition.Name;
        Label = tabDefinition.Name;
        Icon = tabDefinition.Icon;
        TooltipText = tabDefinition.TooltipText;
    }

    /// <summary>
    /// Gets the tab name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the display label.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets the icon.
    /// </summary>
    public string Icon { get; }

    /// <summary>
    /// Gets the tooltip text.
    /// </summary>
    public string TooltipText { get; }

    /// <summary>
    /// Gets or sets whether this tab is active.
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    /// <summary>
    /// Command to navigate to this tab.
    /// </summary>
    [RelayCommand]
    private async Task Navigate()
    {
        await _navigationService.NavigateTo(Name);
    }
}
