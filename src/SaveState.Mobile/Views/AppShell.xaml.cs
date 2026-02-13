using Microsoft.Maui.Controls;
using SaveState.Mobile.ViewModels;

namespace SaveState.Mobile.Views;

/// <summary>
/// Mobile app shell for .NET MAUI application.
/// PHASE 7: REQUIRED - Mobile Companion App (Session 4)
/// </summary>
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        Routing.RegisterRoute(nameof(LibraryPage), typeof(LibraryPage));
        Routing.RegisterRoute(nameof(GameDetailPage), typeof(GameDetailPage));
        Routing.RegisterRoute(nameof(SaveStatesPage), typeof(SaveStatesPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(CloudSyncPage), typeof(CloudSyncPage));
    }
}

/// <summary>
/// Mobile app main page.
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Fire and forget with explicit exception handling
        _ = InitializeViewModelAsync();
    }

    private async Task InitializeViewModelAsync()
    {
        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing main page: {ex}");
        }
    }
}

/// <summary>
/// Game library page for mobile.
/// </summary>
public partial class LibraryPage : ContentPage
{
    private readonly MobileLibraryViewModel _viewModel;

    public LibraryPage()
    {
        InitializeComponent();
        _viewModel = new MobileLibraryViewModel();
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Fire and forget with explicit exception handling
        _ = LoadGamesAsync();
    }

    private async Task LoadGamesAsync()
    {
        try
        {
            await _viewModel.LoadGamesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading games: {ex}");
        }
    }
}

/// <summary>
/// Game detail page for mobile.
/// </summary>
public partial class GameDetailPage : ContentPage
{
    private readonly GameDetailViewModel _viewModel;

    public GameDetailPage()
    {
        InitializeComponent();
        _viewModel = new GameDetailViewModel();
        BindingContext = _viewModel;
    }
}

/// <summary>
/// Save states page for mobile.
/// </summary>
public partial class SaveStatesPage : ContentPage
{
    private readonly SaveStatesViewModel _viewModel;

    public SaveStatesPage()
    {
        InitializeComponent();
        _viewModel = new SaveStatesViewModel();
        BindingContext = _viewModel;
    }
}

/// <summary>
/// Settings page for mobile.
/// </summary>
public partial class SettingsPage : ContentPage
{
    private readonly MobileSettingsViewModel _viewModel;

    public SettingsPage()
    {
        InitializeComponent();
        _viewModel = new MobileSettingsViewModel();
        BindingContext = _viewModel;
    }
}

/// <summary>
/// Cloud sync page for mobile.
/// </summary>
public partial class CloudSyncPage : ContentPage
{
    private readonly CloudSyncViewModel _viewModel;

    public CloudSyncPage()
    {
        InitializeComponent();
        _viewModel = new CloudSyncViewModel();
        BindingContext = _viewModel;
    }
}
