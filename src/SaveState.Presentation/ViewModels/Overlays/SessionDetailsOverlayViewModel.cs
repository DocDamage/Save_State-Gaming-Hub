using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Overlays;

/// <summary>
/// ViewModel for the session details overlay.
/// </summary>
public partial class SessionDetailsOverlayViewModel : ObservableObject
{
    [ObservableProperty]
    private string _sessionSummary = "Viewing all gaming sessions";

    [ObservableProperty]
    private string _totalPlaytime = "0h 0m";

    [ObservableProperty]
    private int _sessionCount;

    [ObservableProperty]
    private string _averageSession = "0h 0m";

    [ObservableProperty]
    private string _lastPlayed = "Never";

    [ObservableProperty]
    private double _avgFps;

    [ObservableProperty]
    private double _avgCpu;

    [ObservableProperty]
    private double _avgGpu;

    [ObservableProperty]
    private ObservableCollection<SessionItemViewModel> _recentSessions = new();

    public SessionDetailsOverlayViewModel()
    {
        // Design-time data
        LoadDesignTimeData();
    }

    private void LoadDesignTimeData()
    {
        TotalPlaytime = "127h 34m";
        SessionCount = 45;
        AverageSession = "2h 50m";
        LastPlayed = "2 hours ago";
        AvgFps = 144.5;
        AvgCpu = 45.2;
        AvgGpu = 78.9;

        RecentSessions.Add(new SessionItemViewModel
        {
            Date = DateTime.Now.AddHours(-2),
            TimeRange = "6:30 PM - 9:15 PM",
            Platform = "Steam",
            Duration = "2h 45m",
            AvgFps = 144,
            Performance = "Excellent"
        });

        RecentSessions.Add(new SessionItemViewModel
        {
            Date = DateTime.Now.AddDays(-1),
            TimeRange = "8:00 PM - 10:30 PM",
            Platform = "Epic Games",
            Duration = "2h 30m",
            AvgFps = 120,
            Performance = "Very Good"
        });

        RecentSessions.Add(new SessionItemViewModel
        {
            Date = DateTime.Now.AddDays(-2),
            TimeRange = "7:15 PM - 11:45 PM",
            Platform = "Steam",
            Duration = "4h 30m",
            AvgFps = 60,
            Performance = "Good"
        });
    }

    [RelayCommand]
    private void Close()
    {
        // Close overlay
    }
}

/// <summary>
/// ViewModel for a single session item.
/// </summary>
public class SessionItemViewModel
{
    public DateTime Date { get; set; }
    public string TimeRange { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public int AvgFps { get; set; }
    public string Performance { get; set; } = string.Empty;
}
