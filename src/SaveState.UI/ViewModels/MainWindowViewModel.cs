using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase? _currentPage;

    [ObservableProperty]
    private bool _isSidebarOpen = true;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public IRelayCommand ToggleSidebarCommand { get; }
    public IRelayCommand<string> NavigateCommand { get; }
    public IAsyncRelayCommand ScanLibrariesCommand { get; }

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger = Log.ForContext<MainWindowViewModel>();

    public MainWindowViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        ToggleSidebarCommand = new RelayCommand(() => IsSidebarOpen = !IsSidebarOpen);
        NavigateCommand = new RelayCommand<string>(Navigate);
        ScanLibrariesCommand = new AsyncRelayCommand(ScanLibrariesAsync);
        
        // Initial page (e.g., Library/Grid)
        Navigate("Library");
    }

    private void Navigate(string? target)
    {
        switch (target)
        {
            case "Library":
                CurrentPage = _serviceProvider.GetRequiredService<GameGridViewModel>();
                break;
            case "Settings":
                CurrentPage = _serviceProvider.GetRequiredService<SettingsViewModel>();
                break;
            case "ROMs":
                CurrentPage = _serviceProvider.GetRequiredService<RomManagerViewModel>();
                break;
            case "AI":
                CurrentPage = _serviceProvider.GetRequiredService<AiAssistantViewModel>();
                break;
            case "Stats":
                CurrentPage = _serviceProvider.GetRequiredService<StatisticsViewModel>();
                break;
            case "Collections":
                CurrentPage = _serviceProvider.GetRequiredService<CollectionsViewModel>();
                break;
            case "Knowledge":
                CurrentPage = _serviceProvider.GetRequiredService<KnowledgeViewModel>();
                break;
            default:
                CurrentPage = null;
                break;
        }
    }

    public void ShowGameDetails(Game game)
    {
        CurrentPage = new GameDetailsViewModel(_serviceProvider, game, () => Navigate("Library"));
    }

    private async Task ScanLibrariesAsync()
    {
        IsScanning = true;
        StatusMessage = "Scanning game libraries...";
        var totalGames = 0;
        var totalCovers = 0;

        try
        {
            var providers = _serviceProvider.GetServices<IGameProvider>();
            var metadataProviders = _serviceProvider.GetServices<IMetadataProvider>().ToList();
            
            using var scope = _serviceProvider.CreateScope();
            var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<SaveState.Core.Data.SaveStateDbContext>();
            
            // Get or create PC platform
            var pcPlatform = dbContext.Platforms.FirstOrDefault(p => p.Name == "PC");
            if (pcPlatform == null)
            {
                pcPlatform = new Platform { Name = "PC" };
                dbContext.Platforms.Add(pcPlatform);
                await dbContext.SaveChangesAsync();
            }

            var newGames = new List<Game>();

            foreach (var provider in providers)
            {
                StatusMessage = $"Scanning {provider.Name}...";
                _logger.Information("Scanning {Provider}...", provider.Name);

                try
                {
                    var games = await provider.GetInstalledGamesAsync();
                    foreach (var game in games)
                    {
                        // Check if game already exists
                        var existing = dbContext.Games.FirstOrDefault(g => 
                            g.Source == game.Source && g.SourceId == game.SourceId);
                        
                        if (existing == null)
                        {
                            game.Platform = pcPlatform;
                            game.PlatformId = pcPlatform.Id;
                            await gameService.AddAsync(game);
                            newGames.Add(game);
                            totalGames++;
                        }
                    }
                    _logger.Information("Found {Count} games from {Provider}", games.Count(), provider.Name);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Error scanning {Provider}", provider.Name);
                }
            }

            // Fetch cover art for new games
            if (newGames.Count > 0 && metadataProviders.Any())
            {
                StatusMessage = $"Fetching artwork for {newGames.Count} games...";
                var steamGridDb = metadataProviders.FirstOrDefault(p => p.Id == "steamgriddb");
                
                if (steamGridDb != null)
                {
                    foreach (var game in newGames.Take(20)) // Limit to avoid rate limits
                    {
                        try
                        {
                            StatusMessage = $"Getting cover: {game.Title}...";
                            var coverPath = await steamGridDb.GetCoverImageAsync(game.Title);
                            if (!string.IsNullOrEmpty(coverPath))
                            {
                                game.CoverImage = coverPath;
                                await gameService.UpdateAsync(game);
                                totalCovers++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug(ex, "Failed to get cover for {Title}", game.Title);
                        }
                    }
                }
            }

            StatusMessage = totalCovers > 0 
                ? $"Added {totalGames} games, {totalCovers} covers!" 
                : $"Added {totalGames} new games.";
            
            // Refresh the grid
            Navigate("Library");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Library scan failed");
            StatusMessage = "Scan failed. Check logs for details.";
        }
        finally
        {
            IsScanning = false;
        }
    }
}
