using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.UI.ViewModels;

public partial class GameGridViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger = Log.ForContext<GameGridViewModel>();
    private List<Game> _allGames = new();

    [ObservableProperty]
    private ObservableCollection<Game> _games = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private Game? _selectedGame;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _selectedSource = "All";

    [ObservableProperty]
    private string _selectedSort = "Title";

    [ObservableProperty]
    private bool _showInstalledOnly;

    [ObservableProperty]
    private int _totalGames;

    [ObservableProperty]
    private int _filteredGames;

    public ObservableCollection<string> Sources { get; } = new() { "All", "Steam", "GOG", "Epic", "Xbox", "EA", "Ubisoft", "ROM" };
    public ObservableCollection<string> SortOptions { get; } = new() { "Title", "Recently Added", "Platform", "Source" };

    public GameGridViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        LoadGamesCommand = new AsyncRelayCommand(LoadGamesAsync);
        OpenDetailsCommand = new AsyncRelayCommand<Game>(OpenDetailsAsync);
        
        // Auto-load games when view is created
        _ = LoadGamesAsync();
    }

    public IAsyncRelayCommand LoadGamesCommand { get; }
    public IAsyncRelayCommand<Game> OpenDetailsCommand { get; }

    partial void OnSearchQueryChanged(string value) => ApplyFilters();
    partial void OnSelectedSourceChanged(string value) => ApplyFilters();
    partial void OnSelectedSortChanged(string value) => ApplyFilters();
    partial void OnShowInstalledOnlyChanged(bool value) => ApplyFilters();

    public async Task LoadGamesAsync()
    {
        IsLoading = true;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
            _allGames = (await gameService.GetAllAsync()).ToList();
            TotalGames = _allGames.Count;
            ApplyFilters();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allGames.AsEnumerable();

        // Search
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var query = SearchQuery.ToLowerInvariant();
            filtered = filtered.Where(g => 
                g.Title.ToLowerInvariant().Contains(query) ||
                (g.Description?.ToLowerInvariant().Contains(query) ?? false));
        }

        // Source filter
        if (SelectedSource != "All")
        {
            filtered = filtered.Where(g => 
                g.Source?.Equals(SelectedSource, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        // Installed filter
        if (ShowInstalledOnly)
        {
            filtered = filtered.Where(g => g.IsInstalled);
        }

        // Sort
        filtered = SelectedSort switch
        {
            "Title" => filtered.OrderBy(g => g.SortTitle ?? g.Title),
            "Recently Added" => filtered.OrderByDescending(g => g.Id),
            "Platform" => filtered.OrderBy(g => g.Platform?.Name ?? "Z"),
            "Source" => filtered.OrderBy(g => g.Source ?? "Z"),
            _ => filtered.OrderBy(g => g.Title)
        };

        var list = filtered.ToList();
        FilteredGames = list.Count;
        Games = new ObservableCollection<Game>(list);
    }

    private Task OpenDetailsAsync(Game? game)
    {
        if (game == null) return Task.CompletedTask;

        _logger.Information("Opening details for: {Title}", game.Title);

        var mainVm = _serviceProvider.GetService<MainWindowViewModel>();
        mainVm?.ShowGameDetails(game);
        
        return Task.CompletedTask;
    }
}
