using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the quick search overlay.
/// </summary>
public partial class QuickSearchViewModel : ObservableObject
{
    private readonly IOverlayService _overlayService;
    private string _searchText = string.Empty;

    public QuickSearchViewModel(IOverlayService overlayService)
    {
        _overlayService = overlayService;
    }

    /// <summary>
    /// Gets or sets the search text.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    /// <summary>
    /// Command to execute the search.
    /// </summary>
    [RelayCommand]
    private void ExecuteSearch()
    {
        // TODO: Implement search execution
        _overlayService.HideQuickSearchOverlay();
    }

    /// <summary>
    /// Command to close the quick search.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        _overlayService.HideQuickSearchOverlay();
    }
}