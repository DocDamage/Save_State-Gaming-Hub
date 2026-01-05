using System.Threading.Tasks;

namespace SaveState.Presentation.Services;

/// <summary>
/// Interface for ViewModels that need to be notified of navigation events.
/// </summary>
public interface INavigationAware
{
    /// <summary>
    /// Called when the ViewModel is navigated to.
    /// </summary>
    /// <param name="parameter">Optional navigation parameter.</param>
    Task OnNavigatedTo(object? parameter);

    /// <summary>
    /// Called when the ViewModel is navigated away from.
    /// </summary>
    Task OnNavigatedFrom();
}