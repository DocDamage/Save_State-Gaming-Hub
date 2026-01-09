using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Analytics.DTOs;
using SaveState.Core.Analytics.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the Analytics tab.
/// </summary>
public partial class AnalyticsViewModel : ObservableObject
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IAnalyticsExportService _exportService;
    private readonly SaveState.Presentation.Services.IDialogService _dialogService;
    private readonly ILogger<AnalyticsViewModel> _logger;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    // Heatmap Data
    [ObservableProperty]
    private int _selectedYear = DateTime.Now.Year;

    [ObservableProperty]
    private int _totalDays;

    [ObservableProperty]
    private int _activeDays;

    [ObservableProperty]
    private int _currentStreak;

    [ObservableProperty]
    private int _longestStreak;

    [ObservableProperty]
    private string _totalPlaytime = "0h 0m";

    [ObservableProperty]
    private ObservableCollection<DailyActivityViewModel> _heatmapActivities = new();

    // Weekly Trends
    [ObservableProperty]
    private ObservableCollection<WeeklyTrendViewModel> _weeklyTrends = new();

    [ObservableProperty]
    private int _selectedWeeks = 12;

    // Top Games
    [ObservableProperty]
    private ObservableCollection<TopGameViewModel> _topGames = new();

    // Time Distribution
    [ObservableProperty]
    private ObservableCollection<DayDistributionViewModel> _dayDistribution = new();

    [ObservableProperty]
    private ObservableCollection<HourDistributionViewModel> _hourDistribution = new();

    // Statistics
    [ObservableProperty]
    private string _averageSessionLength = "0h 0m";

    [ObservableProperty]
    private string _mostActiveDay = "N/A";

    [ObservableProperty]
    private string _mostActiveHour = "N/A";

    [ObservableProperty]
    private string _totalSessions = "0";

    public AnalyticsViewModel(
        IAnalyticsService analyticsService,
        IAnalyticsExportService exportService,
        SaveState.Presentation.Services.IDialogService dialogService,
        ILogger<AnalyticsViewModel> logger)
    {
        _analyticsService = analyticsService;
        _exportService = exportService;
        _dialogService = dialogService;
        _logger = logger;

        // Load data when ViewModel is created (fire-and-forget)
        _ = LoadAnalyticsAsync();
    }

    /// <summary>
    /// Gets the display title for the analytics tab.
    /// </summary>
    public string Title => "📊 Analytics Dashboard";

    /// <summary>
    /// Exports analytics to a file.
    /// </summary>
    [RelayCommand]
    private async Task ExportAsync()
    {
        try
        {
            var folder = await _dialogService.ShowFolderPickerAsync("Select Export Location");
            if (string.IsNullOrEmpty(folder)) return;

            IsLoading = true;
            var dataResult = await _analyticsService.GetExportDataAsync();
            if (dataResult.IsFailure)
            {
                ErrorMessage = "Failed to prepare export data.";
                IsLoading = false;
                return;
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var path = System.IO.Path.Combine(folder, $"SaveState_Analytics_{timestamp}.html");
            var result = await _exportService.GenerateHtmlReportAsync(dataResult.Value, path);

            if (result.IsSuccess)
            {
                await _dialogService.ShowInformationAsync("Export Successful", $"Analytics report exported to:\n{path}");
            }
            else
            {
                 ErrorMessage = "Failed to save export file.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export failed");
            ErrorMessage = "Export failed due to an unexpected error.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads all analytics data.
    /// </summary>
    [RelayCommand]
    private async Task LoadAnalyticsAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            await Task.WhenAll(
                LoadHeatmapAsync(),
                LoadWeeklyTrendsAsync(),
                LoadTopGamesAsync(),
                LoadTimeDistributionAsync()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load analytics data");
            ErrorMessage = "Failed to load analytics data. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads heatmap data for the selected year.
    /// </summary>
    [RelayCommand]
    private async Task LoadHeatmapAsync()
    {
        var result = await _analyticsService.GetHeatmapAsync(SelectedYear);

        if (result.IsSuccess && result.Value != null)
        {
            var data = result.Value;
            TotalDays = data.TotalDays;
            ActiveDays = data.ActiveDays;
            CurrentStreak = data.CurrentStreak;
            LongestStreak = data.LongestStreak;
            TotalPlaytime = FormatTimeSpan(data.TotalPlaytime);

            HeatmapActivities.Clear();
            foreach (var activity in data.Activities.Values.OrderBy(a => a.Date))
            {
                HeatmapActivities.Add(new DailyActivityViewModel(activity));
            }
        }
    }

    /// <summary>
    /// Loads weekly trends data.
    /// </summary>
    [RelayCommand]
    private async Task LoadWeeklyTrendsAsync()
    {
        var result = await _analyticsService.GetWeeklyTrendsAsync(SelectedWeeks);

        if (result.IsSuccess && result.Value != null)
        {
            WeeklyTrends.Clear();
            foreach (var trend in result.Value)
            {
                WeeklyTrends.Add(new WeeklyTrendViewModel(trend));
            }
        }
    }

    /// <summary>
    /// Loads top games data.
    /// </summary>
    [RelayCommand]
    private async Task LoadTopGamesAsync()
    {
        var result = await _analyticsService.GetTopGamesAsync(10);

        if (result.IsSuccess && result.Value != null)
        {
            TopGames.Clear();
            int rank = 1;
            foreach (var game in result.Value)
            {
                TopGames.Add(new TopGameViewModel(game, rank++));
            }

            // Calculate total sessions
            TotalSessions = result.Value.Sum(g => g.SessionCount).ToString();

            // Calculate average session length
            if (result.Value.Any())
            {
                var totalTime = TimeSpan.FromTicks(result.Value.Sum(g => g.TotalPlaytime.Ticks));
                var totalSessionCount = result.Value.Sum(g => g.SessionCount);
                if (totalSessionCount > 0)
                {
                    var avgSession = TimeSpan.FromTicks(totalTime.Ticks / totalSessionCount);
                    AverageSessionLength = FormatTimeSpan(avgSession);
                }
            }
        }
    }

    /// <summary>
    /// Loads time distribution data.
    /// </summary>
    [RelayCommand]
    private async Task LoadTimeDistributionAsync()
    {
        var result = await _analyticsService.GetPlaytimeDistributionAsync();

        if (result.IsSuccess && result.Value != null)
        {
            // Day distribution
            DayDistribution.Clear();
            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                var playtime = result.Value.ByDayOfWeek.TryGetValue(day, out var time) ? time : TimeSpan.Zero;
                DayDistribution.Add(new DayDistributionViewModel(day, playtime));
            }

            // Find most active day
            var mostActiveDay = DayDistribution.OrderByDescending(d => d.Playtime).FirstOrDefault();
            MostActiveDay = mostActiveDay?.DayName ?? "N/A";

            // Hour distribution
            HourDistribution.Clear();
            for (int hour = 0; hour < 24; hour++)
            {
                var playtime = result.Value.ByHour.TryGetValue(hour, out var time) ? time : TimeSpan.Zero;
                HourDistribution.Add(new HourDistributionViewModel(hour, playtime));
            }

            // Find most active hour
            var mostActiveHour = HourDistribution.OrderByDescending(h => h.Playtime).FirstOrDefault();
            MostActiveHour = mostActiveHour != null ? $"{mostActiveHour.Hour:D2}:00" : "N/A";
        }
    }

    /// <summary>
    /// Changes the selected year and reloads heatmap.
    /// </summary>
    [RelayCommand]
    private async Task ChangeYearAsync(int year)
    {
        SelectedYear = year;
        await LoadHeatmapAsync();
    }

    /// <summary>
    /// Refreshes all analytics data.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAnalyticsAsync();
    }

    private static string FormatTimeSpan(TimeSpan time)
    {
        if (time.TotalHours >= 1)
            return $"{(int)time.TotalHours}h {time.Minutes}m";
        return $"{time.Minutes}m";
    }
}

// View Models for nested data
public class DailyActivityViewModel
{
    public DailyActivityViewModel(DailyActivity activity)
    {
        Date = activity.Date;
        Playtime = activity.TotalPlaytime;
        SessionCount = activity.SessionCount;
        GamesPlayed = activity.GamesPlayed;
        Level = activity.Level;

        // Color based on activity level
        LevelColor = Level switch
        {
            ActivityLevel.None => "#1a1a1a",
            ActivityLevel.Low => "#0e4429",
            ActivityLevel.Medium => "#006d32",
            ActivityLevel.High => "#26a641",
            ActivityLevel.VeryHigh => "#39d353",
            _ => "#1a1a1a"
        };
    }

    public DateOnly Date { get; }
    public TimeSpan Playtime { get; }
    public int SessionCount { get; }
    public IReadOnlyList<string> GamesPlayed { get; }
    public ActivityLevel Level { get; }
    public string LevelColor { get; }
    public string Tooltip => $"{Date:MMM dd}: {FormatTime(Playtime)} - {SessionCount} sessions";

    private static string FormatTime(TimeSpan time) =>
        time.TotalHours >= 1 ? $"{(int)time.TotalHours}h {time.Minutes}m" : $"{time.Minutes}m";
}

public class WeeklyTrendViewModel
{
    public WeeklyTrendViewModel(WeeklyTrend trend)
    {
        WeekNumber = trend.WeekNumber;
        Playtime = trend.TotalPlaytime;
        SessionCount = trend.SessionCount;
        ChangePercent = trend.ChangePercent;

        PlaytimeText = FormatTime(Playtime);
        ChangeText = ChangePercent >= 0 ? $"+{ChangePercent:F1}%" : $"{ChangePercent:F1}%";
        IsPositiveChange = ChangePercent >= 0;
    }

    public int WeekNumber { get; }
    public TimeSpan Playtime { get; }
    public int SessionCount { get; }
    public float ChangePercent { get; }
    public string PlaytimeText { get; }
    public string ChangeText { get; }
    public bool IsPositiveChange { get; }

    private static string FormatTime(TimeSpan time) =>
        time.TotalHours >= 1 ? $"{(int)time.TotalHours}h {time.Minutes}m" : $"{time.Minutes}m";
}

public class TopGameViewModel
{
    public TopGameViewModel(TopGame game, int rank)
    {
        Rank = rank;
        Title = game.Title;
        Playtime = game.TotalPlaytime;
        SessionCount = game.SessionCount;

        PlaytimeText = FormatTime(Playtime);
        RankEmoji = rank switch
        {
            1 => "🥇",
            2 => "🥈",
            3 => "🥉",
            _ => $"#{rank}"
        };
    }

    public int Rank { get; }
    public string RankEmoji { get; }
    public string Title { get; }
    public TimeSpan Playtime { get; }
    public int SessionCount { get; }
    public string PlaytimeText { get; }

    private static string FormatTime(TimeSpan time) =>
        time.TotalHours >= 1 ? $"{(int)time.TotalHours}h" : $"{time.Minutes}m";
}

public class DayDistributionViewModel
{
    public DayDistributionViewModel(DayOfWeek day, TimeSpan playtime)
    {
        Day = day;
        Playtime = playtime;
        DayName = day.ToString();
        PlaytimeText = FormatTime(playtime);
    }

    public DayOfWeek Day { get; }
    public string DayName { get; }
    public TimeSpan Playtime { get; }
    public string PlaytimeText { get; }

    private static string FormatTime(TimeSpan time) =>
        time.TotalHours >= 1 ? $"{(int)time.TotalHours}h" : $"{time.Minutes}m";
}

public class HourDistributionViewModel
{
    public HourDistributionViewModel(int hour, TimeSpan playtime)
    {
        Hour = hour;
        Playtime = playtime;
        HourText = $"{hour:D2}:00";
        PlaytimeText = FormatTime(playtime);
    }

    public int Hour { get; }
    public string HourText { get; }
    public TimeSpan Playtime { get; }
    public string PlaytimeText { get; }

    private static string FormatTime(TimeSpan time) =>
        time.TotalHours >= 1 ? $"{(int)time.TotalHours}h" : $"{time.Minutes}m";
}
