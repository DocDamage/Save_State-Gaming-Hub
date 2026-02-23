using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.Services;

/// <summary>
/// Service for managing navigation between different application views and tabs.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Gets the current active view model.
    /// </summary>
    ObservableObject CurrentViewModel { get; }

    /// <summary>
    /// Gets the name of the currently active tab.
    /// </summary>
    string CurrentTab { get; }

    /// <summary>
    /// Gets the navigation history stack.
    /// </summary>
    ReadOnlyObservableCollection<NavigationEntry> History { get; }

    /// <summary>
    /// Gets whether navigation back is possible.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Navigates to a specific view model type.
    /// </summary>
    /// <typeparam name="TViewModel">The view model type to navigate to.</typeparam>
    Task NavigateToAsync<TViewModel>() where TViewModel : ObservableObject;

    /// <summary>
    /// Navigates to a specific tab by name.
    /// </summary>
    /// <param name="tabName">The name of the tab to navigate to.</param>
    Task NavigateToAsync(string tabName);

    /// <summary>
    /// Navigates to a specific tab with a parameter.
    /// </summary>
    /// <param name="tabName">The name of the tab to navigate to.</param>
    /// <param name="parameter">The navigation parameter.</param>
    Task NavigateToAsync(string tabName, object parameter);

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    void GoBack();

    /// <summary>
    /// Raised when navigation occurs.
    /// </summary>
    event EventHandler<NavigationEventArgs>? Navigated;
}

/// <summary>
/// Represents a navigation entry in the history.
/// </summary>
public record NavigationEntry(
    string Tab,
    Type ViewModelType,
    object? Parameter,
    DateTime Timestamp);

/// <summary>
/// Event arguments for navigation events.
/// </summary>
public class NavigationEventArgs : EventArgs
{
    /// <summary>
    /// Gets the navigation entry.
    /// </summary>
    public NavigationEntry Entry { get; }

    /// <summary>
    /// Gets the direction of navigation.
    /// </summary>
    public NavigationDirection Direction { get; }

    public NavigationEventArgs(NavigationEntry entry, NavigationDirection direction)
    {
        Entry = entry;
        Direction = direction;
    }
}

/// <summary>
/// Navigation direction enumeration.
/// </summary>
public enum NavigationDirection
{
    Forward,
    Backward
}

/// <summary>
/// Extension methods for navigation service to support Phase 1 UI features.
/// </summary>
public static class NavigationServiceExtensions
{
    /// <summary>
    /// Navigates to the RetroArch tab.
    /// </summary>
    public static async Task NavigateToRetroArchAsync(this INavigationService navigationService)
    {
        await navigationService.NavigateToAsync("RetroArch");
    }

    /// <summary>
    /// Shows the launch experience overlay for a game.
    /// </summary>
    public static async Task ShowLaunchExperienceAsync(this INavigationService navigationService, object game)
    {
        await navigationService.NavigateToAsync("LaunchExperience", game);
    }

    /// <summary>
    /// Shows the system health view.
    /// </summary>
    public static async Task ShowSystemHealthAsync(this INavigationService navigationService)
    {
        await navigationService.NavigateToAsync("Settings");
        // The System Health view is accessed within Settings
    }

    /// <summary>
    /// Shows the connected accounts view.
    /// </summary>
    public static async Task ShowConnectedAccountsAsync(this INavigationService navigationService)
    {
        await navigationService.NavigateToAsync("Settings");
        // The Connected Accounts view is accessed within Settings
    }

    /// <summary>
    /// Shows the performance dashboard view.
    /// </summary>
    public static async Task ShowPerformanceDashboardAsync(this INavigationService navigationService)
    {
        await navigationService.NavigateToAsync("PerformanceDashboard");
    }

    /// <summary>
    /// Shows the data management view.
    /// </summary>
    public static async Task ShowDataManagementAsync(this INavigationService navigationService)
    {
        await navigationService.NavigateToAsync("DataManagement");
    }

    // Tournament Management Navigation

    /// <summary>
    /// Navigates to the tournament list view.
    /// </summary>
    public static async Task ShowTournamentListAsync(this INavigationService navigationService)
    {
        await navigationService.NavigateToAsync("Tournaments");
    }

    /// <summary>
    /// Shows the tournament detail view.
    /// </summary>
    public static async Task ShowTournamentDetailAsync(this INavigationService navigationService, Guid tournamentId)
    {
        await navigationService.NavigateToAsync("TournamentDetail", tournamentId);
    }

    /// <summary>
    /// Shows the match detail view.
    /// </summary>
    public static async Task ShowMatchDetailAsync(this INavigationService navigationService, Guid tournamentId, Guid matchId)
    {
        await navigationService.NavigateToAsync("MatchDetail", new { TournamentId = tournamentId, MatchId = matchId });
    }

    /// <summary>
    /// Shows the tournament standings view.
    /// </summary>
    public static async Task ShowTournamentStandingsAsync(this INavigationService navigationService, Guid tournamentId)
    {
        await navigationService.NavigateToAsync("TournamentStandings", tournamentId);
    }

    /// <summary>
    /// Shows the live tournament tracker view.
    /// </summary>
    public static async Task ShowLiveTournamentTrackerAsync(this INavigationService navigationService, Guid? tournamentId = null)
    {
        await navigationService.NavigateToAsync("LiveTournamentTracker", tournamentId);
    }
}