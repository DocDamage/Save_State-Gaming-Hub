using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Models.Search;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Search;

/// <summary>
/// View model for the Advanced Search feature, providing comprehensive
/// filtering, sorting, and result management capabilities.
/// </summary>
public partial class AdvancedSearchViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<AdvancedSearchViewModel> _logger;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly System.Timers.Timer _searchDebounceTimer;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SearchFilterViewModel> _activeFilters = new();

    [ObservableProperty]
    private ObservableCollection<SearchResult> _results = new();

    [ObservableProperty]
    private ObservableCollection<string> _searchSuggestions = new();

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private int _totalResults;

    [ObservableProperty]
    private SearchResultType? _selectedType;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 25;

    [ObservableProperty]
    private bool _hasMoreResults;

    [ObservableProperty]
    private string _sortField = "Relevance";

    [ObservableProperty]
    private bool _sortDescending = true;

    [ObservableProperty]
    private SearchResult? _selectedResult;

    /// <summary>
    /// Gets whether there are active filters.
    /// </summary>
    public bool HasActiveFilters => ActiveFilters.Count > 0;

    /// <summary>
    /// Gets the available search result types for filtering.
    /// </summary>
    public IReadOnlyList<SearchResultType> AvailableTypes { get; } = new[]
    {
        SearchResultType.Game,
        SearchResultType.SaveState,
        SearchResultType.Achievement,
        SearchResultType.Replay,
        SearchResultType.Collection,
        SearchResultType.Screenshot
    };

    /// <summary>
    /// Gets the available sort fields.
    /// </summary>
    public IReadOnlyList<string> AvailableSortFields { get; } = new[]
    {
        "Relevance",
        "Name",
        "Date",
        "Rating",
        "PlayTime"
    };

    public AdvancedSearchViewModel(
        ILogger<AdvancedSearchViewModel> logger,
        IDialogService dialogService,
        INavigationService navigationService)
    {
        _logger = logger;
        _dialogService = dialogService;
        _navigationService = navigationService;

        // Initialize debounce timer for search (300ms delay)
        _searchDebounceTimer = new System.Timers.Timer(300);
        _searchDebounceTimer.Elapsed += async (s, e) =>
        {
            _searchDebounceTimer.Stop();
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () => await SearchAsync());
        };
        _searchDebounceTimer.AutoReset = false;

        ActiveFilters.CollectionChanged += (_, __) => OnPropertyChanged(nameof(HasActiveFilters));

        // Load initial results
        _ = SearchAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();

        if (value.Length >= 2)
        {
            _ = LoadSuggestionsAsync(value);
        }
        else
        {
            SearchSuggestions.Clear();
        }
    }

    partial void OnSortFieldChanged(string value) => _ = SearchAsync();
    partial void OnSortDescendingChanged(bool value) => _ = SearchAsync();
    partial void OnSelectedTypeChanged(SearchResultType? value) => _ = SearchAsync();

    /// <summary>
    /// Performs the search with current filters and settings.
    /// </summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        try
        {
            IsSearching = true;
            CurrentPage = 1;

            _logger.LogDebug("Searching with query: '{Query}'", SearchText);

            // TODO: Replace with actual search service call
            // var query = BuildSearchQuery();
            // var result = await _searchService.SearchAsync(query);

            // Mock results for demonstration
            await Task.Delay(300); // Simulate network delay

            Results.Clear();
            var mockResults = GenerateMockResults();

            foreach (var result in mockResults)
            {
                Results.Add(result);
            }

            TotalResults = mockResults.Count;
            HasMoreResults = mockResults.Count >= PageSize;

            _logger.LogInformation("Found {Count} results for query: '{Query}'", TotalResults, SearchText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for query: '{Query}'", SearchText);
            await _dialogService.ShowErrorAsync("Search failed. Please try again.");
        }
        finally
        {
            IsSearching = false;
        }
    }

    /// <summary>
    /// Loads more results for pagination.
    /// </summary>
    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (IsSearching || !HasMoreResults) return;

        try
        {
            IsSearching = true;
            CurrentPage++;

            _logger.LogDebug("Loading more results for page {Page}", CurrentPage);

            // TODO: Replace with actual service call
            await Task.Delay(200);

            var moreResults = GenerateMockResults();
            foreach (var result in moreResults)
            {
                Results.Add(result);
            }

            HasMoreResults = moreResults.Count >= PageSize;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load more results");
        }
        finally
        {
            IsSearching = false;
        }
    }

    /// <summary>
    /// Adds a new filter to the active filters.
    /// </summary>
    [RelayCommand]
    private void AddFilter(SearchFilterType type)
    {
        // Check if filter of this type already exists
        if (ActiveFilters.Any(f => f.Type == type))
        {
            return;
        }

        var filter = new SearchFilterViewModel
        {
            Type = type,
            Field = GetDefaultFieldForType(type),
            Operator = "=",
            Value = ""
        };

        ActiveFilters.Add(filter);
        _logger.LogDebug("Added filter: {Type}", type);

        // Trigger search with new filter
        _ = SearchAsync();
    }

    /// <summary>
    /// Removes a filter from the active filters.
    /// </summary>
    [RelayCommand]
    private void RemoveFilter(SearchFilterViewModel filter)
    {
        if (filter != null && ActiveFilters.Contains(filter))
        {
            ActiveFilters.Remove(filter);
            _logger.LogDebug("Removed filter: {Type}", filter.Type);
            _ = SearchAsync();
        }
    }

    /// <summary>
    /// Clears all active filters.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        ActiveFilters.Clear();
        SelectedType = null;
        _logger.LogDebug("Cleared all filters");
        _ = SearchAsync();
    }

    /// <summary>
    /// Opens the selected result.
    /// </summary>
    [RelayCommand]
    private async Task OpenResultAsync(SearchResult result)
    {
        if (result == null) return;

        SelectedResult = result;
        _logger.LogInformation("Opening result: {Title} ({Type})", result.Title, result.Type);

        try
        {
            switch (result.Type)
            {
                case SearchResultType.Game:
                    await _navigationService.NavigateToAsync("GameDetails", result.Id);
                    break;
                case SearchResultType.SaveState:
                    await _navigationService.NavigateToAsync("SaveStates", result.Id);
                    break;
                case SearchResultType.Achievement:
                    await _navigationService.NavigateToAsync("Achievements", result.Id);
                    break;
                case SearchResultType.Replay:
                    await _navigationService.NavigateToAsync("ReplayTheater", result.Id);
                    break;
                case SearchResultType.Collection:
                    await _navigationService.NavigateToAsync("Collections", result.Id);
                    break;
                default:
                    _logger.LogWarning("Unknown result type: {Type}", result.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open result: {Id}", result.Id);
            await _dialogService.ShowErrorAsync("Failed to open the selected item.");
        }
    }

    /// <summary>
    /// Saves the current search configuration.
    /// </summary>
    [RelayCommand]
    private async Task SaveSearchAsync()
    {
        try
        {
            var name = await _dialogService.ShowInputDialogAsync(
                "Save Search",
                "Enter a name for this search:",
                SearchText);

            if (!string.IsNullOrWhiteSpace(name))
            {
                // TODO: Save search configuration
                _logger.LogInformation("Saved search: {Name}", name);
                await _dialogService.ShowSuccessAsync("Search saved successfully!");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save search");
            await _dialogService.ShowErrorAsync("Failed to save search.");
        }
    }

    /// <summary>
    /// Applies a search suggestion.
    /// </summary>
    [RelayCommand]
    private void ApplySuggestion(string suggestion)
    {
        SearchText = suggestion;
        SearchSuggestions.Clear();
    }

    /// <summary>
    /// Clears the current search text.
    /// </summary>
    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        SearchSuggestions.Clear();
    }

    /// <summary>
    /// Updates a filter value.
    /// </summary>
    [RelayCommand]
    private void UpdateFilterValue(SearchFilterViewModel filter)
    {
        _ = SearchAsync();
    }

    /// <summary>
    /// Changes the sort order.
    /// </summary>
    [RelayCommand]
    private void ToggleSortDirection()
    {
        SortDescending = !SortDescending;
    }

    /// <summary>
    /// Loads search suggestions based on partial input.
    /// </summary>
    private async Task LoadSuggestionsAsync(string partialQuery)
    {
        // TODO: Load actual suggestions from service
        await Task.Delay(50);

        var suggestions = new[]
        {
            $"{partialQuery} games",
            $"{partialQuery} saves",
            $"{partialQuery} achievements",
            $"{partialQuery} replays"
        };

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            SearchSuggestions.Clear();
            foreach (var suggestion in suggestions)
            {
                SearchSuggestions.Add(suggestion);
            }
        });
    }

    /// <summary>
    /// Generates mock search results for demonstration.
    /// </summary>
    private List<SearchResult> GenerateMockResults()
    {
        var results = new List<SearchResult>();

        if (SelectedType == null || SelectedType == SearchResultType.Game)
        {
            results.Add(new SearchResult
            {
                Id = "game:1",
                Title = "Elden Ring",
                Description = "Open-world action RPG",
                Type = SearchResultType.Game,
                RelevanceScore = 0.98f,
                Metadata = new Dictionary<string, object>
                {
                    ["Rating"] = "97% positive",
                    ["PlayTime"] = "120h avg",
                    ["Genre"] = "Action RPG"
                }
            });

            results.Add(new SearchResult
            {
                Id = "game:2",
                Title = "Cyberpunk 2077",
                Description = "Open-world action-adventure",
                Type = SearchResultType.Game,
                RelevanceScore = 0.85f,
                Metadata = new Dictionary<string, object>
                {
                    ["Rating"] = "90% positive",
                    ["PlayTime"] = "80h avg",
                    ["Genre"] = "Action RPG"
                }
            });
        }

        if (SelectedType == null || SelectedType == SearchResultType.SaveState)
        {
            results.Add(new SearchResult
            {
                Id = "savestate:1",
                Title = "Elden Ring - Pre-Margit",
                Description = "Created 2 days ago • Level 35 • Stormveil Castle",
                Type = SearchResultType.SaveState,
                RelevanceScore = 0.92f,
                Metadata = new Dictionary<string, object>
                {
                    ["Game"] = "Elden Ring",
                    ["Date"] = "2 days ago",
                    ["Level"] = 35
                }
            });
        }

        if (SelectedType == null || SelectedType == SearchResultType.Achievement)
        {
            results.Add(new SearchResult
            {
                Id = "achievement:1",
                Title = "Elden Lord",
                Description = "Complete the game",
                Type = SearchResultType.Achievement,
                RelevanceScore = 0.78f,
                Metadata = new Dictionary<string, object>
                {
                    ["Game"] = "Elden Ring",
                    ["UnlockedBy"] = "12% of players"
                }
            });
        }

        if (SelectedType == null || SelectedType == SearchResultType.Replay)
        {
            results.Add(new SearchResult
            {
                Id = "replay:1",
                Title = "Elden Ring - Malenia Boss Fight",
                Description = "Duration 15:42 • 89 views",
                Type = SearchResultType.Replay,
                RelevanceScore = 0.88f,
                Metadata = new Dictionary<string, object>
                {
                    ["Game"] = "Elden Ring",
                    ["Duration"] = "15:42",
                    ["Views"] = 89
                }
            });
        }

        return results;
    }

    private static string GetDefaultFieldForType(SearchFilterType type)
    {
        return type switch
        {
            SearchFilterType.Game => "Name",
            SearchFilterType.Platform => "Platform",
            SearchFilterType.Genre => "Genre",
            SearchFilterType.Tag => "Tag",
            SearchFilterType.Date => "Date",
            SearchFilterType.Rating => "Rating",
            SearchFilterType.PlayTime => "PlayTime",
            SearchFilterType.SaveState => "Game",
            SearchFilterType.Achievement => "Name",
            SearchFilterType.Collection => "Name",
            SearchFilterType.Status => "Status",
            _ => "Name"
        };
    }

    public void Dispose()
    {
        _searchDebounceTimer?.Dispose();
    }
}

/// <summary>
/// View model for a search filter in the UI.
/// </summary>
public partial class SearchFilterViewModel : ObservableObject
{
    [ObservableProperty]
    private SearchFilterType _type;

    [ObservableProperty]
    private string _field = string.Empty;

    [ObservableProperty]
    private string _operator = "=";

    [ObservableProperty]
    private object _value = null!;

    /// <summary>
    /// Gets the display name for the filter type.
    /// </summary>
    public string TypeDisplayName => Type.ToString();

    /// <summary>
    /// Gets the available operators for this filter.
    /// </summary>
    public IReadOnlyList<string> AvailableOperators { get; } = new[]
    {
        "=",
        "!=",
        "<",
        ">",
        "contains",
        "startsWith"
    };
}
