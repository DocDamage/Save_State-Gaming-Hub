using SaveState.Core.Common;

namespace SaveState.Core.GamingHealth.Models;

/// <summary>
/// Represents a gaming health monitoring session.
/// </summary>
public record GamingHealthSession
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string UserId { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; init; }
    public IReadOnlyList<HealthMetric> Metrics { get; init; } = Array.Empty<HealthMetric>();
    public IReadOnlyList<HealthAlert> Alerts { get; init; } = Array.Empty<HealthAlert>();
    public SessionHealthSummary? Summary { get; init; }
}

/// <summary>
/// Represents a health metric reading.
/// </summary>
public record HealthMetric
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public HealthMetricType Type { get; init; }
    public double Value { get; init; }
    public string Unit { get; init; } = string.Empty;
    public HealthStatus Status { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Types of health metrics that can be monitored.
/// </summary>
public enum HealthMetricType
{
    PostureScore,
    EyeStrainLevel,
    BlinkRate,
    ScreenDistance,
    SessionDuration,
    BreakInterval,
    HeartRate,
    StressLevel,
    WristPosition,
    NeckAngle,
    BackAngle
}

/// <summary>
/// Health status levels.
/// </summary>
public enum HealthStatus
{
    Excellent,
    Good,
    Fair,
    Poor,
    Critical
}

/// <summary>
/// Represents a health alert.
/// </summary>
public record HealthAlert
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public AlertType Type { get; init; }
    public AlertSeverity Severity { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? RecommendedAction { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public bool IsAcknowledged { get; init; } = false;
}

/// <summary>
/// Types of health alerts.
/// </summary>
public enum AlertType
{
    PostureWarning,
    EyeStrainWarning,
    BreakReminder,
    HeartRateWarning,
    SessionLengthWarning,
    ScreenDistanceWarning,
    BlinkReminder,
    WristPositionWarning
}

/// <summary>
/// Severity levels for alerts.
/// </summary>
public enum AlertSeverity
{
    Info,
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Summary of a gaming session's health data.
/// </summary>
public record SessionHealthSummary
{
    public TimeSpan TotalDuration { get; init; }
    public TimeSpan BreakTime { get; init; }
    public int AlertCount { get; init; }
    public double AveragePostureScore { get; init; }
    public double AverageEyeStrainLevel { get; init; }
    public int AverageHeartRate { get; init; }
    public HealthStatus OverallStatus { get; init; }
    public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Represents posture detection data.
/// </summary>
public record PostureData
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public float NeckAngle { get; init; }
    public float BackAngle { get; init; }
    public float ShoulderLevel { get; init; }
    public float DistanceFromScreen { get; init; }
    public bool IsSlouching { get; init; }
    public bool IsTooClose { get; init; }
    public float PostureScore { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents eye strain monitoring data.
/// </summary>
public record EyeStrainData
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public int BlinksPerMinute { get; init; }
    public float EyeFatigueScore { get; init; }
    public TimeSpan TimeSinceLastBreak { get; init; }
    public float ScreenBrightness { get; init; }
    public float AmbientLight { get; init; }
    public float ContrastLevel { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents heart rate monitoring data.
/// </summary>
public record HeartRateData
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public int CurrentBpm { get; init; }
    public int AverageBpm { get; init; }
    public int MinBpm { get; init; }
    public int MaxBpm { get; init; }
    public HeartRateZone CurrentZone { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Heart rate zones.
/// </summary>
public enum HeartRateZone
{
    Rest,
    Light,
    Moderate,
    Vigorous,
    Maximum
}

/// <summary>
/// Configuration for gaming health monitoring.
/// </summary>
public record GamingHealthConfiguration
{
    public bool EnablePostureDetection { get; init; } = true;
    public bool EnableEyeStrainMonitoring { get; init; } = true;
    public bool EnableHeartRateMonitoring { get; init; } = false;
    public TimeSpan BreakInterval { get; init; } = TimeSpan.FromMinutes(20);
    public TimeSpan BreakDuration { get; init; } = TimeSpan.FromMinutes(5);
    public float MinScreenDistanceCm { get; init; } = 50;
    public int TargetBlinkRatePerMinute { get; init; } = 15;
    public int MaxHeartRateBpm { get; init; } = 180;
    public int MinHeartRateBpm { get; init; } = 50;
    public IReadOnlyDictionary<string, object> SensorSettings { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Represents a break reminder.
/// </summary>
public record BreakReminder
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public BreakType Type { get; init; }
    public string Message { get; init; } = string.Empty;
    public TimeSpan RecommendedDuration { get; init; } = TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> SuggestedActivities { get; init; } = Array.Empty<string>();
    public DateTime TriggeredAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Types of breaks.
/// </summary>
public enum BreakType
{
    Regular,
    EyeRest,
    PostureCorrection,
    Hydration,
    Stretching,
    Emergency
}

/// <summary>
/// Represents historical health statistics.
/// </summary>
public record HealthStatistics
{
    public string UserId { get; init; } = string.Empty;
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public int TotalSessions { get; init; }
    public TimeSpan TotalPlayTime { get; init; }
    public double AverageSessionDurationMinutes { get; init; }
    public double AveragePostureScore { get; init; }
    public int TotalAlerts { get; init; }
    public IReadOnlyDictionary<HealthMetricType, double> AverageMetrics { get; init; } = new Dictionary<HealthMetricType, double>();
}
