// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.SmartLauncher;

namespace SaveState.Presentation.ViewModels.SmartLauncher;

/// <summary>
/// ViewModel for Smart Launcher statistics page.
/// </summary>
public sealed partial class SmartLauncherStatisticsViewModel : ObservableObject
{
    private readonly ISmartLauncherStatisticsService _statisticsService;
    private readonly ILogger<SmartLauncherStatisticsViewModel> _logger;

    [ObservableProperty]
    private SmartLauncherStatistics _overallStats = new();

    [ObservableProperty]
    private PerformanceComparison _performanceComparison = new();

    [ObservableProperty]
    private ObservableCollection<MostPlayedGameViewModel> _mostPlayedGames = new();

    [ObservableProperty]
    private string _totalGamingTimeText = "0h 0m";

    [ObservableProperty]
    private string _timeSavedText = "0h 0m";

    [ObservableProperty]
    private bool _isLoading;

    public SmartLauncherStatisticsViewModel(
        ISmartLauncherStatisticsService statisticsService,
        ILogger<SmartLauncherStatisticsViewModel> logger)
    {
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _ = LoadStatisticsAsync();
    }

    [RelayCommand]
    private async Task LoadStatisticsAsync()
    {
        IsLoading = true;
        try
        {
            var overallStatsResult = await _statisticsService.GetOverallStatisticsAsync();
            if (overallStatsResult.IsSuccess && overallStatsResult.Value is not null)
            {
                OverallStats = overallStatsResult.Value;
            }
            else
            {
                _logger.LogWarning("Failed to load overall launcher statistics: {Error}", overallStatsResult.Error);
            }

            var comparisonResult = await _statisticsService.GetPerformanceComparisonAsync();
            if (comparisonResult.IsSuccess && comparisonResult.Value is not null)
            {
                PerformanceComparison = comparisonResult.Value;
            }
            else
            {
                _logger.LogWarning("Failed to load performance comparison: {Error}", comparisonResult.Error);
            }

            var mostPlayedResult = await _statisticsService.GetMostPlayedGamesAsync(10);
            MostPlayedGames.Clear();
            if (mostPlayedResult.IsSuccess && mostPlayedResult.Value is not null)
            {
                foreach (var game in mostPlayedResult.Value)
                {
                    MostPlayedGames.Add(new MostPlayedGameViewModel(game));
                }
            }
            else
            {
                _logger.LogWarning("Failed to load most played games: {Error}", mostPlayedResult.Error);
            }

            // Format display text
            TotalGamingTimeText = FormatTimeSpan(OverallStats.TotalGamingTime);
            TimeSavedText = FormatTimeSpan(OverallStats.TotalTimeSaved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load statistics");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadStatisticsAsync();
    }

    [RelayCommand]
    private async Task ExportStatisticsAsync()
    {
        try
        {
            // Would open a save dialog and export to JSON/CSV
            _logger.LogInformation("Exporting statistics...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export statistics");
        }
    }

    private static string FormatTimeSpan(TimeSpan time)
    {
        if (time.TotalHours >= 1)
        {
            return $"{time.TotalHours:F0}h {time.Minutes}m";
        }
        return $"{time.TotalMinutes:F0}m";
    }
}

/// <summary>
/// ViewModel for most played game display.
/// </summary>
public sealed class MostPlayedGameViewModel : ObservableObject
{
    private readonly MostPlayedGame _game;

    public MostPlayedGameViewModel(MostPlayedGame game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
    }

    public Guid GameId => _game.GameId;
    public string GameName => _game.GameName;
    public int SessionCount => _game.SessionCount;
    public string TotalPlayTimeText => FormatTimeSpan(_game.TotalPlayTime);
    public string LastPlayedText => _game.LastPlayed.HasValue 
        ? _game.LastPlayed.Value.ToString("MMM dd, yyyy") 
        : "Never";

    private static string FormatTimeSpan(TimeSpan time)
    {
        if (time.TotalHours >= 1)
        {
            return $"{time.TotalHours:F1}h";
        }
        return $"{time.TotalMinutes:F0}m";
    }
}
