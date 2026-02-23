using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Models.CloudGaming;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Overlays;

/// <summary>
/// ViewModel for the in-stream overlay.
/// </summary>
public partial class StreamOverlayViewModel : ObservableObject
{
    private readonly ILogger<StreamOverlayViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the StreamOverlayViewModel.
    /// </summary>
    public StreamOverlayViewModel(ILogger<StreamOverlayViewModel> logger)
    {
        _logger = logger;
        LatencyHistory = new ObservableCollection<float>();
        PerformanceHistory = new ObservableCollection<PerformanceDataPoint>();
    }

    #region Observable Properties

    /// <summary>
    /// Current streaming session.
    /// </summary>
    [ObservableProperty]
    private CloudSession? _session;

    /// <summary>
    /// Whether overlay is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isVisible = true;

    /// <summary>
    /// Current latency in milliseconds.
    /// </summary>
    [ObservableProperty]
    private float _currentLatency;

    /// <summary>
    /// Current frame rate.
    /// </summary>
    [ObservableProperty]
    private int _currentFps;

    /// <summary>
    /// Current bitrate in Mbps.
    /// </summary>
    [ObservableProperty]
    private float _currentBitrate;

    /// <summary>
    /// Current packet loss percentage.
    /// </summary>
    [ObservableProperty]
    private float _currentPacketLoss;

    /// <summary>
    /// Resolution display string.
    /// </summary>
    [ObservableProperty]
    private string _resolution = "1920x1080";

    /// <summary>
    /// History of latency measurements for graph.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<float> _latencyHistory;

    /// <summary>
    /// Performance history data.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PerformanceDataPoint> _performanceHistory;

    /// <summary>
    /// Whether stats panel is expanded.
    /// </summary>
    [ObservableProperty]
    private bool _isStatsExpanded;

    /// <summary>
    /// Whether quick settings panel is visible.
    /// </summary>
    [ObservableProperty]
    private bool _showQuickSettings;

    /// <summary>
    /// Session duration formatted.
    /// </summary>
    public string SessionDuration
    {
        get
        {
            if (Session is null) return "00:00:00";
            var duration = DateTime.UtcNow - Session.StartedAt;
            return duration.ToString(@"hh\:mm\:ss");
        }
    }

    /// <summary>
    /// Connection quality indicator.
    /// </summary>
    public string ConnectionQuality
    {
        get
        {
            if (CurrentLatency <= 20) return "Excellent";
            if (CurrentLatency <= 40) return "Good";
            if (CurrentLatency <= 60) return "Fair";
            return "Poor";
        }
    }

    /// <summary>
    /// Quality color for UI.
    /// </summary>
    public string QualityColor => CurrentLatency switch
    {
        <= 20 => "#4CAF50", // Green
        <= 40 => "#8BC34A", // Light Green
        <= 60 => "#FFC107", // Yellow
        _ => "#F44336"      // Red
    };

    /// <summary>
    /// Whether the stream is experiencing issues.
    /// </summary>
    public bool HasIssues => CurrentLatency > 100 || CurrentPacketLoss > 1.0f || CurrentFps < 30;

    #endregion

    #region Commands

    /// <summary>
    /// Toggles overlay visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleVisibility()
    {
        IsVisible = !IsVisible;
        _logger.LogDebug("Overlay visibility: {Visible}", IsVisible);
    }

    /// <summary>
    /// Toggles stats panel expansion.
    /// </summary>
    [RelayCommand]
    private void ToggleStats()
    {
        IsStatsExpanded = !IsStatsExpanded;
    }

    /// <summary>
    /// Toggles quick settings panel.
    /// </summary>
    [RelayCommand]
    private void ToggleQuickSettings()
    {
        ShowQuickSettings = !ShowQuickSettings;
    }

    /// <summary>
    /// Changes stream quality.
    /// </summary>
    [RelayCommand]
    private async Task ChangeQualityAsync(SessionQuality quality)
    {
        if (Session is null) return;

        _logger.LogInformation("Changing stream quality to {Quality}", quality);

        // TODO: Send quality change request to provider
        Session.Quality = quality;

        await Task.Delay(100);
    }

    /// <summary>
    /// Toggles microphone.
    /// </summary>
    [RelayCommand]
    private void ToggleMicrophone()
    {
        // TODO: Toggle microphone in stream
        _logger.LogInformation("Microphone toggled");
    }

    /// <summary>
    /// Takes a screenshot of the stream.
    /// </summary>
    [RelayCommand]
    private async Task TakeScreenshotAsync()
    {
        _logger.LogInformation("Taking screenshot");

        // TODO: Capture screenshot
        await Task.Delay(100);
    }

    /// <summary>
    /// Opens the main menu overlay.
    /// </summary>
    [RelayCommand]
    private void OpenMenu()
    {
        _logger.LogInformation("Opening stream menu");
        // TODO: Show main menu overlay
    }

    /// <summary>
    /// Disconnection from stream.
    /// </summary>
    [RelayCommand]
    private async Task DisconnectAsync()
    {
        _logger.LogInformation("Disconnecting from stream");

        // TODO: Signal disconnect
        await Task.Delay(100);
    }

    #endregion

    /// <summary>
    /// Updates performance metrics.
    /// </summary>
    public void UpdateMetrics(float latency, int fps, float bitrate, float packetLoss)
    {
        CurrentLatency = latency;
        CurrentFps = fps;
        CurrentBitrate = bitrate;
        CurrentPacketLoss = packetLoss;

        // Add to history (keep last 60 points)
        LatencyHistory.Add(latency);
        if (LatencyHistory.Count > 60) LatencyHistory.RemoveAt(0);

        PerformanceHistory.Add(new PerformanceDataPoint
        {
            Timestamp = DateTime.UtcNow,
            Latency = latency,
            Fps = fps,
            Bitrate = bitrate
        });
        if (PerformanceHistory.Count > 60) PerformanceHistory.RemoveAt(0);

        OnPropertyChanged(nameof(SessionDuration));
        OnPropertyChanged(nameof(ConnectionQuality));
        OnPropertyChanged(nameof(QualityColor));
        OnPropertyChanged(nameof(HasIssues));
    }

    /// <summary>
    /// Initializes the overlay for a session.
    /// </summary>
    public void Initialize(CloudSession session)
    {
        Session = session;
        Resolution = $"{session.ResolutionWidth}x{session.ResolutionHeight}";

        // Start performance monitoring
        _ = StartPerformanceMonitoringAsync();
    }

    private async Task StartPerformanceMonitoringAsync()
    {
        while (Session?.IsActive == true)
        {
            // Simulate metric updates (in real implementation, get from provider SDK)
            UpdateMetrics(
                Random.Shared.Next(10, 50),
                Random.Shared.Next(55, 65),
                35 + Random.Shared.NextSingle() * 20,
                Random.Shared.NextSingle() * 0.5f
            );

            await Task.Delay(1000);
        }
    }

    partial void OnCurrentLatencyChanged(float value)
    {
        OnPropertyChanged(nameof(ConnectionQuality));
        OnPropertyChanged(nameof(QualityColor));
        OnPropertyChanged(nameof(HasIssues));
    }

    partial void OnCurrentPacketLossChanged(float value)
    {
        OnPropertyChanged(nameof(HasIssues));
    }

    partial void OnCurrentFpsChanged(int value)
    {
        OnPropertyChanged(nameof(HasIssues));
    }
}

/// <summary>
/// Data point for performance history.
/// </summary>
public class PerformanceDataPoint
{
    /// <summary>Timestamp of measurement.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Latency in ms.</summary>
    public float Latency { get; set; }

    /// <summary>FPS at this time.</summary>
    public int Fps { get; set; }

    /// <summary>Bitrate in Mbps.</summary>
    public float Bitrate { get; set; }
}
