using SaveState.Core.Common.Services;

namespace SaveState.Core.Health.Models;

/// <summary>
/// Represents the user's posture status during gaming sessions.
/// </summary>
public enum PostureStatus
{
    Excellent,
    Good,
    Slouching,
    Poor,
    Critical
}

/// <summary>
/// Represents the level of eye strain based on screen time and usage patterns.
/// </summary>
public enum EyeStrainLevel
{
    None,
    Low,
    Moderate,
    High,
    Critical
}

/// <summary>
/// Represents the user's focus level during gaming.
/// </summary>
public enum FocusLevel
{
    Low,
    Medium,
    High,
    Deep
}

/// <summary>
/// Represents the user's stress level during gaming.
/// </summary>
public enum StressLevel
{
    Relaxed,
    Normal,
    Elevated,
    High
}

/// <summary>
/// Represents the user's fatigue level during gaming sessions.
/// </summary>
public enum FatigueLevel
{
    Rested,
    Alert,
    Tired,
    Exhausted
}

/// <summary>
/// Configuration settings for health monitoring during gaming sessions.
/// </summary>
public record HealthMonitoringConfig
{
    public required bool EnablePostureDetection { get; init; }
    public required bool EnableEyeStrainMonitoring { get; init; }
    public required bool EnableHeartRateMonitoring { get; init; }
    public required bool EnableErgonomicWarnings { get; init; }
    public required bool EnableBreakReminders { get; init; }
    public required TimeSpan AlertInterval { get; init; }
    public required TimeSpan BreakInterval { get; init; }
    public required int BreakDurationMinutes { get; init; }

    /// <summary>
    /// Creates a default configuration with recommended settings.
    /// </summary>
    public static HealthMonitoringConfig Default => new()
    {
        EnablePostureDetection = true,
        EnableEyeStrainMonitoring = true,
        EnableHeartRateMonitoring = false,
        EnableErgonomicWarnings = true,
        EnableBreakReminders = true,
        AlertInterval = TimeSpan.FromMinutes(5),
        BreakInterval = TimeSpan.FromMinutes(60),
        BreakDurationMinutes = 5
    };
}

/// <summary>
/// A snapshot of the user's current health status during a gaming session.
/// </summary>
public record HealthSnapshot
{
    public required PostureStatus Posture { get; init; }
    public required EyeStrainLevel EyeStrain { get; init; }
    public required int? HeartRate { get; init; }
    public required TimeSpan SessionDuration { get; init; }
    public required int BreakReminderCount { get; init; }
    public required float HealthScore { get; init; }
    public required FocusLevel Focus { get; init; }
    public required StressLevel Stress { get; init; }
    public required FatigueLevel Fatigue { get; init; }
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Time since the last 20-20-20 rule reminder.
    /// </summary>
    public TimeSpan TimeSinceLastEyeBreak { get; init; }

    /// <summary>
    /// Time until the next scheduled break.
    /// </summary>
    public TimeSpan TimeUntilNextBreak { get; init; }
}

/// <summary>
/// A comprehensive report of a gaming session's health metrics.
/// </summary>
public record HealthReport
{
    public required Guid SessionId { get; init; }
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
    public required TimeSpan TotalDuration { get; init; }
    public required int BreaksTaken { get; init; }
    public required float AverageHealthScore { get; init; }
    public required IReadOnlyList<HealthAlert> Alerts { get; init; }
    public required IReadOnlyList<PostureReading> PostureHistory { get; init; }

    /// <summary>
    /// Percentage of time spent in good or excellent posture.
    /// </summary>
    public float GoodPosturePercentage { get; init; }

    /// <summary>
    /// Total time spent looking at the screen without breaks.
    /// </summary>
    public TimeSpan TotalScreenTime { get; init; }

    /// <summary>
    /// Number of 20-20-20 rule breaks taken.
    /// </summary>
    public int EyeBreaksTaken { get; init; }
}

/// <summary>
/// Represents a health alert triggered during a gaming session.
/// </summary>
public record HealthAlert
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string Message { get; init; }
    public required DateTime TriggeredAt { get; init; }
    public required bool IsAcknowledged { get; init; }
    public required AlertSeverity Severity { get; init; }

    /// <summary>
    /// Creates a new health alert.
    /// </summary>
    public static HealthAlert Create(string type, string message, AlertSeverity severity)
        => Create(type, message, severity, SystemTimeProvider.Instance.UtcNow);

    public static HealthAlert Create(string type, string message, AlertSeverity severity, DateTime triggeredAt)
    {
        return new HealthAlert
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Type = type,
            Message = message,
            TriggeredAt = triggeredAt,
            IsAcknowledged = false,
            Severity = severity
        };
    }
}

/// <summary>
/// Severity levels for health alerts.
/// </summary>
public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// A single posture reading at a specific point in time.
/// </summary>
public record PostureReading
{
    public required DateTime Timestamp { get; init; }
    public required PostureStatus Status { get; init; }
    public required float Confidence { get; init; }
}

/// <summary>
/// A configurable rule for triggering health alerts.
/// </summary>
public record HealthAlertRule
{
    public required string RuleId { get; init; }
    public required string Name { get; init; }
    public required string Condition { get; init; }
    public required string Message { get; init; }
    public required bool IsEnabled { get; init; }
    public required AlertSeverity Severity { get; init; }

    /// <summary>
    /// Creates a default set of health alert rules.
    /// </summary>
    public static IReadOnlyList<HealthAlertRule> DefaultRules => new List<HealthAlertRule>
    {
        new()
        {
            RuleId = "posture-poor",
            Name = "Poor Posture Warning",
            Condition = "Posture == Poor && Duration > 5m",
            Message = "Your posture has been poor for 5 minutes. Please sit up straight!",
            IsEnabled = true,
            Severity = AlertSeverity.Warning
        },
        new()
        {
            RuleId = "eye-strain-high",
            Name = "Eye Strain Warning",
            Condition = "EyeStrain >= High",
            Message = "High eye strain detected. Consider taking a break using the 20-20-20 rule.",
            IsEnabled = true,
            Severity = AlertSeverity.Warning
        },
        new()
        {
            RuleId = "break-overdue",
            Name = "Break Reminder",
            Condition = "TimeSinceBreak >= 60m",
            Message = "You've been gaming for an hour. Time for a break!",
            IsEnabled = true,
            Severity = AlertSeverity.Info
        },
        new()
        {
            RuleId = "fatigue-exhausted",
            Name = "Fatigue Warning",
            Condition = "Fatigue == Exhausted",
            Message = "You appear exhausted. Consider ending your session and resting.",
            IsEnabled = true,
            Severity = AlertSeverity.Critical
        }
    };
}

/// <summary>
/// Event arguments for health status changes.
/// </summary>
public class HealthStatusChangedEventArgs : EventArgs
{
    public required HealthSnapshot PreviousStatus { get; init; }
    public required HealthSnapshot CurrentStatus { get; init; }
    public required string ChangedProperty { get; init; }
}

/// <summary>
/// Event arguments for health alerts.
/// </summary>
public class HealthAlertEventArgs : EventArgs
{
    public required HealthAlert Alert { get; init; }
    public required HealthSnapshot CurrentStatus { get; init; }
}

/// <summary>
/// Event arguments for break reminders.
/// </summary>
public class BreakReminderEventArgs : EventArgs
{
    public required TimeSpan SessionDuration { get; init; }
    public required TimeSpan RecommendedBreakDuration { get; init; }
    public required string Message { get; init; }
    public required bool Is20_20_20Rule { get; init; }
}
