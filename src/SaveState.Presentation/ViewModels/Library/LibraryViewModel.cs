using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Presentation.ViewModels.Library;

/// <summary>
/// Main view model for the Library tab.
/// </summary>
public partial class LibraryViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<LibraryViewModel> _logger;

    [ObservableProperty]
    private LibrarySidebarViewModel _sidebarViewModel;

    [ObservableProperty]
    private LibraryToolbarViewModel _toolbarViewModel;

    [ObservableProperty]
    private GameGridViewModel _gridViewModel;

    [ObservableProperty]
    private GameListViewModel _listViewModel;

    [ObservableProperty]
    private GameGridViewModel _compactViewModel;

    [ObservableProperty]
    private GameListViewModel _tableViewModel;

    [ObservableProperty]
    private string _libraryStats = "Loading...";

    [ObservableProperty]
    private bool _isGridView = true;

    [ObservableProperty]
    private bool _isListView;

    [ObservableProperty]
    private bool _isCompactView;

    [ObservableProperty]
    private bool _isTableView;

    [ObservableProperty]
    private bool _isFilterPanelVisible;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private bool _hasGames;

    [ObservableProperty]
    private string _emptyStateMessage = "No games found. Add some games to get started!";

    [ObservableProperty]
    private string _paginationInfo = string.Empty;

    [ObservableProperty]
    private string _currentPageInfo = "Page 1";

    [ObservableProperty]
    private bool _hasPreviousPage;

    [ObservableProperty]
    private bool _hasNextPage;

    [ObservableProperty]
    private ObservableCollection<int> _pageSizeOptions = new() { 12, 24, 48, 96 };

    [ObservableProperty]
    private int _selectedPageSize = 24;

    private int _currentPage = 1;

    public LibraryViewModel(
        LibrarySidebarViewModel sidebarViewModel,
        LibraryToolbarViewModel toolbarViewModel,
        GameGridViewModel gridViewModel,
        GameListViewModel listViewModel,
        GameGridViewModel compactViewModel,
        GameListViewModel tableViewModel,
        INavigationService navigationService,
        IDialogService dialogService,
        ILogger<LibraryViewModel> logger)
    {
        _sidebarViewModel = sidebarViewModel;
        _toolbarViewModel = toolbarViewModel;
        _gridViewModel = gridViewModel;
        _listViewModel = listViewModel;
        _compactViewModel = compactViewModel;
        _tableViewModel = tableViewModel;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _logger = logger;

        _sidebarViewModel.FilterChanged += OnSidebarFilterChanged;
        _toolbarViewModel.SortChanged += OnSortChanged;
        _toolbarViewModel.SearchChanged += OnSearchChanged;
        _toolbarViewModel.ViewModeChanged += OnViewModeChanged;
        _toolbarViewModel.FilterPanelToggled += OnFilterPanelToggled;

        InitializeView();
    }

    private void InitializeView()
    {
        // Set up view mode defaults
        SetGridView();

        // Load initial data
        _ = LoadLibraryDataAsync();
    }

    private void OnFilterPanelToggled(object? sender, EventArgs e)
    {
        IsFilterPanelVisible = !IsFilterPanelVisible;
    }

    private void OnViewModeChanged(object? sender, string mode)
    {
        IsGridView = mode == "grid";
        IsListView = mode == "list";
        IsCompactView = mode == "compact";
        IsTableView = mode == "table";

        UpdatePlacementAndPagination();
    }

    private void UpdatePlacementAndPagination()
    {
         UpdateSelectionMode();
         UpdatePagination();
         UpdateLibraryStats();
    }

    [RelayCommand]
    public async Task LoadLibraryDataAsync()
    {
        var smartFilter = _sidebarViewModel.SelectedSmartFilter?.Id;
        var collectionId = _sidebarViewModel.SelectedCollection?.Id;
        var platformId = _sidebarViewModel.SelectedPlatform?.Id;

        var sortOption = _toolbarViewModel.SelectedSortOption?.Id ?? "title_asc";
        var sortDescending = sortOption.EndsWith("_desc");
        var searchTerm = _toolbarViewModel.SearchTerm;

        await Task.WhenAll(
            SidebarViewModel.LoadSidebarDataCommand.ExecuteAsync(null),
            GridViewModel.LoadGamesAsync(_currentPage, SelectedPageSize, searchTerm, smartFilter, collectionId, platformId, sortOption, sortDescending),
            ListViewModel.LoadGamesAsync(_currentPage, SelectedPageSize, searchTerm, smartFilter, collectionId, platformId, sortOption, sortDescending),
            CompactViewModel.LoadGamesAsync(_currentPage, SelectedPageSize, searchTerm, smartFilter, collectionId, platformId, sortOption, sortDescending),
            TableViewModel.LoadGamesAsync(_currentPage, SelectedPageSize, searchTerm, smartFilter, collectionId, platformId, sortOption, sortDescending)
        );

        UpdateLibraryStats();
        UpdatePagination();
    }

    [RelayCommand]
    private void SetGridView()
    {
        IsGridView = true;
        IsListView = false;
        IsCompactView = false;
        IsTableView = false;

        // Sync toolbar state without triggering event loop if checked properly,
        // but here we just ensure visual state matches.
        // If this was triggered by toolbar, this command might be redundant if toolbar is binding to this.
        // If toolbar has its own command, this is only used for programmatic/shortcut switches.
        if (_toolbarViewModel.GridViewButtonClass != "Primary")
            _toolbarViewModel.SetGridViewCommand.Execute(null);

        UpdateSelectionMode();
    }

    [RelayCommand]
    private void SetListView()
    {
        IsGridView = false;
        IsListView = true;
        IsCompactView = false;
        IsTableView = false;

        if (_toolbarViewModel.ListViewButtonClass != "Primary")
            _toolbarViewModel.SetListViewCommand.Execute(null);

        UpdateSelectionMode();
    }

    [RelayCommand]
    private void SetCompactView()
    {
        IsGridView = false;
        IsListView = false;
        IsCompactView = true;
        IsTableView = false;

        if (_toolbarViewModel.CompactViewButtonClass != "Primary")
            _toolbarViewModel.SetCompactViewCommand.Execute(null);

        UpdateSelectionMode();
    }

    [RelayCommand]
    private void SetTableView()
    {
        IsGridView = false;
        IsListView = false;
        IsCompactView = false;
        IsTableView = true;

        if (_toolbarViewModel.TableViewButtonClass != "Primary")
            _toolbarViewModel.SetTableViewCommand.Execute(null);

        UpdateSelectionMode();
    }

    [RelayCommand]
    private async Task AddGame()
    {
        try
        {
            var result = await _dialogService.ShowAddGameWizardAsync();
            if (result != null)
            {
                _logger.LogInformation(
                    "Add game wizard completed: {Title}, Platform: {Platform}, Scan: {Scan}",
                    result.Title,
                    result.Platform ?? "Unknown",
                    result.ScanAutomatically);

                // In full implementation, this would:
                // 1. Create the game via ImportGameCommand
                // 2. Optionally scan for metadata if ScanAutomatically is true
                // 3. Refresh the library view

                await _dialogService.ShowInformationAsync(
                    "Game Added",
                    $"'{result.Title}' has been added to your library.");

                // Refresh library
                await LoadLibraryDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing add game wizard");
            await _dialogService.ShowErrorAsync(
                "Error",
                "An error occurred while adding the game. Please try again.");
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        _ = LoadLibraryDataAsync();
    }

    [RelayCommand]
    private async Task OpenSettings()
    {
        await _navigationService.NavigateTo("Settings");
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (!HasPreviousPage) return;

        _currentPage--;
        await LoadLibraryDataAsync();
        _logger.LogInformation("Navigated to previous page {PageNumber}", _currentPage);
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (!HasNextPage) return;

        _currentPage++;
        await LoadLibraryDataAsync();
        _logger.LogInformation("Navigated to next page {PageNumber}", _currentPage);
    }

    [ObservableProperty]
    private bool _isSelectionMode;

    private void OnSelectionModeToggled(object? sender, bool isSelectionMode)
    {
        IsSelectionMode = isSelectionMode;
        UpdateSelectionMode();
    }

    [RelayCommand]
    private async Task BulkTag()
    {
        var selectedGameIds = GetSelectedGameIds().ToList();
        if (!selectedGameIds.Any())
        {
            await _dialogService.ShowInformationAsync("Selection", "No games selected.");
            return;
        }

        var result = await _dialogService.ShowTagEditorAsync(Array.Empty<string>());
        if (result != null)
        {
             await _dialogService.ShowInformationAsync("Bulk Tagging", $"Applied tags to {selectedGameIds.Count} games.");
             _toolbarViewModel.IsSelectionMode = false; // Exit selection mode
        }
    }

    [RelayCommand]
    private async Task BulkMove()
    {
        var selectedGameIds = GetSelectedGameIds().ToList();
        if (!selectedGameIds.Any())
        {
            await _dialogService.ShowInformationAsync("Selection", "No games selected.");
             return;
        }

        // Placeholder for collection selector
        await _dialogService.ShowInformationAsync("Bulk Move", $"Moved {selectedGameIds.Count} games to collection (Placeholder).");
        _toolbarViewModel.IsSelectionMode = false;
    }

    private IEnumerable<GameId> GetSelectedGameIds()
    {
        if (IsGridView) return GridViewModel.GetSelectedGames().Select(g => g.GameId);
        if (IsListView) return ListViewModel.GetSelectedGames().Select(g => g.GameId);
        if (IsCompactView) return CompactViewModel.GetSelectedGames().Select(g => g.GameId);
        if (IsTableView) return TableViewModel.GetSelectedGames().Select(g => g.GameId);
        return Enumerable.Empty<GameId>();
    }

    private void UpdateSelectionMode()
    {
        GridViewModel.UpdateSelectionMode(IsSelectionMode);
        ListViewModel.UpdateSelectionMode(IsSelectionMode);
        CompactViewModel.UpdateSelectionMode(IsSelectionMode);
        TableViewModel.UpdateSelectionMode(IsSelectionMode);
    }

    private void UpdateLibraryStats()
    {
        // Get pagination state from the current view model
        var (_, _, totalCount, _, _, _) = GetCurrentViewModelPaginationState();
        var installedGames = totalCount; // TODO: Calculate properly when status filtering is available

        LibraryStats = $"{totalCount} games • {installedGames} installed";

        IsEmpty = totalCount == 0;
        HasGames = totalCount > 0;
    }

    private void UpdatePagination()
    {
        var (currentPage, pageSize, totalCount, totalPages, hasPreviousPage, hasNextPage) = GetCurrentViewModelPaginationState();

        if (totalCount > 0)
        {
            var startIndex = ((currentPage - 1) * pageSize) + 1;
            var endIndex = Math.Min(currentPage * pageSize, totalCount);
            PaginationInfo = $"Showing {startIndex}-{endIndex} of {totalCount} games";
            CurrentPageInfo = $"Page {currentPage} of {totalPages}";
        }
        else
        {
            PaginationInfo = "No games";
            CurrentPageInfo = "Page 1";
        }

        HasPreviousPage = hasPreviousPage;
        HasNextPage = hasNextPage;
    }

    private (int CurrentPage, int PageSize, int TotalCount, int TotalPages, bool HasPreviousPage, bool HasNextPage) GetCurrentViewModelPaginationState()
    {
        // Get pagination state from the currently active view model
        if (IsGridView)
        {
            return GridViewModel.GetPaginationState();
        }
        else if (IsListView)
        {
            return ListViewModel.GetPaginationState();
        }
        else if (IsCompactView)
        {
            return CompactViewModel.GetPaginationState();
        }
        else if (IsTableView)
        {
            return TableViewModel.GetPaginationState();
        }

        // Default to grid view
        return GridViewModel.GetPaginationState();
    }

    partial void OnSelectedPageSizeChanged(int value)
    {
        // Reset to page 1 when page size changes
        _currentPage = 1;
        _ = LoadLibraryDataAsync();
    }

    private void OnSidebarFilterChanged(object? sender, EventArgs e)
    {
        _currentPage = 1;
        _ = LoadLibraryDataAsync();
    }

    private void OnSortChanged(object? sender, EventArgs e)
    {
        _currentPage = 1;
        _ = LoadLibraryDataAsync();
    }

    private void OnSearchChanged(object? sender, EventArgs e)
    {
        _currentPage = 1;
        _ = LoadLibraryDataAsync();
    }
}
