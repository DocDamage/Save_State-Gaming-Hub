using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common.Services;
using SaveState.Core.Health.Models;
using SaveState.Core.Health.Services;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.Timers;

namespace SaveState.Presentation.ViewModels.Health;

/// <summary>
/// ViewModel for the health monitor view.
/// Displays health metrics, posture status, eye strain, and break reminders.
/// </summary>
public partial class HealthMonitorViewModel : ObservableObject, IDisposable
{
    private readonly IGamingHealthMonitorService _healthMonitorService;
    private readonly IOverlayService _overlayService;
    private readonly ITimeProvider _timeProvider;

    private System.Timers.Timer? _updateTimer;
    private HealthSnapshot? _currentSnapshot;

    // Observable collections
    public ObservableCollection<HealthAlert> ActiveAlerts { get; } = new();
    public ObservableCollection<PostureReading> PostureHistory { get; } = new();

    // Configuration
    [ObservableProperty]
    private bool _enablePostureDetection = true;

    [ObservableProperty]
    private bool _enableEyeStrainMonitoring = true;

    [ObservableProperty]
    private bool _enableHeartRateMonitoring;

    [ObservableProperty]
    private bool _enableErgonomicWarnings = true;

    [ObservableProperty]
    private bool _enableBreakReminders = true;

    [ObservableProperty]
    private int _breakIntervalMinutes = 60;

    [ObservableProperty]
    private int _breakDurationMinutes = 5;

    // Status properties
    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private float _healthScore;

    [ObservableProperty]
    private PostureStatus _postureStatus = PostureStatus.Good;

    [ObservableProperty]
    private EyeStrainLevel _eyeStrainLevel = EyeStrainLevel.None;

    [ObservableProperty]
    private FocusLevel _focusLevel = FocusLevel.Medium;

    [ObservableProperty]
    private StressLevel _stressLevel = StressLevel.Normal;

    [ObservableProperty]
    private FatigueLevel _fatigueLevel = FatigueLevel.Rested;

    [ObservableProperty]
    private int? _heartRate;

    [ObservableProperty]
    private TimeSpan _sessionDuration = TimeSpan.Zero;

    [ObservableProperty]
    private TimeSpan _timeUntilNextBreak = TimeSpan.FromHours(1);

    [ObservableProperty]
    private TimeSpan _timeSinceLastEyeBreak = TimeSpan.Zero;

    [ObservableProperty]
    private int _breakReminderCount;

    [ObservableProperty]
    private bool _hasActiveAlerts;

    [ObservableProperty]
    private string _statusMessage = "Health monitoring is not active.";

    [ObservableProperty]
    private bool _showSettings;

    /// <summary>
    /// Gets the health score as a percentage string.
    /// </summary>
    public string HealthScoreDisplay => $"{HealthScore:F0}%";

    /// <summary>
    /// Gets the health score color based on the score value.
    /// </summary>
    public string HealthScoreColor => HealthScore switch
    {
        >= 80 => "#4CAF50",  // Green
        >= 60 => "#FFC107",  // Yellow
        >= 40 => "#FF9800",  // Orange
        _ => "#F44336"       // Red
    };

    /// <summary>
    /// Gets the posture status display text.
    /// </summary>
    public string PostureStatusDisplay => PostureStatus switch
    {
        PostureStatus.Excellent => "Excellent Posture",
        PostureStatus.Good => "Good Posture",
        PostureStatus.Slouching => "Slouching Detected",
        PostureStatus.Poor => "Poor Posture",
        PostureStatus.Critical => "Critical Posture",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the posture status color.
    /// </summary>
    public string PostureStatusColor => PostureStatus switch
    {
        PostureStatus.Excellent => "#4CAF50",
        PostureStatus.Good => "#8BC34A",
        PostureStatus.Slouching => "#FFC107",
        PostureStatus.Poor => "#FF9800",
        PostureStatus.Critical => "#F44336",
        _ => "#9E9E9E"
    };

    /// <summary>
    /// Gets the eye strain level display text.
    /// </summary>
    public string EyeStrainDisplay => EyeStrainLevel switch
    {
        EyeStrainLevel.None => "No Eye Strain",
        EyeStrainLevel.Low => "Low Eye Strain",
        EyeStrainLevel.Moderate => "Moderate Eye Strain",
        EyeStrainLevel.High => "High Eye Strain",
        EyeStrainLevel.Critical => "Critical Eye Strain",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the eye strain level color.
    /// </summary>
    public string EyeStrainColor => EyeStrainLevel switch
    {
        EyeStrainLevel.None => "#4CAF50",
        EyeStrainLevel.Low => "#8BC34A",
        EyeStrainLevel.Moderate => "#FFC107",
        EyeStrainLevel.High => "#FF9800",
        EyeStrainLevel.Critical => "#F44336",
        _ => "#9E9E9E"
    };

    /// <summary>
    /// Gets the fatigue level display text.
    /// </summary>
    public string FatigueDisplay => FatigueLevel switch
    {
        FatigueLevel.Rested => "Well Rested",
        FatigueLevel.Alert => "Alert",
        FatigueLevel.Tired => "Tired",
        FatigueLevel.Exhausted => "Exhausted",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the fatigue level color.
    /// </summary>
    public string FatigueColor => FatigueLevel switch
    {
        FatigueLevel.Rested => "#4CAF50",
        FatigueLevel.Alert => "#8BC34A",
        FatigueLevel.Tired => "#FFC107",
        FatigueLevel.Exhausted => "#F44336",
        _ => "#9E9E9E"
    };

    /// <summary>
    /// Gets the formatted session duration.
    /// </summary>
    public string SessionDurationDisplay => $"{SessionDuration.Hours:D2}:{SessionDuration.Minutes:D2}:{SessionDuration.Seconds:D2}";

    /// <summary>
    /// Gets the formatted break timer.
    /// </summary>
    public string BreakTimerDisplay => TimeUntilNextBreak.TotalSeconds > 0
        ? $"{TimeUntilNextBreak.Minutes:D2}:{TimeUntilNextBreak.Seconds:D2}"
        : "00:00";

    /// <summary>
    /// Gets whether a break is overdue.
    /// </summary>
    public bool IsBreakOverdue => TimeUntilNextBreak <= TimeSpan.Zero;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthMonitorViewModel"/> class.
    /// </summary>
    public HealthMonitorViewModel(
        IGamingHealthMonitorService healthMonitorService,
        IOverlayService overlayService,
        ITimeProvider timeProvider)
    {
        _healthMonitorService = healthMonitorService;
        _overlayService = overlayService;
        _timeProvider = timeProvider;

        // Subscribe to service events
        _healthMonitorService.StatusChanged += OnHealthStatusChanged;
        _healthMonitorService.AlertTriggered += OnHealthAlertTriggered;
        _healthMonitorService.BreakReminder += OnBreakReminder;

        // Start update timer
        _updateTimer = new System.Timers.Timer(1000);
        _updateTimer.Elapsed += OnUpdateTimerElapsed;
        _updateTimer.AutoReset = true;
        _updateTimer.Start();

        // Load initial configuration
        LoadConfiguration();
    }

    /// <summary>
    /// Starts health monitoring with the current configuration.
    /// </summary>
    [RelayCommand]
    private async Task StartMonitoringAsync()
    {
        var config = new HealthMonitoringConfig
        {
            EnablePostureDetection = EnablePostureDetection,
            EnableEyeStrainMonitoring = EnableEyeStrainMonitoring,
            EnableHeartRateMonitoring = EnableHeartRateMonitoring,
            EnableErgonomicWarnings = EnableErgonomicWarnings,
            EnableBreakReminders = EnableBreakReminders,
            AlertInterval = TimeSpan.FromMinutes(5),
            BreakInterval = TimeSpan.FromMinutes(BreakIntervalMinutes),
            BreakDurationMinutes = BreakDurationMinutes
        };

        var result = await _healthMonitorService.StartMonitoringAsync(config);

        if (result.IsSuccess)
        {
            IsMonitoring = true;
            StatusMessage = "Health monitoring is active.";

            // Update status immediately
            await RefreshStatusAsync();
        }
        else
        {
            StatusMessage = $"Failed to start monitoring: {result.Error}";
        }
    }

    /// <summary>
    /// Stops health monitoring.
    /// </summary>
    [RelayCommand]
    private async Task StopMonitoringAsync()
    {
        var result = await _healthMonitorService.StopMonitoringAsync();

        if (result.IsSuccess)
        {
            IsMonitoring = false;
            StatusMessage = "Health monitoring stopped.";
            SessionDuration = TimeSpan.Zero;
        }
        else
        {
            StatusMessage = $"Failed to stop monitoring: {result.Error}";
        }
    }

    /// <summary>
    /// Records that the user has taken a break.
    /// </summary>
    [RelayCommand]
    private async Task TakeBreakAsync()
    {
        await _healthMonitorService.RecordBreakTakenAsync(false);
        StatusMessage = "Break recorded. Great job taking care of yourself!";
    }

    /// <summary>
    /// Records that the user has taken a 20-20-20 rule break.
    /// </summary>
    [RelayCommand]
    private async Task TakeEyeBreakAsync()
    {
        await _healthMonitorService.RecordBreakTakenAsync(true);
        StatusMessage = "20-20-20 break recorded. Your eyes will thank you!";
    }

    /// <summary>
    /// Acknowledges a health alert.
    /// </summary>
    [RelayCommand]
    private async Task AcknowledgeAlertAsync(string alertId)
    {
        await _healthMonitorService.AcknowledgeAlertAsync(alertId);

        // Remove from UI
        var alert = ActiveAlerts.FirstOrDefault(a => a.Id == alertId);
        if (alert != null)
        {
            ActiveAlerts.Remove(alert);
            HasActiveAlerts = ActiveAlerts.Count > 0;
        }
    }

    /// <summary>
    /// Refreshes the current health status.
    /// </summary>
    [RelayCommand]
    private async Task RefreshStatusAsync()
    {
        var statusResult = await _healthMonitorService.GetCurrentStatusAsync();
        if (statusResult.IsSuccess)
        {
            UpdateStatusDisplay(statusResult.Value);
        }

        var alertsResult = await _healthMonitorService.GetActiveAlertsAsync();
        if (alertsResult.IsSuccess)
        {
            ActiveAlerts.Clear();
            foreach (var alert in alertsResult.Value)
            {
                ActiveAlerts.Add(alert);
            }
            HasActiveAlerts = ActiveAlerts.Count > 0;
        }
    }

    /// <summary>
    /// Toggles the settings panel visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleSettings()
    {
        ShowSettings = !ShowSettings;
    }

    /// <summary>
    /// Closes the health monitor view.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        _overlayService?.HideAllOverlays();
    }

    /// <summary>
    /// Simulates a posture change for testing.
    /// </summary>
    [RelayCommand]
    private async Task SimulatePostureAsync(string posture)
    {
        var postureStatus = posture switch
        {
            "excellent" => PostureStatus.Excellent,
            "good" => PostureStatus.Good,
            "slouching" => PostureStatus.Slouching,
            "poor" => PostureStatus.Poor,
            "critical" => PostureStatus.Critical,
            _ => PostureStatus.Good
        };

        await _healthMonitorService.UpdatePostureAsync(postureStatus, 0.85f);
    }

    private void OnHealthStatusChanged(object? sender, HealthStatusChangedEventArgs e)
    {
        // Update UI on UI thread (Avalonia will handle this)
        UpdateStatusDisplay(e.CurrentStatus);
    }

    private void OnHealthAlertTriggered(object? sender, HealthAlertEventArgs e)
    {
        // Add alert to UI
        if (!ActiveAlerts.Any(a => a.Id == e.Alert.Id))
        {
            ActiveAlerts.Add(e.Alert);
            HasActiveAlerts = true;
        }
    }

    private void OnBreakReminder(object? sender, BreakReminderEventArgs e)
    {
        StatusMessage = e.Message;
    }

    private void OnUpdateTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // Update timer display
        if (IsMonitoring && _currentSnapshot != null)
        {
            // Calculate elapsed time since snapshot
            var elapsed = _timeProvider.UtcNow - _currentSnapshot.Timestamp;
            var currentSessionDuration = _currentSnapshot.SessionDuration + elapsed;

            // Update UI properties on the UI thread
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                SessionDuration = currentSessionDuration;

                if (_currentSnapshot.TimeUntilNextBreak > elapsed)
                {
                    TimeUntilNextBreak = _currentSnapshot.TimeUntilNextBreak - elapsed;
                }
                else
                {
                    TimeUntilNextBreak = TimeSpan.Zero;
                }

                TimeSinceLastEyeBreak = _currentSnapshot.TimeSinceLastEyeBreak + elapsed;
            });
        }
    }

    private void UpdateStatusDisplay(HealthSnapshot snapshot)
    {
        _currentSnapshot = snapshot;

        HealthScore = snapshot.HealthScore;
        PostureStatus = snapshot.Posture;
        EyeStrainLevel = snapshot.EyeStrain;
        FocusLevel = snapshot.Focus;
        StressLevel = snapshot.Stress;
        FatigueLevel = snapshot.Fatigue;
        HeartRate = snapshot.HeartRate;
        SessionDuration = snapshot.SessionDuration;
        TimeUntilNextBreak = snapshot.TimeUntilNextBreak;
        TimeSinceLastEyeBreak = snapshot.TimeSinceLastEyeBreak;
        BreakReminderCount = snapshot.BreakReminderCount;

        // Trigger property change notifications for computed properties
        OnPropertyChanged(nameof(HealthScoreDisplay));
        OnPropertyChanged(nameof(HealthScoreColor));
        OnPropertyChanged(nameof(PostureStatusDisplay));
        OnPropertyChanged(nameof(PostureStatusColor));
        OnPropertyChanged(nameof(EyeStrainDisplay));
        OnPropertyChanged(nameof(EyeStrainColor));
        OnPropertyChanged(nameof(FatigueDisplay));
        OnPropertyChanged(nameof(FatigueColor));
        OnPropertyChanged(nameof(SessionDurationDisplay));
        OnPropertyChanged(nameof(BreakTimerDisplay));
        OnPropertyChanged(nameof(IsBreakOverdue));
    }

    private void LoadConfiguration()
    {
        // Load from current config if monitoring is already active
        if (_healthMonitorService.IsMonitoring)
        {
            var config = _healthMonitorService.CurrentConfig;
            EnablePostureDetection = config.EnablePostureDetection;
            EnableEyeStrainMonitoring = config.EnableEyeStrainMonitoring;
            EnableHeartRateMonitoring = config.EnableHeartRateMonitoring;
            EnableErgonomicWarnings = config.EnableErgonomicWarnings;
            EnableBreakReminders = config.EnableBreakReminders;
            BreakIntervalMinutes = (int)config.BreakInterval.TotalMinutes;
            BreakDurationMinutes = config.BreakDurationMinutes;
            IsMonitoring = true;

            _ = RefreshStatusAsync();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
        _updateTimer = null;

        _healthMonitorService.StatusChanged -= OnHealthStatusChanged;
        _healthMonitorService.AlertTriggered -= OnHealthAlertTriggered;
        _healthMonitorService.BreakReminder -= OnBreakReminder;
    }
}
