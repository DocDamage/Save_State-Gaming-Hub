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

public partial class StatisticsViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger = Log.ForContext<StatisticsViewModel>();

    [ObservableProperty]
    private int _totalGames;

    [ObservableProperty]
    private int _installedGames;

    [ObservableProperty]
    private int _romCount;

    [ObservableProperty]
    private TimeSpan _totalPlayTime;

    [ObservableProperty]
    private string _totalPlayTimeFormatted = "0h";

    [ObservableProperty]
    private ObservableCollection<SourceStat> _sourceStats = new();

    [ObservableProperty]
    private ObservableCollection<PlatformStat> _platformStats = new();

    [ObservableProperty]
    private ObservableCollection<Game> _recentGames = new();

    [ObservableProperty]
    private ObservableCollection<Game> _topPlayedGames = new();

    [ObservableProperty]
    private bool _isLoading;

    public IAsyncRelayCommand LoadStatsCommand { get; }

    public StatisticsViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        LoadStatsCommand = new AsyncRelayCommand(LoadStatsAsync);
        _ = LoadStatsAsync();
    }

    public async Task LoadStatsAsync()
    {
        IsLoading = true;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
            var games = (await gameService.GetAllAsync()).ToList();

            TotalGames = games.Count;
            InstalledGames = games.Count(g => g.IsInstalled);
            RomCount = games.Count(g => g.Source == "ROM");
            
            TotalPlayTime = TimeSpan.FromTicks(games.Sum(g => g.PlayTime.Ticks));
            TotalPlayTimeFormatted = FormatPlayTime(TotalPlayTime);

            // Source breakdown
            SourceStats = new ObservableCollection<SourceStat>(
                games.GroupBy(g => g.Source ?? "Unknown")
                     .Select(g => new SourceStat { Name = g.Key, Count = g.Count() })
                     .OrderByDescending(s => s.Count)
            );

            // Platform breakdown  
            PlatformStats = new ObservableCollection<PlatformStat>(
                games.Where(g => g.Platform != null)
                     .GroupBy(g => g.Platform!.Name)
                     .Select(g => new PlatformStat { Name = g.Key, Count = g.Count() })
                     .OrderByDescending(p => p.Count)
                     .Take(10)
            );

            // Recent games (last added)
            RecentGames = new ObservableCollection<Game>(
                games.OrderByDescending(g => g.Id).Take(5)
            );

            // Top played
            TopPlayedGames = new ObservableCollection<Game>(
                games.OrderByDescending(g => g.PlayTime).Take(5)
            );
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load statistics");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string FormatPlayTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
            return $"{(int)time.TotalHours}h {time.Minutes}m";
        if (time.TotalMinutes >= 1)
            return $"{(int)time.TotalMinutes}m";
        return "0h";
    }
}

public class SourceStat
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class PlatformStat
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}
