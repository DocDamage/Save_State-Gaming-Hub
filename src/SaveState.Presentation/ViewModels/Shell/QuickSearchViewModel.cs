using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Presentation.Services;
using SaveState.Presentation.Utilities;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the quick search overlay with throttled search.
/// </summary>
public partial class QuickSearchViewModel : ObservableObject, IDisposable
{
    private readonly IOverlayService _overlayService;
    private readonly IGameRepository _gameRepository;
    private readonly INavigationService _navigationService;
    private readonly IUiGameContextService _gameContextService;
    private readonly ILogger<QuickSearchViewModel>? _logger;
    private readonly AsyncSearchThrottleHelper _searchThrottleHelper;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private string _searchError = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SearchResultViewModel> _searchResults = new();

    public bool HasResults => SearchResults.Count > 0;

    public bool ShowNoResults =>
        !IsSearching && SearchResults.Count == 0 && !string.IsNullOrWhiteSpace(SearchText);

    public QuickSearchViewModel(
        IOverlayService overlayService,
        IGameRepository gameRepository,
        INavigationService navigationService,
        IUiGameContextService gameContextService,
        ILogger<QuickSearchViewModel>? logger = null)
    {
        _overlayService = overlayService;
        _gameRepository = gameRepository;
        _navigationService = navigationService;
        _gameContextService = gameContextService;
        _logger = logger;

        // Initialize throttled search with 200ms delay for responsive feel
        _searchThrottleHelper = new AsyncSearchThrottleHelper(
            async (query, ct) =>
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        SearchResults.Clear();
                        SearchError = string.Empty;
                        RefreshResultsState();
                    });
                    return;
                }

                await SearchGamesAsync(query, ct);
            },
            TimeSpan.FromMilliseconds(200));

        SearchResults.CollectionChanged += (_, __) => RefreshResultsState();
    }

    /// <summary>
    /// Called when SearchText changes. Uses throttling to prevent excessive search operations.
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        _searchThrottleHelper.UpdateSearchText(value);
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
    private async Task SearchGamesAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsSearching = true;
                SearchError = string.Empty;
                SearchResults.Clear();
            });

            var results = await _gameRepository.GetGameSummariesAsync(
                pageNumber: 1,
                pageSize: 10,
                searchTerm: query,
                sortBy: GameSortBy.Title);

            cancellationToken.ThrowIfCancellationRequested();

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var game in results.Items)
                {
                    SearchResults.Add(new SearchResultViewModel(
                        game.Id,
                        game.Title,
                        game.PlatformName ?? "Unknown",
                        game.CoverImageUrl));
                }
            });
        }
        catch (OperationCanceledException)
        {
            // Expected when search is cancelled due to new input
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Search failed for query: {Query}", query);
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                SearchError = "Search failed. Please try again.";
            });
        }
        finally
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsSearching = false;
            });
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
        await _navigationService.NavigateToAsync("Library", gameId);
    }

    /// <summary>
    /// Command to close the quick search.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        SearchText = string.Empty;
        SearchError = string.Empty;
        SearchResults.Clear();
        _overlayService.HideQuickSearchOverlay();
    }

    /// <summary>
    /// Disposes resources used by this view model.
    /// </summary>
    public void Dispose()
    {
        _searchThrottleHelper.Dispose();
    }
}

/// <summary>
/// View model for a search result item.
/// </summary>
public record SearchResultViewModel(Guid GameId, string Title, string Platform, string? CoverArtPath);
