using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.Analytics.Queries;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using SaveState.Core.Analytics.Services;
using SaveState.Core.UserManagement.Services;
using System.Linq;

namespace SaveState.Presentation.ViewModels.Analytics;

public partial class AnalyticsDashboardViewModel : ObservableObject, IDisposable
{
    private readonly IMediator _mediator;
    private readonly ILogger<AnalyticsDashboardViewModel> _logger;
    private readonly INotificationService _notificationService;
    private readonly IRealTimeNotificationService _realTimeService;
    private readonly IAnalyticsExportService _exportService;
    private readonly IUserContextService? _userContextService;
    private readonly DispatcherTimer _refreshTimer;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _totalPlayTime = "0h";

    [ObservableProperty]
    private int _totalGames = 0;

    [ObservableProperty]
    private int _totalSessions = 0;

    [ObservableProperty]
    private string _averageSessionLength = "0m";

    [ObservableProperty]
    private string _mostPlayedDay = "N/A";

    [ObservableProperty]
    private string _mostPlayedTime = "N/A";

    [ObservableProperty]
    private double _sessionsPerWeek = 0;

    [ObservableProperty]
    private ObservableCollection<ChartDataPoint> _dayOfWeekData = new();

    [ObservableProperty]
    private ObservableCollection<ChartDataPoint> _hourOfDayData = new();

    [ObservableProperty]
    private ObservableCollection<GenrePlaytimeData> _genreData = new();

    [ObservableProperty]
    private ObservableCollection<PlayStreakViewModel> _playStreaks = new();

    [ObservableProperty]
    private string _longestStreak = "0 days";

    [ObservableProperty]
    private string _currentStreak = "0 days";

    [ObservableProperty]
    private ObservableCollection<CompletionPrediction> _predictions = new();

    [ObservableProperty]
    private ObservableCollection<SaveState.Application.Analytics.Queries.BacklogDataPoint> _backlogData = new();

    [ObservableProperty]
    private ObservableCollection<SaveState.Application.Analytics.Queries.CompletionRateDataPoint> _completionRates = new();

    [ObservableProperty]
    private string _currentUsername = "Guest";

    [ObservableProperty]
    private Guid? _currentUserId;

    [ObservableProperty]
    private ObservableCollection<TimeSeriesDataPoint> _playTimeTimeSeries = new();

    [ObservableProperty]
    private ObservableCollection<ComparisonData> _weeklyComparisons = new();

    [ObservableProperty]
    private ObservableCollection<HeatmapCell> _activityHeatmap = new();

    [ObservableProperty]
    private ObservableCollection<AdvancedChartDataPoint> _platformDistribution = new();

    public AnalyticsDashboardViewModel(
        IMediator mediator,
        ILogger<AnalyticsDashboardViewModel> logger,
        INotificationService notificationService,
        IRealTimeNotificationService realTimeService,
        IAnalyticsExportService exportService,
        IUserContextService? userContextService = null)
    {
        _mediator = mediator;
        _logger = logger;
        _notificationService = notificationService;
        _realTimeService = realTimeService;
        _exportService = exportService;
        _userContextService = userContextService;

        // Load user context
        if (_userContextService != null)
        {
            CurrentUserId = _userContextService.CurrentUserId;
            CurrentUsername = _userContextService.CurrentUsername ?? "Guest";
            _logger.LogInformation("Analytics loaded for user: {Username} ({UserId})", CurrentUsername, CurrentUserId);
        }

        _realTimeService.OnAnalyticsUpdated += OnAnalyticsUpdated;

        _refreshTimer = new DispatcherTimer(
            TimeSpan.FromSeconds(60),
            DispatcherPriority.Background,
            (s, e) => _ = LoadAnalyticsAsync());
        _refreshTimer.Start();

        _ = LoadAnalyticsAsync();
    }

    [RelayCommand]
    private async Task LoadAnalyticsAsync()
    {
        try
        {
            IsLoading = true;
            _logger.LogInformation("Loading analytics dashboard...");

            var result = await _mediator.Send(new GetPlayPatternsQuery());

            if (result.IsSuccess && result.Value != null)
            {
                var analytics = result.Value;

                // Update summary stats
                TotalSessions = analytics.DayOfWeekDistribution.Values.Sum(v => (int)v);
                AverageSessionLength = FormatDuration(analytics.AverageSession.AverageDuration);
                MostPlayedTime = analytics.AverageSession.MostPlayedTime;
                SessionsPerWeek = analytics.AverageSession.SessionsPerWeek;

                // Day of week chart
                DayOfWeekData.Clear();
                foreach (var (day, percentage) in analytics.DayOfWeekDistribution.OrderBy(x => x.Key))
                {
                    DayOfWeekData.Add(new ChartDataPoint(
                        day.ToString().Substring(0, 3),
                        percentage));
                }

                MostPlayedDay = DayOfWeekData.OrderByDescending(d => d.Value).FirstOrDefault()?.Label ?? "N/A";

                // Hour of day chart
                HourOfDayData.Clear();
                foreach (var (hour, percentage) in analytics.HourOfDayDistribution.OrderBy(x => x.Key))
                {
                    HourOfDayData.Add(new ChartDataPoint(
                        $"{hour:D2}:00",
                        percentage));
                }

                // Genre playtime
                GenreData.Clear();
                var totalMinutes = analytics.GenrePlaytime.Values.Sum();
                foreach (var (genre, minutes) in analytics.GenrePlaytime.OrderByDescending(x => x.Value).Take(10))
                {
                    var hours = minutes / 60.0;
                    var percentage = totalMinutes > 0 ? (double)minutes / totalMinutes * 100 : 0;
                    GenreData.Add(new GenrePlaytimeData(
                        genre,
                        hours,
                        percentage));
                }

                TotalPlayTime = FormatDuration(TimeSpan.FromMinutes(totalMinutes));

                // Play streaks
                PlayStreaks.Clear();
                foreach (var streak in analytics.PlayStreaks)
                {
                    PlayStreaks.Add(new PlayStreakViewModel(
                        streak.StartDate,
                        streak.EndDate,
                        streak.DaysCount,
                        streak.TotalSessions));
                }

                if (PlayStreaks.Any())
                {
                    LongestStreak = $"{PlayStreaks.Max(s => s.DaysCount)} days";

                    // Check if current streak is ongoing (ended yesterday or today)
                    var latestStreak = PlayStreaks.OrderByDescending(s => s.EndDate).FirstOrDefault();
                    if (latestStreak != null && (DateTime.Now.Date - latestStreak.EndDate).TotalDays <= 1)
                    {
                        CurrentStreak = $"{latestStreak.DaysCount} days";
                    }
                }

                // Predictions
                Predictions.Clear();
                foreach (var p in analytics.CompletionPredictions)
                {
                    Predictions.Add(p);
                }

                // Backlog Burndown
                BacklogData.Clear();
                foreach (var b in analytics.BacklogBurndown)
                {
                    BacklogData.Add(b);
                }

                // Completion Rates
                CompletionRates.Clear();
                foreach (var c in analytics.CompletionRates)
                {
                    CompletionRates.Add(c);
                }

                _logger.LogInformation("Analytics dashboard loaded successfully");
            }
            else
            {
                _notificationService.ShowWarning("No analytics data available", "Analytics");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load analytics");
            _notificationService.ShowError("Failed to load analytics data", "Error");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Removed old ExportDataAsync - replaced by ExportToJsonAsync, ExportToCsvAsync, and GenerateReportAsync

    [RelayCommand]
    private async Task ExportToCsvAsync()
    {
        try
        {
            _logger.LogInformation("Exporting analytics to CSV...");

            var exportData = BuildExportData();
            var fileName = $"analytics_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SaveState", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            var result = await _exportService.ExportToCsvAsync(exportData, filePath);

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Analytics exported to: {result.Value}", "Export Complete");
                _logger.LogInformation("Analytics exported to CSV: {FilePath}", result.Value);
            }
            else
            {
                _notificationService.ShowError(result.Error, "Export Failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export analytics to CSV");
            _notificationService.ShowError("Failed to export analytics data", "Error");
        }
    }

    [RelayCommand]
    private async Task ExportToJsonAsync()
    {
        try
        {
            _logger.LogInformation("Exporting analytics to JSON...");

            var exportData = BuildExportData();
            var fileName = $"analytics_export_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SaveState", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            var result = await _exportService.ExportToJsonAsync(exportData, filePath);

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Analytics exported to: {result.Value}", "Export Complete");
                _logger.LogInformation("Analytics exported to JSON: {FilePath}", result.Value);
            }
            else
            {
                _notificationService.ShowError(result.Error, "Export Failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export analytics to JSON");
            _notificationService.ShowError("Failed to export analytics data", "Error");
        }
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        try
        {
            _logger.LogInformation("Generating HTML report...");

            var exportData = BuildExportData();
            var fileName = $"analytics_report_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SaveState", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            var result = await _exportService.GenerateHtmlReportAsync(exportData, filePath);

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Report generated: {result.Value}", "Report Complete");
                _logger.LogInformation("HTML report generated: {FilePath}", result.Value);

                // Open the report in default browser
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = result.Value,
                    UseShellExecute = true
                });
            }
            else
            {
                _notificationService.ShowError(result.Error, "Report Failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate HTML report");
            _notificationService.ShowError("Failed to generate report", "Error");
        }
    }

    private AnalyticsExportData BuildExportData()
    {
        // Build session data (empty for now, can be enhanced later)
        var sessions = new List<SessionExportData>();

        var genres = GenreData.Select(g => new GenreExportData(g.Genre, g.Hours, g.Percentage)).ToList();
        var streaks = PlayStreaks.Select(s => new StreakExportData(s.StartDate, s.EndDate, s.DaysCount, s.TotalSessions)).ToList();
        var predictions = Predictions.Select(p => new PredictionExportData(p.GameName, FormatDuration(p.EstimatedTimeRemaining), p.ConfidenceScore)).ToList();

        return new AnalyticsExportData(
            DateTime.Now,
            TotalPlayTime,
            TotalGames,
            TotalSessions,
            AverageSessionLength,
            sessions,
            genres,
            streaks,
            predictions);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{duration.TotalHours:F1}h";
        if (duration.TotalMinutes >= 1)
            return $"{duration.TotalMinutes:F0}m";
        return $"{duration.TotalSeconds:F0}s";
    }

    private void OnAnalyticsUpdated(object? sender, AnalyticsUpdatedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => LoadAnalyticsAsync());
    }

    public void Dispose()
    {
        _realTimeService.OnAnalyticsUpdated -= OnAnalyticsUpdated;
        _refreshTimer.Stop();
        GC.SuppressFinalize(this);
    }
}

public record ChartDataPoint(string Label, double Value);

public record GenrePlaytimeData(string Genre, double Hours, double Percentage);

/// <summary>
/// Advanced chart data point with color and tooltip support.
/// </summary>
public record AdvancedChartDataPoint(
    string Label,
    double Value,
    string Color,
    string TooltipText,
    string Category);

/// <summary>
/// Time series data point for trend analysis.
/// </summary>
public record TimeSeriesDataPoint(
    DateTime Timestamp,
    double Value,
    string Series,
    string Unit);

/// <summary>
/// Comparison data for showing side-by-side metrics.
/// </summary>
public record ComparisonData(
    string Name,
    double CurrentValue,
    double PreviousValue,
    double PercentageChange,
    string Trend); // "Up", "Down", "Stable"

/// <summary>
/// Heatmap cell data for activity visualization.
/// </summary>
public record HeatmapCell(
    DateTime Date,
    int Intensity, // 0-10 scale
    string TooltipText);

public partial class PlayStreakViewModel : ObservableObject
{
    public PlayStreakViewModel(DateTime startDate, DateTime endDate, int daysCount, int totalSessions)
    {
        StartDate = startDate;
        EndDate = endDate;
        DaysCount = daysCount;
        TotalSessions = totalSessions;
    }

    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public int DaysCount { get; }
    public int TotalSessions { get; }

    public string DateRange => $"{StartDate:MMM d} - {EndDate:MMM d, yyyy}";
    public string StreakText => $"{DaysCount} day{(DaysCount != 1 ? "s" : "")}";
    public string SessionsText => $"{TotalSessions} session{(TotalSessions != 1 ? "s" : "")}";
}

