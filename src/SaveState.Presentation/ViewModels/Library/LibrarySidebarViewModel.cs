using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Library;

/// <summary>
/// Enumeration for different library view modes.
/// </summary>
public enum ViewMode
{
    /// <summary>
    /// Grid view - displays games in a grid layout.
    /// </summary>
    Grid,

    /// <summary>
    /// List view - displays games in a vertical list.
    /// </summary>
    List,

    /// <summary>
    /// Compact view - displays games in a compact grid.
    /// </summary>
    Compact,

    /// <summary>
    /// Table view - displays games in a detailed table.
    /// </summary>
    Table
}

/// <summary>
/// View model for the Library sidebar.
/// </summary>
public partial class LibrarySidebarViewModel : ObservableObject
{
    private readonly IVirtualCollectionService _collectionService;
    private readonly IPlatformRepository _platformRepository;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<LibrarySidebarViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<SidebarItemViewModel> _smartFilters = new();

    [ObservableProperty]
    private ObservableCollection<SidebarItemViewModel> _collections = new();

    [ObservableProperty]
    private ObservableCollection<SidebarItemViewModel> _platforms = new();

    [ObservableProperty]
    private SidebarItemViewModel? _selectedSmartFilter;

    [ObservableProperty]
    private SidebarItemViewModel? _selectedCollection;

    [ObservableProperty]
    private SidebarItemViewModel? _selectedPlatform;

    [ObservableProperty]
    private int _totalGames;

    [ObservableProperty]
    private int _installedGames;

    [ObservableProperty]
    private int _currentlyPlaying;

    [ObservableProperty]
    private int _completedGames;

    public LibrarySidebarViewModel(
        IVirtualCollectionService collectionService,
        IPlatformRepository platformRepository,
        IGameRepository gameRepository,
        ILogger<LibrarySidebarViewModel> logger)
    {
        _collectionService = collectionService;
        _platformRepository = platformRepository;
        _gameRepository = gameRepository;
        _logger = logger;

        InitializeSidebarItems();
    }

    private void InitializeSidebarItems()
    {
        // Smart Filters
        SmartFilters.Add(new SidebarItemViewModel("all", "All Games", "🎮", 0, () => SelectSmartFilter("all")));
        SmartFilters.Add(new SidebarItemViewModel("favorites", "Favorites", "⭐", 0, () => SelectSmartFilter("favorites")));
        SmartFilters.Add(new SidebarItemViewModel("backlog", "Backlog", "📋", 0, () => SelectSmartFilter("backlog")));
        SmartFilters.Add(new SidebarItemViewModel("completed", "Completed", "✅", 0, () => SelectSmartFilter("completed")));
        SmartFilters.Add(new SidebarItemViewModel("playing", "Playing", "▶️", 0, () => SelectSmartFilter("playing")));
        SmartFilters.Add(new SidebarItemViewModel("installed", "Installed", "💾", 0, () => SelectSmartFilter("installed")));
        SmartFilters.Add(new SidebarItemViewModel("not_installed", "Uninstalled", "☁️", 0, () => SelectSmartFilter("not_installed")));
        SmartFilters.Add(new SidebarItemViewModel("hidden", "Hidden", "👁️", 0, () => SelectSmartFilter("hidden")));

        // Default selection
        SelectedSmartFilter = SmartFilters.First();
    }

    [RelayCommand]
    private async Task LoadSidebarDataAsync()
    {
        await Task.WhenAll(
            LoadCollectionsAsync(),
            LoadPlatformsAsync(),
            LoadStatisticsAsync()
        );
    }

    private async Task LoadCollectionsAsync()
    {
        try
        {
            var result = await _collectionService.GetAllCollectionsAsync(includeSystem: true);
            if (result.IsSuccess && result.Value != null)
            {
                Collections.Clear();
                foreach (var collection in result.Value)
                {
                    Collections.Add(new SidebarItemViewModel(
                        collection.Id.ToString(),
                        collection.Name,
                        "📁",
                        0, // Count can be added if service supports it
                        () => SelectCollection(collection.Id)));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load collections");
        }
    }

    private async Task LoadPlatformsAsync()
    {
        try
        {
            var platforms = await _platformRepository.GetAllAsync();
            var stats = await _gameRepository.GetPlatformStatisticsAsync();

            Platforms.Clear();
            foreach (var platform in platforms)
            {
                var count = stats.TryGetValue(platform.Name.Value, out var c) ? c : 0;
                Platforms.Add(new SidebarItemViewModel(
                    platform.Id.ToString(),
                    platform.Name.Value,
                    "🎮",
                    count,
                    () => SelectPlatform(platform.Id.ToString())));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load platforms");
        }
    }

    private async Task LoadStatisticsAsync()
    {
        try
        {
            TotalGames = await _gameRepository.CountAsync();

            // Get stats from DB directly or use efficient queries
            var installedResult = await _gameRepository.GetGamesAsync(1, 1, statusFilter: Core.GameLibrary.Enums.GameStatus.Installed);
            InstalledGames = installedResult.TotalCount;

            var playingResult = await _gameRepository.GetGamesAsync(1, 1, statusFilter: Core.GameLibrary.Enums.GameStatus.Running);
            CurrentlyPlaying = playingResult.TotalCount;

            // Completed games logic depends on status or tag, assuming 'Completed' status if it exists, otherwise 0 for now
            // Or use a smart filter for it? For now, 0 or check tags if possible.
            CompletedGames = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load statistics");
        }
    }

    [RelayCommand]
    private void CreateCollection()
    {
        _logger.LogInformation("Create collection dialog requested");
        // Future: Show collection creation dialog with name, description, and rules
    }

    public event EventHandler? FilterChanged;

    private void SelectSmartFilter(string filterId)
    {
        var filter = SmartFilters.FirstOrDefault(f => f.Id == filterId);
        if (filter == null || SelectedSmartFilter == filter) return;

        _selectedCollection = null;
        OnPropertyChanged(nameof(SelectedCollection));
        _selectedPlatform = null;
        OnPropertyChanged(nameof(SelectedPlatform));

        SelectedSmartFilter = filter;
        FilterChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SelectCollection(Guid collectionId)
    {
        var collection = Collections.FirstOrDefault(c => c.Id == collectionId.ToString());
        if (collection == null || SelectedCollection == collection) return;

        _selectedSmartFilter = null;
        OnPropertyChanged(nameof(SelectedSmartFilter));
        _selectedPlatform = null;
        OnPropertyChanged(nameof(SelectedPlatform));

        SelectedCollection = collection;
        FilterChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SelectPlatform(string platformId)
    {
        var platform = Platforms.FirstOrDefault(p => p.Id == platformId);
        if (platform == null || SelectedPlatform == platform) return;

        _selectedSmartFilter = null;
        OnPropertyChanged(nameof(SelectedSmartFilter));
        _selectedCollection = null;
        OnPropertyChanged(nameof(SelectedCollection));

        SelectedPlatform = platform;
        FilterChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// View model for individual sidebar items.
/// </summary>
public class SidebarItemViewModel : ObservableObject
{
    public SidebarItemViewModel(string id, string name, string icon, int count, Action selectAction)
    {
        Id = id;
        Name = name;
        Icon = icon;
        Count = count;
        SelectCommand = new RelayCommand(selectAction);
    }

    public string Id { get; }
    public string Name { get; }
    public string Icon { get; }
    public int Count { get; }
    public IRelayCommand SelectCommand { get; }
}
