using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.Search.Models;
using SaveState.Core.Search.Services;
using SaveState.Presentation.Services;
using SaveState.Presentation.Utilities;

namespace SaveState.Presentation.ViewModels.Search;

/// <summary>
/// View model for the universal search overlay with semantic and content-aware search.
/// </summary>
public partial class UniversalSearchViewModel : ObservableObject, IDisposable
{
    private readonly IUniversalSearchService _searchService;
    private readonly IOverlayService _overlayService;
    private readonly INavigationService _navigationService;
    private readonly ILogger<UniversalSearchViewModel>? _logger;
    private readonly AsyncSearchThrottleHelper _searchThrottleHelper;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private string _searchError = string.Empty;

    [ObservableProperty]
    private ObservableCollection<UniversalSearchResultViewModel> _searchResults = new();

    [ObservableProperty]
    private UniversalSearchResultViewModel? _selectedResult;

    [ObservableProperty]
    private ObservableCollection<string> _suggestions = new();

    [ObservableProperty]
    private bool _showSuggestions;

    [ObservableProperty]
    private SearchScope _currentScope = SearchScope.All;

    public bool HasResults => SearchResults.Count > 0;
    public bool ShowNoResults => !IsSearching && SearchResults.Count == 0 && !string.IsNullOrWhiteSpace(SearchText);

    public IReadOnlyList<SearchScope> AvailableScopes => new[]
    {
        SearchScope.All,
        SearchScope.Games,
        SearchScope.Saves,
        SearchScope.Actions,
        SearchScope.Settings
    };

    public UniversalSearchViewModel(
        IUniversalSearchService searchService,
        IOverlayService overlayService,
        INavigationService navigationService,
        ILogger<UniversalSearchViewModel>? logger = null)
    {
        _searchService = searchService;
        _overlayService = overlayService;
        _navigationService = navigationService;
        _logger = logger;

        // Initialize throttled search with 150ms delay for responsive feel
        _searchThrottleHelper = new AsyncSearchThrottleHelper(
            async (query, ct) => await PerformSearchAsync(query, ct),
            TimeSpan.FromMilliseconds(150));

        SearchResults.CollectionChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(ShowNoResults));
        };
    }

    partial void OnSearchTextChanged(string value)
    {
        _searchThrottleHelper.UpdateSearchText(value);

        if (value.Length >= 2)
        {
            _ = LoadSuggestionsAsync(value);
        }
        else
        {
            Suggestions.Clear();
            ShowSuggestions = false;
        }
    }

    partial void OnCurrentScopeChanged(SearchScope value)
    {
        // Re-search when scope changes
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            _searchThrottleHelper.UpdateSearchText(SearchText);
        }
    }

    /// <summary>
    /// Performs the search operation.
    /// </summary>
    private async Task PerformSearchAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                SearchResults.Clear();
                SearchError = string.Empty;
                SelectedResult = null;
            });
            return;
        }

        try
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsSearching = true;
                SearchError = string.Empty;
                ShowSuggestions = false;
            });

            var result = await _searchService.SearchInstantAsync(query, CurrentScope, cancellationToken);

            if (result.IsFailure)
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SearchError = result.Error ?? "Search failed";
                });
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                SearchResults.Clear();
                SelectedResult = null;

                foreach (var searchResult in result.Value!)
                {
                    SearchResults.Add(new UniversalSearchResultViewModel(
                        searchResult.Id,
                        searchResult.Title,
                        searchResult.Subtitle,
                        searchResult.Type,
                        searchResult.Icon,
                        searchResult.RelevanceScore,
                        searchResult.Action,
                        searchResult.Shortcut));
                }

                // Select first result by default
                if (SearchResults.Count > 0)
                {
                    SelectedResult = SearchResults[0];
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
    /// Loads search suggestions for autocomplete.
    /// </summary>
    private async Task LoadSuggestionsAsync(string partialQuery)
    {
        try
        {
            var result = await _searchService.GetSuggestionsAsync(partialQuery);

            if (result.IsSuccess)
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Suggestions.Clear();
                    foreach (var suggestion in result.Value!.Take(5))
                    {
                        Suggestions.Add(suggestion);
                    }
                    ShowSuggestions = Suggestions.Count > 0 && string.IsNullOrWhiteSpace(SearchError);
                });
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to load suggestions");
        }
    }

    /// <summary>
    /// Command to execute the selected search result.
    /// </summary>
    [RelayCommand]
    private async Task ExecuteSelectedResult()
    {
        if (SelectedResult != null)
        {
            await ExecuteResultAsync(SelectedResult);
        }
    }

    /// <summary>
    /// Command to select a search result.
    /// </summary>
    [RelayCommand]
    private async Task SelectResult(UniversalSearchResultViewModel result)
    {
        if (result == null)
        {
            return;
        }

        await ExecuteResultAsync(result);
    }

    /// <summary>
    /// Executes the action associated with a search result.
    /// </summary>
    private async Task ExecuteResultAsync(UniversalSearchResultViewModel result)
    {
        try
        {
            _overlayService.HideAllOverlays();

            // Execute the result's action if available
            if (result.Action != null)
            {
                var actionResult = await result.Action();
                if (actionResult.IsFailure)
                {
                    _logger?.LogWarning("Action failed: {Error}", actionResult.Error);
                }
            }

            // Handle navigation based on result type
            switch (result.Type)
            {
                case SearchResultType.Game:
                    var gameId = result.Id.Replace("game:", "");
                    if (Guid.TryParse(gameId, out var parsedGameId))
                    {
                        await _navigationService.NavigateToAsync("Library", GameId.From(parsedGameId));
                    }
                    break;

                case SearchResultType.Setting:
                    await _navigationService.NavigateToAsync("Settings");
                    break;

                case SearchResultType.Action:
                    // Actions are already executed above
                    break;

                case SearchResultType.SaveState:
                    // Navigate to game's save states tab
                    var saveStateParts = result.Id.Replace("savestate:", "").Split(':');
                    if (saveStateParts.Length > 0 && Guid.TryParse(saveStateParts[0], out var saveGameId))
                    {
                        await _navigationService.NavigateToAsync("Library", GameId.From(saveGameId));
                    }
                    break;

                default:
                    _logger?.LogDebug("No navigation handler for result type: {Type}", result.Type);
                    break;
            }

            // Clear search state
            SearchText = string.Empty;
            SearchResults.Clear();
            SelectedResult = null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to execute search result");
        }
    }

    /// <summary>
    /// Command to close the search overlay.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        SearchText = string.Empty;
        SearchError = string.Empty;
        SearchResults.Clear();
        SelectedResult = null;
        Suggestions.Clear();
        ShowSuggestions = false;
        _overlayService.HideAllOverlays();
    }

    /// <summary>
    /// Command to select the next result (for keyboard navigation).
    /// </summary>
    [RelayCommand]
    private void SelectNextResult()
    {
        if (SearchResults.Count == 0) return;

        var currentIndex = SelectedResult != null ? SearchResults.IndexOf(SelectedResult) : -1;
        var nextIndex = currentIndex + 1;

        if (nextIndex >= SearchResults.Count)
        {
            nextIndex = 0; // Wrap around
        }

        SelectedResult = SearchResults[nextIndex];
    }

    /// <summary>
    /// Command to select the previous result (for keyboard navigation).
    /// </summary>
    [RelayCommand]
    private void SelectPreviousResult()
    {
        if (SearchResults.Count == 0) return;

        var currentIndex = SelectedResult != null ? SearchResults.IndexOf(SelectedResult) : 0;
        var prevIndex = currentIndex - 1;

        if (prevIndex < 0)
        {
            prevIndex = SearchResults.Count - 1; // Wrap around
        }

        SelectedResult = SearchResults[prevIndex];
    }

    /// <summary>
    /// Command to apply a suggestion.
    /// </summary>
    [RelayCommand]
    private void ApplySuggestion(string suggestion)
    {
        SearchText = suggestion;
        ShowSuggestions = false;
    }

    /// <summary>
    /// Command to change the search scope.
    /// </summary>
    [RelayCommand]
    private void ChangeScope(SearchScope scope)
    {
        CurrentScope = scope;
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
/// View model for a universal search result item.
/// </summary>
public class UniversalSearchResultViewModel
{
    public string Id { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public SearchResultType Type { get; }
    public string Icon { get; }
    public float RelevanceScore { get; }
    public Func<Task<Result>>? Action { get; }
    public string? Shortcut { get; }

    public string TypeLabel => Type switch
    {
        SearchResultType.Game => "Game",
        SearchResultType.SaveState => "Save",
        SearchResultType.Setting => "Setting",
        SearchResultType.Action => "Action",
        SearchResultType.Command => "Command",
        SearchResultType.Guide => "Guide",
        SearchResultType.Achievement => "Achievement",
        _ => "Other"
    };

    public UniversalSearchResultViewModel(
        string id,
        string title,
        string subtitle,
        SearchResultType type,
        string icon,
        float relevanceScore,
        Func<Task<Result>>? action,
        string? shortcut)
    {
        Id = id;
        Title = title;
        Subtitle = subtitle;
        Type = type;
        Icon = icon;
        RelevanceScore = relevanceScore;
        Action = action;
        Shortcut = shortcut;
    }
}
