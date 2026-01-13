using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common.ValueObjects;
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
    private readonly IUiGameContextService _gameContextService;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private ObservableCollection<SearchResultViewModel> _searchResults = new();

    public bool HasResults => SearchResults.Count > 0;

    public bool ShowNoResults =>
        !IsSearching && SearchResults.Count == 0 && !string.IsNullOrWhiteSpace(SearchText);

    public QuickSearchViewModel(
        IOverlayService overlayService,
        IGameRepository gameRepository,
        INavigationService navigationService,
        IUiGameContextService gameContextService)
    {
        _overlayService = overlayService;
        _gameRepository = gameRepository;
        _navigationService = navigationService;
        _gameContextService = gameContextService;

        SearchResults.CollectionChanged += (_, __) => RefreshResultsState();
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

        RefreshResultsState();
    }

    partial void OnIsSearchingChanged(bool value)
    {
        RefreshResultsState();
    }

    /// <summary>
    /// Refreshes derived UI state for the current search results.
    /// </summary>
    private void RefreshResultsState()
    {
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(ShowNoResults));
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
    private async Task ExecuteSearch()
    {
        if (SearchResults.Count > 0)
        {
            // Navigate to first result
            await SelectResult(SearchResults[0]);
        }
    }

    /// <summary>
    /// Command to select a search result.
    /// </summary>
    [RelayCommand]
    private async Task SelectResult(SearchResultViewModel result)
    {
        if (result == null)
        {
            return;
        }

        _overlayService.HideQuickSearchOverlay();
        var gameId = GameId.From(result.GameId);
        _gameContextService.SetSelectedGame(gameId);
        await _navigationService.NavigateTo("Library", gameId);
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
