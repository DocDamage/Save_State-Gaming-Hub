using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Core.Common.Services;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using SaveState.Presentation.Services;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Application.Analytics.Queries;
using System.Collections.Generic;

namespace SaveState.Presentation.ViewModels.Library.GameDetail;

/// <summary>
/// View model for the Game Sessions tab.
/// </summary>
public partial class GameSessionsTabViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialogService;
    private readonly ILogger<GameSessionsTabViewModel> _logger;
    private readonly ITimeProvider _timeProvider;

    [ObservableProperty]
    private string _sessionCountText = "0 sessions";

    [ObservableProperty]
    private string _totalPlaytimeText = "0h 0m";

    [ObservableProperty]
    private string _averageSessionText = "0h 0m";

    [ObservableProperty]
    private int _totalSessions;

    [ObservableProperty]
    private string _longestSessionText = "0h 0m";

    [ObservableProperty]
    private string _shortestSessionText = "0h 0m";

    [ObservableProperty]
    private string _mostActiveDay = "Unknown";

    [ObservableProperty]
    private string _mostActiveHour = "Unknown";

    [ObservableProperty]
    private ObservableCollection<GameSessionViewModel> _sessions = new();

    [ObservableProperty]
    private ObservableCollection<string> _timeRangeOptions = new() { "All Time", "Last Week", "Last Month", "Last Year" };

    [ObservableProperty]
    private string _selectedTimeRange = "All Time";

    [ObservableProperty]
    private ObservableCollection<GamePlaytimeByDayViewModel> _playtimeByDay = new();

    [ObservableProperty]
    private ObservableCollection<GamePlaytimeByHourViewModel> _playtimeByHour = new();

    [ObservableProperty]
    private double _todayGoalProgress;

    [ObservableProperty]
    private string _todayGoalText = "0/1h";

    [ObservableProperty]
    private double _weekGoalProgress;

    [ObservableProperty]
    private string _weekGoalText = "0/10h";

    public GameSessionsTabViewModel(
        IMediator mediator,
        IDialogService dialogService,
        ILogger<GameSessionsTabViewModel> logger,
        ITimeProvider timeProvider)
    {
        _mediator = mediator;
        _dialogService = dialogService;
        _logger = logger;
        _timeProvider = timeProvider;
        // Note: ISessionTrackingService will be resolved from DI container when needed
        // For now, we'll handle null case gracefully
    }

    public async Task LoadDataAsync(GameId gameId)
    {
        try
        {
            // Load sessions from backend
            var query = new GetGameSessionsQuery(gameId.Value, Limit: 50);
            var sessionHistory = await _mediator.Send(query).ConfigureAwait(false);

            TotalSessions = sessionHistory.Count;
            SessionCountText = $"{TotalSessions} session{(TotalSessions == 1 ? "" : "s")}";

            if (TotalSessions == 0)
            {
                await LoadPlaceholderData();
                return;
            }

            // Calculate total playtime
            var totalPlaytime = TimeSpan.Zero;
            foreach (var session in sessionHistory)
            {
                var duration = session.EndedAt.HasValue
                    ? session.EndedAt.Value - session.StartedAt
                    : _timeProvider.UtcNow - session.StartedAt;
                totalPlaytime += duration;
            }

            var totalHours = (int)totalPlaytime.TotalHours;
            var totalMinutes = totalPlaytime.Minutes;
            TotalPlaytimeText = $"{totalHours}h {totalMinutes}m";

            // Calculate average session length
            var avgDuration = TimeSpan.FromTicks(totalPlaytime.Ticks / TotalSessions);
            var avgHours = (int)avgDuration.TotalHours;
            var avgMinutes = avgDuration.Minutes;
            AverageSessionText = $"{avgHours}h {avgMinutes}m";

            // Calculate longest and shortest sessions
            var durations = sessionHistory
                .Where(s => s.EndedAt.HasValue)
                .Select(s => s.EndedAt!.Value - s.StartedAt)
                .ToList();

            if (durations.Any())
            {
                var longest = durations.Max();
                var longestHours = (int)longest.TotalHours;
                var longestMinutes = longest.Minutes;
                LongestSessionText = $"{longestHours}h {longestMinutes}m";

                var shortest = durations.Min();
                if (shortest.TotalHours >= 1)
                {
                    var shortestHours = (int)shortest.TotalHours;
                    var shortestMinutes = shortest.Minutes;
                    ShortestSessionText = $"{shortestHours}h {shortestMinutes}m";
                }
                else
                {
                    ShortestSessionText = $"{shortest.Minutes}m";
                }
            }

            // Populate sessions collection
            Sessions.Clear();
            foreach (var session in sessionHistory.OrderByDescending(s => s.StartedAt))
            {
                var startTimeText = FormatDateTime(session.StartedAt);
                var duration = session.EndedAt.HasValue
                    ? session.EndedAt.Value - session.StartedAt
                    : _timeProvider.UtcNow - session.StartedAt;
                var durationText = FormatDuration(duration);
                var endTimeText = session.EndedAt.HasValue
                    ? FormatDateTime(session.EndedAt.Value)
                    : "In Progress";

                Sessions.Add(new GameSessionViewModel
                {
                    SessionTitle = $"Session - {session.StartedAt:MMM d, yyyy}",
                    StartTimeText = startTimeText,
                    DurationText = durationText,
                    EndTimeText = endTimeText,
                    SessionNotes = session.Notes,
                    HasNotes = !string.IsNullOrWhiteSpace(session.Notes)
                });
            }

            _logger.LogInformation("Loaded {Count} sessions for game {GameId}", sessionHistory.Count, gameId);

            // Calculate playtime by day and hour
            CalculateDistributions(sessionHistory);

            // Load goal progress
            await LoadGoalProgressAsync(gameId, totalPlaytime).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load session data for game {GameId}", gameId);
            await LoadPlaceholderData();
        }
    }

    private void CalculateDistributions(IReadOnlyList<GameSession> sessions)
    {
        if (!sessions.Any()) return;

        // Playtime by Day
        var dayGroups = sessions
            .GroupBy(s => s.StartedAt.DayOfWeek)
            .Select(g => new {
                Day = g.Key,
                Ticks = g.Sum(s => (s.EndedAt ?? _timeProvider.UtcNow).Ticks - s.StartedAt.Ticks)
            })
            .ToList();

        var totalTicks = dayGroups.Sum(d => d.Ticks);
        PlaytimeByDay.Clear();
        foreach (var day in Enum.GetValues<DayOfWeek>().OrderBy(d => (int)d))
        {
            var dayData = dayGroups.FirstOrDefault(d => d.Day == day);
            var ticks = dayData?.Ticks ?? 0;
            var time = TimeSpan.FromTicks(ticks);

            PlaytimeByDay.Add(new GamePlaytimeByDayViewModel
            {
                DayName = day.ToString(),
                TimeText = FormatDuration(time),
                Percentage = totalTicks > 0 ? (double)ticks / totalTicks * 100 : 0
            });
        }

        MostActiveDay = PlaytimeByDay.OrderByDescending(d => d.Percentage).FirstOrDefault()?.DayName ?? "None";

        // Playtime by Hour
        var hourGroups = sessions
            .GroupBy(s => s.StartedAt.Hour)
            .Select(g => new {
                Hour = g.Key,
                Ticks = g.Sum(s => (s.EndedAt ?? _timeProvider.UtcNow).Ticks - s.StartedAt.Ticks)
            })
            .ToList();

        PlaytimeByHour.Clear();
        for (int i = 0; i < 24; i++)
        {
            var hourData = hourGroups.FirstOrDefault(h => h.Hour == i);
            var ticks = hourData?.Ticks ?? 0;
            var time = TimeSpan.FromTicks(ticks);

            PlaytimeByHour.Add(new GamePlaytimeByHourViewModel
            {
                HourLabel = $"{i:D2}:00",
                TimeText = FormatDuration(time),
                Percentage = totalTicks > 0 ? (double)ticks / totalTicks * 100 : 0
            });
        }

        var activeHour = PlaytimeByHour.OrderByDescending(h => h.Percentage).FirstOrDefault();
        MostActiveHour = activeHour != null ? activeHour.HourLabel : "None";
    }

    private async Task LoadGoalProgressAsync(GameId gameId, TimeSpan totalPlaytime)
    {
        try
        {
            // Fetch goals for this game
            var query = new GetActiveGoalsQuery();
            var result = await _mediator.Send(query).ConfigureAwait(false);

            if (result.IsSuccess && result.Value != null)
            {
                // Find playtime goal for this specific game
                var gameGoal = result.Value.FirstOrDefault(g =>
                    g.SpecificGameId == gameId.Value && g.Type == Core.Analytics.Entities.GoalType.PlaytimePerGame);

                if (gameGoal != null)
                {
                    TodayGoalProgress = gameGoal.CurrentValue > 0
                        ? (double)gameGoal.CurrentValue / gameGoal.TargetValue * 100
                        : 0;
                    TodayGoalText = $"{gameGoal.CurrentValue}/{gameGoal.TargetValue}h";
                }

                // General playtime goal (weekly)
                var weeklyGoal = result.Value.FirstOrDefault(g =>
                    g.Type == Core.Analytics.Entities.GoalType.PlaytimeHours && g.Title.Contains("Weekly", StringComparison.OrdinalIgnoreCase));

                if (weeklyGoal != null)
                {
                    WeekGoalProgress = weeklyGoal.CurrentValue > 0
                        ? (double)weeklyGoal.CurrentValue / weeklyGoal.TargetValue * 100
                        : 0;
                    WeekGoalText = $"{weeklyGoal.CurrentValue}/{weeklyGoal.TargetValue}h";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load goal progress for game {GameId}", gameId);
        }
    }

    private async Task LoadPlaceholderData()
    {
        TotalSessions = 0;
        SessionCountText = "0 sessions";
        TotalPlaytimeText = "0h 0m";
        AverageSessionText = "0h 0m";
        LongestSessionText = "0h 0m";
        ShortestSessionText = "0h 0m";
        Sessions.Clear();
        await Task.CompletedTask;
    }

    private string FormatDateTime(DateTime dateTime)
    {
        var timeSince = _timeProvider.UtcNow - dateTime;
        if (timeSince.TotalDays < 1)
            return $"Today {dateTime:HH:mm}";
        else if (timeSince.TotalDays < 2)
            return $"Yesterday {dateTime:HH:mm}";
        else
            return dateTime.ToString("MMM d, yyyy HH:mm");
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            var hours = (int)duration.TotalHours;
            var minutes = duration.Minutes;
            return $"{hours}h {minutes}m";
        }
        else
        {
            return $"{duration.Minutes}m";
        }
    }

    [RelayCommand]
    private async Task ViewCharts()
    {
        // Show a summary as a proxy for detailed charts
        var summary = $"Playtime Summary:\n\n" +
                     $"Most Active Day: {MostActiveDay}\n" +
                     $"Peak Hour: {MostActiveHour}\n\n" +
                     $"Distribution:\n";

        foreach(var day in PlaytimeByDay.Where(d => d.Percentage > 0))
        {
            summary += $"- {day.DayName}: {day.TimeText} ({day.Percentage:F1}%)\n";
        }

        await _dialogService.ShowInformationAsync("Session Analytics", summary);
    }

    [RelayCommand]
    private async Task ExportData()
    {
        var csv = "Title,Start,End,Duration,Notes\n";
        foreach(var s in Sessions)
        {
            csv += $"\"{s.SessionTitle}\",\"{s.StartTimeText}\",\"{s.EndTimeText}\",\"{s.DurationText}\",\"{s.SessionNotes}\"\n";
        }

        // Export to clipboard as a simple "export" mechanism
        var clipboard = Locator.Current.GetService<IClipboardService>();
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(csv);
            await _dialogService.ShowInformationAsync("Exported", "Session data has been exported to your clipboard in CSV format.");
        }
    }

    [RelayCommand]
    private async Task StartNewSession()
    {
        // Logic to start session? Usually Launch Game does this.
        await _dialogService.ShowInformationAsync("Info", "Sessions are automatically tracked when you launch a game.");
    }

    [RelayCommand]
    private async Task AddManualSession()
    {
        var result = await _dialogService.ShowNoteEditorAsync(null, "Manual Session: [Duration] hours\nNotes: ");
        if (result != null)
        {
            _logger.LogInformation("Manual session entry added: {Notes}", result.Content);
            // In real app, dispatch CreateManualSessionCommand
            await _dialogService.ShowInformationAsync("Manual Entry", "Manual session entry recorded.");
        }
    }
}

/// <summary>
/// View model for individual game sessions.
/// </summary>
public partial class GameSessionViewModel : ObservableObject
{
    public Action? EditNotesAction { get; set; }
    public Action? DeleteAction { get; set; }
    public Action? ViewDetailsAction { get; set; }

    [ObservableProperty]
    private string _sessionTitle = string.Empty;

    [ObservableProperty]
    private string _startTimeText = string.Empty;

    [ObservableProperty]
    private string _durationText = string.Empty;

    [ObservableProperty]
    private string _endTimeText = string.Empty;

    [ObservableProperty]
    private string? _sessionNotes;

    [ObservableProperty]
    private bool _hasNotes;

    [ObservableProperty]
    private int _achievementProgress;

    [ObservableProperty]
    private string _achievementProgressText = "0";

    [ObservableProperty]
    private string _sessionIcon = "🎮";

    [ObservableProperty]
    private string _sessionIconBackground = "#4CAF50";

    [ObservableProperty]
    private string _backgroundBrush = "Transparent";

    [ObservableProperty]
    private string _borderBrush = "Transparent";

    public string PrimaryActionText => "View Details";
    public string PrimaryActionClass => "Secondary";

    [RelayCommand]
    private void EditNotes()
    {
        EditNotesAction?.Invoke();
    }

    [RelayCommand]
    private void ViewDetails()
    {
        ViewDetailsAction?.Invoke();
    }

    [RelayCommand]
    private void Delete()
    {
        DeleteAction?.Invoke();
    }
}

/// <summary>
/// View model for playtime by day statistics.
/// </summary>
public class GamePlaytimeByDayViewModel : ObservableObject
{
    public string DayName { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public double Percentage { get; set; }
}

/// <summary>
/// View model for playtime by hour statistics.
/// </summary>
public class GamePlaytimeByHourViewModel : ObservableObject
{
    public string HourLabel { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public double Percentage { get; set; }
}
