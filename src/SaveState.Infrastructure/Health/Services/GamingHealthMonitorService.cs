using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Health.Models;
using SaveState.Core.Health.Services;
using System.Collections.Concurrent;

namespace SaveState.Infrastructure.Health.Services;

/// <summary>
/// Implementation of the gaming health monitor service.
/// Tracks posture, eye strain, break reminders, and overall health scores during gaming sessions.
/// </summary>
public class GamingHealthMonitorService : IGamingHealthMonitorService
{
    private readonly ILogger<GamingHealthMonitorService> _logger;
    private readonly ITimeProvider _timeProvider;

    private HealthMonitoringConfig _config = HealthMonitoringConfig.Default;
    private readonly List<HealthAlertRule> _alertRules = new();
    private readonly ConcurrentDictionary<string, HealthAlert> _activeAlerts = new();
    private readonly List<PostureReading> _postureHistory = new();
    private readonly object _lockObject = new();

    private DateTime _sessionStartTime;
    private DateTime _lastEyeBreakTime;
    private DateTime _lastBreakTime;
    private int _breakReminderCount;
    private int _breaksTaken;
    private int _eyeBreaksTaken;
    private Guid _currentSessionId;
    private System.Timers.Timer? _monitoringTimer;

    // Current status tracking
    private PostureStatus _currentPosture = PostureStatus.Good;
    private EyeStrainLevel _currentEyeStrain = EyeStrainLevel.None;
    private int? _currentHeartRate;
    private FocusLevel _currentFocus = FocusLevel.Medium;
    private StressLevel _currentStress = StressLevel.Normal;
    private FatigueLevel _currentFatigue = FatigueLevel.Rested;
    private float _postureConfidence = 1.0f;

    /// <inheritdoc />
    public bool IsMonitoring { get; private set; }

    /// <inheritdoc />
    public HealthMonitoringConfig CurrentConfig => _config;

    /// <inheritdoc />
    public event EventHandler<HealthStatusChangedEventArgs>? StatusChanged;

    /// <inheritdoc />
    public event EventHandler<HealthAlertEventArgs>? AlertTriggered;

    /// <inheritdoc />
    public event EventHandler<BreakReminderEventArgs>? BreakReminder;

    /// <summary>
    /// Initializes a new instance of the <see cref="GamingHealthMonitorService"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic information.</param>
    /// <param name="timeProvider">The time provider for time-related operations.</param>
    public GamingHealthMonitorService(
        ILogger<GamingHealthMonitorService> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;

        // Initialize with default alert rules
        _alertRules.AddRange(HealthAlertRule.DefaultRules);
    }

    /// <inheritdoc />
    public Task<Result> StartMonitoringAsync(HealthMonitoringConfig config, CancellationToken ct = default)
    {
        try
        {
            if (IsMonitoring)
            {
                _logger.LogWarning("Health monitoring is already active");
                return Task.FromResult(Result.Success());
            }

            lock (_lockObject)
            {
                _config = config;
                _currentSessionId = Guid.NewGuid();
                _sessionStartTime = _timeProvider.UtcNow;
                _lastEyeBreakTime = _sessionStartTime;
                _lastBreakTime = _sessionStartTime;
                _breakReminderCount = 0;
                _breaksTaken = 0;
                _eyeBreaksTaken = 0;
                _activeAlerts.Clear();
                _postureHistory.Clear();

                // Start the monitoring timer
                _monitoringTimer = new System.Timers.Timer(config.AlertInterval.TotalMilliseconds);
                _monitoringTimer.Elapsed += OnMonitoringTimerElapsed;
                _monitoringTimer.AutoReset = true;
                _monitoringTimer.Start();

                IsMonitoring = true;
            }

            _logger.LogInformation(
                "Started health monitoring for session {SessionId} with break interval {BreakInterval} minutes",
                _currentSessionId,
                config.BreakInterval.TotalMinutes);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start health monitoring");
            return Task.FromResult(Result.Failure($"Failed to start monitoring: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result> StopMonitoringAsync(CancellationToken ct = default)
    {
        try
        {
            if (!IsMonitoring)
            {
                return Task.FromResult(Result.Success());
            }

            lock (_lockObject)
            {
                _monitoringTimer?.Stop();
                _monitoringTimer?.Dispose();
                _monitoringTimer = null;
                IsMonitoring = false;
            }

            _logger.LogInformation(
                "Stopped health monitoring for session {SessionId}. Duration: {Duration}, Breaks taken: {Breaks}",
                _currentSessionId,
                _timeProvider.UtcNow - _sessionStartTime,
                _breaksTaken);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop health monitoring");
            return Task.FromResult(Result.Failure($"Failed to stop monitoring: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result<HealthSnapshot>> GetCurrentStatusAsync(CancellationToken ct = default)
    {
        try
        {
            var snapshot = CreateHealthSnapshot();
            return Task.FromResult(Result.Success(snapshot));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current health status");
            return Task.FromResult(Result.Failure<HealthSnapshot>($"Failed to get status: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result<HealthReport>> GenerateSessionReportAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            if (sessionId != _currentSessionId && IsMonitoring)
            {
                return Task.FromResult(Result.Failure<HealthReport>("Session ID does not match the current session"));
            }

            var endTime = _timeProvider.UtcNow;
            var totalDuration = endTime - _sessionStartTime;

            // Calculate good posture percentage
            var goodPostureCount = _postureHistory.Count(p => p.Status is PostureStatus.Excellent or PostureStatus.Good);
            var goodPosturePercentage = _postureHistory.Count > 0
                ? (float)goodPostureCount / _postureHistory.Count * 100
                : 100f;

            // Calculate average health score
            var avgHealthScore = CalculateHealthScoreInternal();

            var report = new HealthReport
            {
                SessionId = sessionId,
                StartTime = _sessionStartTime,
                EndTime = endTime,
                TotalDuration = totalDuration,
                BreaksTaken = _breaksTaken,
                AverageHealthScore = avgHealthScore,
                Alerts = _activeAlerts.Values.ToList().AsReadOnly(),
                PostureHistory = _postureHistory.AsReadOnly(),
                GoodPosturePercentage = goodPosturePercentage,
                TotalScreenTime = totalDuration,
                EyeBreaksTaken = _eyeBreaksTaken
            };

            return Task.FromResult(Result.Success(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate session report for {SessionId}", sessionId);
            return Task.FromResult(Result.Failure<HealthReport>($"Failed to generate report: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result> ConfigureAlertsAsync(IReadOnlyList<HealthAlertRule> rules, CancellationToken ct = default)
    {
        try
        {
            lock (_lockObject)
            {
                _alertRules.Clear();
                _alertRules.AddRange(rules);
            }

            _logger.LogInformation("Configured {Count} health alert rules", rules.Count);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure alert rules");
            return Task.FromResult(Result.Failure($"Failed to configure alerts: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result> AcknowledgeAlertAsync(string alertId, CancellationToken ct = default)
    {
        try
        {
            if (_activeAlerts.TryGetValue(alertId, out var alert))
            {
                var acknowledgedAlert = alert with { IsAcknowledged = true };
                _activeAlerts[alertId] = acknowledgedAlert;
                _logger.LogDebug("Alert {AlertId} acknowledged", alertId);
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acknowledge alert {AlertId}", alertId);
            return Task.FromResult(Result.Failure($"Failed to acknowledge alert: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<HealthAlert>>> GetActiveAlertsAsync(CancellationToken ct = default)
    {
        try
        {
            var activeAlerts = _activeAlerts.Values.Where(a => !a.IsAcknowledged).ToList();
            return Task.FromResult(Result.Success<IReadOnlyList<HealthAlert>>(activeAlerts));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active alerts");
            return Task.FromResult(Result.Failure<IReadOnlyList<HealthAlert>>($"Failed to get alerts: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result<float>> CalculateHealthScoreAsync(CancellationToken ct = default)
    {
        try
        {
            var score = CalculateHealthScoreInternal();
            return Task.FromResult(Result.Success(score));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate health score");
            return Task.FromResult(Result.Failure<float>($"Failed to calculate score: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result> RecordBreakTakenAsync(bool is20_20_20Rule = false, CancellationToken ct = default)
    {
        try
        {
            lock (_lockObject)
            {
                var now = _timeProvider.UtcNow;

                if (is20_20_20Rule)
                {
                    _lastEyeBreakTime = now;
                    _eyeBreaksTaken++;
                    _currentEyeStrain = EyeStrainLevel.None;
                    _logger.LogDebug("20-20-20 rule break recorded");
                }
                else
                {
                    _lastBreakTime = now;
                    _breaksTaken++;
                    _currentFatigue = FatigueLevel.Rested;
                    _logger.LogDebug("Break recorded: {BreakDuration} minutes", _config.BreakDurationMinutes);
                }
            }

            // Clear relevant alerts
            ClearAlert("break-overdue");
            if (is20_20_20Rule)
            {
                ClearAlert("eye-strain-high");
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record break");
            return Task.FromResult(Result.Failure($"Failed to record break: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result> UpdatePostureAsync(PostureStatus posture, float confidence, CancellationToken ct = default)
    {
        try
        {
            var previousStatus = _currentPosture;

            lock (_lockObject)
            {
                _currentPosture = posture;
                _postureConfidence = confidence;

                // Record the posture reading
                _postureHistory.Add(new PostureReading
                {
                    Timestamp = _timeProvider.UtcNow,
                    Status = posture,
                    Confidence = confidence
                });

                // Keep only the last 1000 readings to prevent memory bloat
                if (_postureHistory.Count > 1000)
                {
                    _postureHistory.RemoveAt(0);
                }
            }

            // Check for posture-related alerts
            if (posture == PostureStatus.Poor && previousStatus != PostureStatus.Poor)
            {
                TriggerAlert("posture-poor", "Your posture has deteriorated. Please sit up straight!", AlertSeverity.Warning);
            }

            // Raise status changed event
            if (previousStatus != posture)
            {
                var snapshot = CreateHealthSnapshot();
                StatusChanged?.Invoke(this, new HealthStatusChangedEventArgs
                {
                    PreviousStatus = CreateHealthSnapshot() with { Posture = previousStatus },
                    CurrentStatus = snapshot,
                    ChangedProperty = nameof(HealthSnapshot.Posture)
                });
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update posture");
            return Task.FromResult(Result.Failure($"Failed to update posture: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<PostureReading>>> GetPostureHistoryAsync(CancellationToken ct = default)
    {
        try
        {
            lock (_lockObject)
            {
                return Task.FromResult(Result.Success<IReadOnlyList<PostureReading>>(_postureHistory.AsReadOnly()));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get posture history");
            return Task.FromResult(Result.Failure<IReadOnlyList<PostureReading>>($"Failed to get history: {ex.Message}"));
        }
    }

    private void OnMonitoringTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            if (!IsMonitoring)
                return;

            var now = _timeProvider.UtcNow;
            var sessionDuration = now - _sessionStartTime;
            var timeSinceLastBreak = now - _lastBreakTime;
            var timeSinceLastEyeBreak = now - _lastEyeBreakTime;

            // Update eye strain based on time since last eye break (20-20-20 rule)
            UpdateEyeStrain(timeSinceLastEyeBreak);

            // Check for break reminders
            if (_config.EnableBreakReminders && timeSinceLastBreak >= _config.BreakInterval)
            {
                TriggerAlert("break-overdue", "You've been gaming for an hour. Time for a break!", AlertSeverity.Info);

                BreakReminder?.Invoke(this, new BreakReminderEventArgs
                {
                    SessionDuration = sessionDuration,
                    RecommendedBreakDuration = TimeSpan.FromMinutes(_config.BreakDurationMinutes),
                    Message = "You've been gaming for an hour. Consider taking a break to rest your eyes and stretch.",
                    Is20_20_20Rule = false
                });

                _breakReminderCount++;
            }

            // Check for 20-20-20 rule (every 20 minutes, look at something 20 feet away for 20 seconds)
            if (_config.EnableEyeStrainMonitoring && timeSinceLastEyeBreak >= TimeSpan.FromMinutes(20))
            {
                if (_currentEyeStrain < EyeStrainLevel.Moderate)
                {
                    TriggerAlert("eye-strain-moderate", "It's time for a 20-20-20 break! Look at something 20 feet away for 20 seconds.", AlertSeverity.Info);
                }

                BreakReminder?.Invoke(this, new BreakReminderEventArgs
                {
                    SessionDuration = sessionDuration,
                    RecommendedBreakDuration = TimeSpan.FromSeconds(20),
                    Message = "Time for the 20-20-20 rule! Look at something 20 feet away for 20 seconds.",
                    Is20_20_20Rule = true
                });
            }

            // Update fatigue based on session duration
            UpdateFatigue(sessionDuration);

            _logger.LogDebug("Health monitoring check completed. Session duration: {Duration}, Health score: {Score}",
                sessionDuration, CalculateHealthScoreInternal());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health monitoring timer elapsed");
        }
    }

    private void UpdateEyeStrain(TimeSpan timeSinceLastEyeBreak)
    {
        var previousStrain = _currentEyeStrain;

        _currentEyeStrain = timeSinceLastEyeBreak.TotalMinutes switch
        {
            < 20 => EyeStrainLevel.None,
            < 40 => EyeStrainLevel.Low,
            < 60 => EyeStrainLevel.Moderate,
            < 90 => EyeStrainLevel.High,
            _ => EyeStrainLevel.Critical
        };

        if (_currentEyeStrain == EyeStrainLevel.High && previousStrain < EyeStrainLevel.High)
        {
            TriggerAlert("eye-strain-high", "High eye strain detected. Please take a break using the 20-20-20 rule!", AlertSeverity.Warning);
        }

        if (previousStrain != _currentEyeStrain)
        {
            StatusChanged?.Invoke(this, new HealthStatusChangedEventArgs
            {
                PreviousStatus = CreateHealthSnapshot() with { EyeStrain = previousStrain },
                CurrentStatus = CreateHealthSnapshot(),
                ChangedProperty = nameof(HealthSnapshot.EyeStrain)
            });
        }
    }

    private void UpdateFatigue(TimeSpan sessionDuration)
    {
        var previousFatigue = _currentFatigue;

        _currentFatigue = sessionDuration.TotalHours switch
        {
            < 1 => FatigueLevel.Rested,
            < 2 => FatigueLevel.Alert,
            < 4 => FatigueLevel.Tired,
            _ => FatigueLevel.Exhausted
        };

        if (_currentFatigue == FatigueLevel.Exhausted && previousFatigue != FatigueLevel.Exhausted)
        {
            TriggerAlert("fatigue-exhausted", "You've been gaming for a long time. Consider taking a longer break!", AlertSeverity.Critical);
        }
    }

    private float CalculateHealthScoreInternal()
    {
        // Calculate score based on multiple factors (0-100)
        float score = 100f;

        // Posture penalty (0-30 points)
        score -= _currentPosture switch
        {
            PostureStatus.Excellent => 0f,
            PostureStatus.Good => 5f,
            PostureStatus.Slouching => 15f,
            PostureStatus.Poor => 25f,
            PostureStatus.Critical => 30f,
            _ => 0f
        };

        // Eye strain penalty (0-25 points)
        score -= _currentEyeStrain switch
        {
            EyeStrainLevel.None => 0f,
            EyeStrainLevel.Low => 5f,
            EyeStrainLevel.Moderate => 10f,
            EyeStrainLevel.High => 20f,
            EyeStrainLevel.Critical => 25f,
            _ => 0f
        };

        // Fatigue penalty (0-25 points)
        score -= _currentFatigue switch
        {
            FatigueLevel.Rested => 0f,
            FatigueLevel.Alert => 5f,
            FatigueLevel.Tired => 15f,
            FatigueLevel.Exhausted => 25f,
            _ => 0f
        };

        // Session duration penalty (0-20 points for very long sessions)
        var sessionDuration = _timeProvider.UtcNow - _sessionStartTime;
        if (sessionDuration.TotalHours > 4)
            score -= 20f;
        else if (sessionDuration.TotalHours > 3)
            score -= 10f;
        else if (sessionDuration.TotalHours > 2)
            score -= 5f;

        return Math.Max(0f, Math.Min(100f, score));
    }

    private HealthSnapshot CreateHealthSnapshot()
    {
        var now = _timeProvider.UtcNow;
        var sessionDuration = now - _sessionStartTime;

        return new HealthSnapshot
        {
            Posture = _currentPosture,
            EyeStrain = _currentEyeStrain,
            HeartRate = _currentHeartRate,
            SessionDuration = sessionDuration,
            BreakReminderCount = _breakReminderCount,
            HealthScore = CalculateHealthScoreInternal(),
            Focus = _currentFocus,
            Stress = _currentStress,
            Fatigue = _currentFatigue,
            Timestamp = now,
            TimeSinceLastEyeBreak = now - _lastEyeBreakTime,
            TimeUntilNextBreak = _config.BreakInterval - (now - _lastBreakTime)
        };
    }

    private void TriggerAlert(string type, string message, AlertSeverity severity)
    {
        // Check if this alert type already exists and is unacknowledged
        var existingAlert = _activeAlerts.Values.FirstOrDefault(a => a.Type == type && !a.IsAcknowledged);
        if (existingAlert != null)
        {
            return; // Don't duplicate unacknowledged alerts
        }

        var alert = HealthAlert.Create(type, message, severity);
        _activeAlerts[alert.Id] = alert;

        _logger.LogInformation("Health alert triggered: {Type} - {Message}", type, message);

        AlertTriggered?.Invoke(this, new HealthAlertEventArgs
        {
            Alert = alert,
            CurrentStatus = CreateHealthSnapshot()
        });
    }

    private void ClearAlert(string type)
    {
        var alertToRemove = _activeAlerts.Values.FirstOrDefault(a => a.Type == type);
        if (alertToRemove != null)
        {
            _activeAlerts.TryRemove(alertToRemove.Id, out _);
        }
    }
}
