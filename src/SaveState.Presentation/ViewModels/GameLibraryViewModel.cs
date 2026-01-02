namespace SaveState.Presentation.ViewModels;

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Application.GameLibrary.ReadModels;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;

public partial class GameLibraryViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly SaveState.Presentation.Resources.Resources _resources;

    public GameLibraryViewModel(IMediator mediator, SaveState.Presentation.Resources.Resources resources)
    {
        _mediator = mediator;
        _resources = resources;

        // Initialize collections
        Games = new ObservableCollection<GameSummaryViewModel>();
        Collections = new ObservableCollection<CollectionViewModel>();
        QuickFilters = new ObservableCollection<QuickFilterViewModel>();
        SortOptions = new ObservableCollection<string>
        {
            "Title",
            "Platform",
            "Last Played",
            "Playtime",
            "Recently Added"
        };
        PlatformFilters = new ObservableCollection<string>
        {
            "All Platforms",
            "Steam",
            "GOG",
            "Epic Games",
            "Origin",
            "Uplay",
            "Battle.net",
            "Other"
        };

        // Initialize commands
        LoadGamesCommand = new AsyncRelayCommand(LoadGamesAsync);
        ScanForGamesCommand = new AsyncRelayCommand(ScanForGamesAsync);
        PickRandomGameCommand = new AsyncRelayCommand(PickRandomGameAsync);
        SetGridViewCommand = new RelayCommand(() => ViewMode = ViewMode.Grid);
        SetListViewCommand = new RelayCommand(() => ViewMode = ViewMode.List);
        SelectCollectionCommand = new RelayCommand<CollectionViewModel>(SelectCollection);
        ToggleGameSelectionCommand = new RelayCommand<GameSummaryViewModel>(ToggleGameSelection);
        AddSelectedToCollectionCommand = new AsyncRelayCommand(AddSelectedToCollectionAsync);
        DeleteSelectedCommand = new AsyncRelayCommand(DeleteSelectedAsync);
        ExportSelectedCommand = new AsyncRelayCommand(ExportSelectedAsync);
        OpenGameDetailCommand = new RelayCommand<GameSummaryViewModel>(OpenGameDetail);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => HasNextPage);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => HasPreviousPage);
        LoadMoreCommand = new AsyncRelayCommand(LoadMoreAsync, () => HasNextPage);

        // Initialize data
        InitializeCollections();
        InitializeQuickFilters();

        // Auto-load games when ViewModel is created
        _ = LoadGamesAsync();
    }

    // View State
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGridView))]
    [NotifyPropertyChangedFor(nameof(IsListView))]
    private ViewMode viewMode = ViewMode.Grid;

    [ObservableProperty]
    private string searchTerm = string.Empty;

    partial void OnSearchTermChanged(string value)
    {
        _ = LoadGamesAsync();
    }

    [ObservableProperty]
    private string selectedSortOption = "Title";

    [ObservableProperty]
    private string selectedPlatformFilter = "All Platforms";

    partial void OnSelectedPlatformFilterChanged(string value)
    {
        _ = LoadGamesAsync();
    }

    [ObservableProperty]
    private bool isSelectionMode;

    // Pagination
    [ObservableProperty]
    private int currentPage = 1;

    [ObservableProperty]
    private int pageSize = 20;

    [ObservableProperty]
    private int totalGames;

    [ObservableProperty]
    private bool hasNextPage;

    [ObservableProperty]
    private bool hasPreviousPage;

    // Data Collections
    public ObservableCollection<GameSummaryViewModel> Games { get; }
    public ObservableCollection<CollectionViewModel> Collections { get; }
    public ObservableCollection<QuickFilterViewModel> QuickFilters { get; }
    public ObservableCollection<string> SortOptions { get; }
    public ObservableCollection<string> PlatformFilters { get; }

    // Selected Items
    [ObservableProperty]
    private CollectionViewModel? selectedCollection;

    [ObservableProperty]
    private QuickFilterViewModel? selectedQuickFilter;

    // Localized properties
    public string Title => _resources.GameLibrary_Title;
    public string NoGamesMessage => _resources.GameLibrary_NoGames;
    public string SearchPlaceholder => _resources.GameLibrary_Search_Placeholder;

    // Computed Properties
    public bool IsGridView => ViewMode == ViewMode.Grid;
    public bool IsListView => ViewMode == ViewMode.List;
    public bool HasGames => Games != null && Games.Count > 0;
    public bool HasNoGames => !HasGames;
    public string GameCountText => $"{Games?.Count ?? 0} Games";
    public bool HasSelectedGames => Games != null && Games.Any(g => g.IsSelected);
    public IEnumerable<GameSummaryViewModel> SelectedGames => Games.Where(g => g.IsSelected);

    // Commands
    public IAsyncRelayCommand LoadGamesCommand { get; }
    public IAsyncRelayCommand ScanForGamesCommand { get; }
    public IAsyncRelayCommand PickRandomGameCommand { get; }
    public ICommand SetGridViewCommand { get; }
    public ICommand SetListViewCommand { get; }
    public ICommand SelectCollectionCommand { get; }
    public ICommand ToggleGameSelectionCommand { get; }
    public IAsyncRelayCommand AddSelectedToCollectionCommand { get; }
    public IAsyncRelayCommand DeleteSelectedCommand { get; }
    public IAsyncRelayCommand ExportSelectedCommand { get; }
    public ICommand OpenGameDetailCommand { get; }
    public IAsyncRelayCommand NextPageCommand { get; }
    public IAsyncRelayCommand PreviousPageCommand { get; }
    public IAsyncRelayCommand LoadMoreCommand { get; }

    private void InitializeCollections()
    {
        Collections.Add(new CollectionViewModel("📚 All Games", "All", 0, true));
        Collections.Add(new CollectionViewModel("⭐ Favorites", "Favorites", 0, false));
        Collections.Add(new CollectionViewModel("📋 Backlog", "Backlog", 0, false));
        Collections.Add(new CollectionViewModel("✅ Completed", "Completed", 0, false));
        Collections.Add(new CollectionViewModel("🎮 Currently Playing", "Playing", 0, false));
        SelectedCollection = Collections.First();
    }

    private void InitializeQuickFilters()
    {
        QuickFilters.Add(new QuickFilterViewModel("🏷️ All", GameStatus.Installed, 0)); // Use Installed as "All" for now
        QuickFilters.Add(new QuickFilterViewModel("📥 Not Installed", GameStatus.NotInstalled, 0));
        QuickFilters.Add(new QuickFilterViewModel("🎮 Installed", GameStatus.Installed, 0));
        QuickFilters.Add(new QuickFilterViewModel("⚡ Running", GameStatus.Running, 0));
        QuickFilters.Add(new QuickFilterViewModel("🔄 Updating", GameStatus.Updating, 0));
    }

    private async Task LoadGamesAsync()
    {
        try
        {
            Console.WriteLine("[DEBUG] LoadGamesAsync started");
            var query = new GetGameSummariesQuery
            {
                PageNumber = CurrentPage,
                PageSize = PageSize,
                SearchTerm = string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm,
                PlatformFilter = SelectedPlatformFilter == "All Platforms" ? null : SelectedPlatformFilter,
                SortBy = ParseSortBy(SelectedSortOption),
                SortDescending = false // TODO: Add sort direction toggle
            };

            Console.WriteLine($"[DEBUG] Sending GetGameSummariesQuery: Page {query.PageNumber}, Search '{query.SearchTerm}'");
            var result = await _mediator.Send(query);

            if (result == null)
            {
                Console.WriteLine("[ERROR] Mediator returned null result");
                return;
            }

            if (result.IsSuccess)
            {
                Console.WriteLine($"[DEBUG] Received {result.Value.Items.Count()} games");
                Games.Clear();
                foreach (var game in result.Value.Items)
                {
                    Games.Add(new GameSummaryViewModel(game));
                }

                // Update pagination info
                TotalGames = result.Value.TotalCount;
                HasNextPage = result.Value.HasNextPage;
                HasPreviousPage = result.Value.HasPreviousPage;

                UpdateCollectionCounts();
                OnPropertyChanged(nameof(HasGames));
                OnPropertyChanged(nameof(HasNoGames));
                OnPropertyChanged(nameof(GameCountText));
                Console.WriteLine("[DEBUG] LoadGamesAsync completed successfully");
            }
            else
            {
                Console.WriteLine($"[ERROR] GetGameSummariesQuery failed: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CRITICAL] LoadGamesAsync crashed: {ex}");
            // Don't rethrow async void exceptions as they crash the app
        }
    }

    private void UpdateCollectionCounts()
    {
        // Update counts based on current games
        foreach (var collection in Collections)
        {
            collection.GameCount = Games.Count; // Simplified - implement real logic later
        }

        foreach (var filter in QuickFilters)
        {
            filter.GameCount = Games.Count; // Simplified - implement real logic later
        }
    }

    private GameSummarySortBy ParseSortBy(string sortOption)
    {
        return sortOption switch
        {
            "Title" => GameSummarySortBy.Title,
            "Platform" => GameSummarySortBy.Platform,
            "Last Played" => GameSummarySortBy.LastPlayed,
            "Playtime" => GameSummarySortBy.TotalPlayTime,
            "Recently Added" => GameSummarySortBy.Title, // TODO: Add DateAdded to sort options
            _ => GameSummarySortBy.Title
        };
    }

    private async Task ScanForGamesAsync()
    {
        // TODO: Implement game scanning
        await Task.CompletedTask;
    }

    private async Task PickRandomGameAsync()
    {
        // TODO: Implement random game picker
        await Task.CompletedTask;
    }

    private void SelectCollection(CollectionViewModel? collection)
    {
        if (SelectedCollection != null)
        {
            SelectedCollection.IsSelected = false;
        }

        SelectedCollection = collection;

        if (SelectedCollection != null)
        {
            SelectedCollection.IsSelected = true;
        }

        // TODO: Filter games based on collection
    }

    private void ToggleGameSelection(GameSummaryViewModel? game)
    {
        if (game != null && IsSelectionMode)
        {
            game.IsSelected = !game.IsSelected;
            OnPropertyChanged(nameof(HasSelectedGames));
        }
    }

    private async Task AddSelectedToCollectionAsync()
    {
        var selectedGames = SelectedGames.ToList();
        if (!selectedGames.Any()) return;

        // TODO: Show collection selection dialog
        // For now, just clear selection
        foreach (var game in selectedGames)
        {
            game.IsSelected = false;
        }

        OnPropertyChanged(nameof(HasSelectedGames));
        await Task.CompletedTask;
    }

    private async Task DeleteSelectedAsync()
    {
        var selectedGames = SelectedGames.ToList();
        if (!selectedGames.Any()) return;

        // TODO: Show confirmation dialog
        // For now, just remove from local collection
        foreach (var game in selectedGames.ToList())
        {
            Games.Remove(game);
        }

        OnPropertyChanged(nameof(HasGames));
        OnPropertyChanged(nameof(HasNoGames));
        OnPropertyChanged(nameof(GameCountText));
        OnPropertyChanged(nameof(HasSelectedGames));

        await Task.CompletedTask;
    }

    private async Task ExportSelectedAsync()
    {
        var selectedGames = SelectedGames.ToList();
        if (!selectedGames.Any()) return;

        // TODO: Implement export logic (JSON, CSV, etc.)
        // For now, just clear selection
        foreach (var game in selectedGames)
        {
            game.IsSelected = false;
        }

        OnPropertyChanged(nameof(HasSelectedGames));
        await Task.CompletedTask;
    }

    private void OpenGameDetail(GameSummaryViewModel? game)
    {
        if (game != null)
        {
            // TODO: Navigate to game detail view
        }
    }

    private async Task NextPageAsync()
    {
        if (HasNextPage)
        {
            CurrentPage++;
            await LoadGamesAsync();
        }
    }

    private async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
            await LoadGamesAsync();
        }
    }

    private async Task LoadMoreAsync()
    {
        // For infinite scroll - append to existing games
        if (!HasNextPage) return;

        var nextPage = CurrentPage + 1;
        var query = new GetGameSummariesQuery
        {
            PageNumber = nextPage,
            PageSize = PageSize,
            SearchTerm = string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm,
            PlatformFilter = SelectedPlatformFilter == "All Platforms" ? null : SelectedPlatformFilter,
            SortBy = ParseSortBy(SelectedSortOption),
            SortDescending = false
        };

        var result = await _mediator.Send(query);
        if (result.IsSuccess)
        {
            foreach (var game in result.Value.Items)
            {
                Games.Add(new GameSummaryViewModel(game));
            }

            CurrentPage = nextPage;
            TotalGames = result.Value.TotalCount;
            HasNextPage = result.Value.HasNextPage;
            HasPreviousPage = result.Value.HasPreviousPage;

            OnPropertyChanged(nameof(GameCountText));
        }
    }
}

// Supporting View Models
public enum ViewMode { Grid, List }

public class GameSummaryViewModel : ObservableObject
{
    private readonly GameSummary _gameSummary;

    public GameSummaryViewModel(GameSummary gameSummary)
    {
        _gameSummary = gameSummary;
    }

    public string Id => _gameSummary.Id.ToString();
    public string Title => _gameSummary.Title;
    public string Platform => _gameSummary.Platform;
    public GameStatus Status => _gameSummary.Status;
    public string? CoverImageUrl => _gameSummary.CoverImageUrl;
    public DateTime? LastPlayed => _gameSummary.LastPlayed;
    public TimeSpan TotalPlayTime => _gameSummary.TotalPlayTime;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public class CollectionViewModel : ObservableObject
{
    public CollectionViewModel(string name, string type, int gameCount, bool isSelected = false)
    {
        Name = name;
        Type = type;
        GameCount = gameCount;
        IsSelected = isSelected;
        Icon = name.Split(' ')[0]; // Extract emoji icon
    }

    public string Name { get; }
    public string Type { get; }
    public string Icon { get; }

    private int _gameCount;
    public int GameCount
    {
        get => _gameCount;
        set => SetProperty(ref _gameCount, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public class QuickFilterViewModel : ObservableObject
{
    public QuickFilterViewModel(string name, GameStatus status, int gameCount)
    {
        Name = name;
        Status = status;
        GameCount = gameCount;
        Icon = name.Split(' ')[0]; // Extract emoji icon
    }

    public string Name { get; }
    public GameStatus Status { get; }
    public string Icon { get; }

    private int _gameCount;
    public int GameCount
    {
        get => _gameCount;
        set => SetProperty(ref _gameCount, value);
    }
}
