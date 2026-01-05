using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the quick search overlay.
/// </summary>
public partial class QuickSearchViewModel : ObservableObject
{
    private readonly IOverlayService _overlayService;
    private readonly IGameRepository _gameRepository;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private ObservableCollection<SearchResultViewModel> _searchResults = new();

    public QuickSearchViewModel(
        IOverlayService overlayService,
        IGameRepository gameRepository,
        INavigationService navigationService)
    {
        _overlayService = overlayService;
        _gameRepository = gameRepository;
        _navigationService = navigationService;
    }

    partial void OnSearchTextChanged(string value)
    {
        // Trigger search when text changes (with debounce in real UI)
        if (value.Length >= 2)
        {
            _ = SearchGamesAsync(value);
        }
        else
        {
            SearchResults.Clear();
        }
    }

    /// <summary>
    /// Searches for games matching the query.
    /// </summary>
    private async Task SearchGamesAsync(string query)
    {
        try
        {
            IsSearching = true;
            SearchResults.Clear();

            var results = await _gameRepository.GetGameSummariesAsync(
                pageNumber: 1,
                pageSize: 10,
                searchTerm: query,
                sortBy: GameSortBy.Title);

            foreach (var game in results.Items)
            {
                SearchResults.Add(new SearchResultViewModel(
                    game.Id,
                    game.Title,
                    game.PlatformName ?? "Unknown",
                    game.CoverImageUrl));
            }
        }
        catch
        {
            // Silently handle search errors
        }
        finally
        {
            IsSearching = false;
        }
    }

    /// <summary>
    /// Command to execute the search and navigate to first result.
    /// </summary>
    [RelayCommand]
    private void ExecuteSearch()
    {
        if (SearchResults.Count > 0)
        {
            // Navigate to first result
            SelectResult(SearchResults[0]);
        }
    }

    /// <summary>
    /// Command to select a search result.
    /// </summary>
    [RelayCommand]
    private async Task SelectResult(SearchResultViewModel result)
    {
        _overlayService.HideQuickSearchOverlay();
        // Navigate to game detail
        await _navigationService.NavigateTo("GameDetail");
    }

    /// <summary>
    /// Command to close the quick search.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        SearchText = string.Empty;
        SearchResults.Clear();
        _overlayService.HideQuickSearchOverlay();
    }
}

/// <summary>
/// View model for a search result item.
/// </summary>
public record SearchResultViewModel(Guid GameId, string Title, string Platform, string? CoverArtPath);
