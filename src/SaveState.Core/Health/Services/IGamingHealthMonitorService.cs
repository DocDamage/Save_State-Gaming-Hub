using SaveState.Core.Common;
using SaveState.Core.Health.Models;

namespace SaveState.Core.Health.Services;

/// <summary>
/// Service interface for monitoring health during gaming sessions.
/// Tracks posture, eye strain, break reminders, and overall health scores.
/// </summary>
public interface IGamingHealthMonitorService
{
    /// <summary>
    /// Event raised when the health status changes.
    /// </summary>
    event EventHandler<HealthStatusChangedEventArgs>? StatusChanged;

    /// <summary>
    /// Event raised when a health alert is triggered.
    /// </summary>
    event EventHandler<HealthAlertEventArgs>? AlertTriggered;

    /// <summary>
    /// Event raised when a break reminder should be shown.
    /// </summary>
    event EventHandler<BreakReminderEventArgs>? BreakReminder;

    /// <summary>
    /// Gets whether health monitoring is currently active.
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Gets the current monitoring configuration.
    /// </summary>
    HealthMonitoringConfig CurrentConfig { get; }

    /// <summary>
    /// Starts health monitoring with the specified configuration.
    /// </summary>
    /// <param name="config">The monitoring configuration.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> StartMonitoringAsync(
        HealthMonitoringConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Stops health monitoring.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> StopMonitoringAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current health status snapshot.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the current health snapshot.</returns>
    Task<Result<HealthSnapshot>> GetCurrentStatusAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Generates a comprehensive health report for a completed session.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the session.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the health report.</returns>
    Task<Result<HealthReport>> GenerateSessionReportAsync(
        Guid sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Configures the alert rules for health monitoring.
    /// </summary>
    /// <param name="rules">The list of alert rules to apply.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ConfigureAlertsAsync(
        IReadOnlyList<HealthAlertRule> rules,
        CancellationToken ct = default);

    /// <summary>
    /// Acknowledges a health alert, preventing it from showing again.
    /// </summary>
    /// <param name="alertId">The unique identifier of the alert.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> AcknowledgeAlertAsync(
        string alertId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all active (unacknowledged) health alerts.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the list of active alerts.</returns>
    Task<Result<IReadOnlyList<HealthAlert>>> GetActiveAlertsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Calculates the current overall health score (0-100).
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the health score.</returns>
    Task<Result<float>> CalculateHealthScoreAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Records that the user has taken a break.
    /// </summary>
    /// <param name="is20_20_20Rule">Whether this was a 20-20-20 rule break.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> RecordBreakTakenAsync(
        bool is20_20_20Rule = false,
        CancellationToken ct = default);

    /// <summary>
    /// Updates the current posture status.
    /// </summary>
    /// <param name="posture">The new posture status.</param>
    /// <param name="confidence">Confidence level (0.0-1.0) of the detection.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> UpdatePostureAsync(
        PostureStatus posture,
        float confidence,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the history of posture readings for the current session.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the posture history.</returns>
    Task<Result<IReadOnlyList<PostureReading>>> GetPostureHistoryAsync(
        CancellationToken ct = default);
}
