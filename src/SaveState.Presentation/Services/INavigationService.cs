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
    Task NavigateTo<TViewModel>() where TViewModel : ObservableObject;

    /// <summary>
    /// Navigates to a specific tab by name.
    /// </summary>
    /// <param name="tabName">The name of the tab to navigate to.</param>
    Task NavigateTo(string tabName);

    /// <summary>
    /// Navigates to a specific tab with a parameter.
    /// </summary>
    /// <param name="tabName">The name of the tab to navigate to.</param>
    /// <param name="parameter">The navigation parameter.</param>
    Task NavigateTo(string tabName, object parameter);

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