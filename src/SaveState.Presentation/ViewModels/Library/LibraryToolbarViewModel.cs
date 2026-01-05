using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Library;

/// <summary>
/// View model for the Library toolbar.
/// </summary>
public partial class LibraryToolbarViewModel : ObservableObject
{
    private readonly ILogger<LibraryToolbarViewModel> _logger;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    partial void OnSearchTermChanged(string value)
    {
        SearchChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? SearchChanged;

    [ObservableProperty]
    private ObservableCollection<SortOptionViewModel> _sortOptions = new();

    [ObservableProperty]
    private SortOptionViewModel? _selectedSortOption;

    [ObservableProperty]
    private string _gridViewButtonClass = "Secondary";

    [ObservableProperty]
    private string _listViewButtonClass = "Secondary";

    [ObservableProperty]
    private string _compactViewButtonClass = "Secondary";

    [ObservableProperty]
    private string _tableViewButtonClass = "Secondary";

    public LibraryToolbarViewModel(
        ILogger<LibraryToolbarViewModel> logger,
        IDialogService dialogService)
    {
        _logger = logger;
        _dialogService = dialogService;
        InitializeSortOptions();
        SetGridViewCommand.Execute(null); // Default to grid view
    }

    private void InitializeSortOptions()
    {
        SortOptions.Add(new SortOptionViewModel("title_asc", "Title (A-Z)", () => SortByTitle(true)));
        SortOptions.Add(new SortOptionViewModel("title_desc", "Title (Z-A)", () => SortByTitle(false)));
        SortOptions.Add(new SortOptionViewModel("playtime_desc", "Most Played", () => SortByPlaytime(false)));
        SortOptions.Add(new SortOptionViewModel("playtime_asc", "Least Played", () => SortByPlaytime(true)));
        SortOptions.Add(new SortOptionViewModel("last_played_desc", "Recently Played", () => SortByLastPlayed(false)));
        SortOptions.Add(new SortOptionViewModel("last_played_asc", "Least Recently Played", () => SortByLastPlayed(true)));
        SortOptions.Add(new SortOptionViewModel("added_desc", "Recently Added", () => SortByDateAdded(false)));
        SortOptions.Add(new SortOptionViewModel("added_asc", "Oldest Added", () => SortByDateAdded(true)));
        SortOptions.Add(new SortOptionViewModel("release_desc", "Newest Releases", () => SortByReleaseDate(false)));
        SortOptions.Add(new SortOptionViewModel("release_asc", "Oldest Releases", () => SortByReleaseDate(true)));
        SortOptions.Add(new SortOptionViewModel("rating_desc", "Highest Rated", () => SortByRating(false)));
        SortOptions.Add(new SortOptionViewModel("rating_asc", "Lowest Rated", () => SortByRating(true)));

        SelectedSortOption = SortOptions.First(); // Default to title ascending
    }

    public event EventHandler<string>? ViewModeChanged;

    [RelayCommand]
    private void SetGridView()
    {
        UpdateViewModeButtons("grid");
        ViewModeChanged?.Invoke(this, "grid");
    }

    [RelayCommand]
    private void SetListView()
    {
        UpdateViewModeButtons("list");
        ViewModeChanged?.Invoke(this, "list");
    }

    [RelayCommand]
    private void SetCompactView()
    {
        UpdateViewModeButtons("compact");
        ViewModeChanged?.Invoke(this, "compact");
    }

    [RelayCommand]
    private void SetTableView()
    {
        UpdateViewModeButtons("table");
        ViewModeChanged?.Invoke(this, "table");
    }

    private void UpdateViewModeButtons(string activeView)
    {
        GridViewButtonClass = activeView == "grid" ? "Primary" : "Secondary";
        ListViewButtonClass = activeView == "list" ? "Primary" : "Secondary";
        CompactViewButtonClass = activeView == "compact" ? "Primary" : "Secondary";
        TableViewButtonClass = activeView == "table" ? "Primary" : "Secondary";
    }

    [RelayCommand]
    private void ToggleFilterPanel()
    {
        FilterPanelToggled?.Invoke(this, EventArgs.Empty);
        _logger.LogInformation("Filter panel toggle requested");
    }

    public event EventHandler? FilterPanelToggled;

    [RelayCommand]
    private async Task AddGame()
    {
        _logger.LogInformation("Add game wizard requested");
        var result = await _dialogService.ShowAddGameWizardAsync();

        if (result != null)
        {
            _logger.LogInformation("Game added via wizard: {Title}", result.Title);
            // Future: Execute command to actually add the game to repository
        }
    }

    public event EventHandler? SortChanged;

    private void SortByTitle(bool ascending)
    {
        SelectedSortOption = SortOptions.FirstOrDefault(o => o.Id == $"title_{(ascending ? "asc" : "desc")}");
        SortChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SortByPlaytime(bool ascending)
    {
        SelectedSortOption = SortOptions.FirstOrDefault(o => o.Id == $"playtime_{(ascending ? "asc" : "desc")}");
        SortChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SortByLastPlayed(bool ascending)
    {
        SelectedSortOption = SortOptions.FirstOrDefault(o => o.Id == $"last_played_{(ascending ? "asc" : "desc")}");
        SortChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SortByDateAdded(bool ascending)
    {
        SelectedSortOption = SortOptions.FirstOrDefault(o => o.Id == $"added_{(ascending ? "asc" : "desc")}");
        SortChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SortByReleaseDate(bool ascending)
    {
        SelectedSortOption = SortOptions.FirstOrDefault(o => o.Id == $"release_{(ascending ? "asc" : "desc")}");
        SortChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SortByRating(bool ascending)
    {
        SelectedSortOption = SortOptions.FirstOrDefault(o => o.Id == $"rating_{(ascending ? "asc" : "desc")}");
        SortChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// View model for sort options.
/// </summary>
public class SortOptionViewModel : ObservableObject
{
    public SortOptionViewModel(string id, string displayName, Action sortAction)
    {
        Id = id;
        DisplayName = displayName;
        SortCommand = new RelayCommand(sortAction);
    }

    public string Id { get; }
    public string DisplayName { get; }
    public IRelayCommand SortCommand { get; }
}
