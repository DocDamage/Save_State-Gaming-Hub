using SaveState.Core.Common;
using SaveState.Core.GamingHealth.Models;

namespace SaveState.Core.GamingHealth.Services;

/// <summary>
/// Service that monitors gaming health including posture, eye strain, and heart rate.
/// </summary>
public interface IGamingHealthMonitorService
{
    /// <summary>
    /// Initializes the health monitoring service with configuration.
    /// </summary>
    /// <param name="configuration">Health monitoring configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> InitializeAsync(GamingHealthConfiguration configuration, CancellationToken ct = default);

    /// <summary>
    /// Starts a new health monitoring session.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the session ID.</returns>
    Task<Result<string>> StartSessionAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Ends the current monitoring session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the session summary.</returns>
    Task<Result<SessionHealthSummary>> EndSessionAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Gets the current posture data.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing posture data.</returns>
    Task<Result<PostureData>> GetCurrentPostureAsync(CancellationToken ct = default);

    /// <summary>
    /// Analyzes posture from camera input.
    /// </summary>
    /// <param name="imageData">Camera image data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing posture analysis.</returns>
    Task<Result<PostureData>> AnalyzePostureAsync(byte[] imageData, CancellationToken ct = default);

    /// <summary>
    /// Gets current eye strain data.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing eye strain data.</returns>
    Task<Result<EyeStrainData>> GetEyeStrainDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Monitors eye strain metrics.
    /// </summary>
    /// <param name="screenTimeMinutes">Current screen time in minutes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing eye strain assessment.</returns>
    Task<Result<EyeStrainData>> MonitorEyeStrainAsync(int screenTimeMinutes, CancellationToken ct = default);

    /// <summary>
    /// Gets current heart rate data.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing heart rate data.</returns>
    Task<Result<HeartRateData>> GetHeartRateAsync(CancellationToken ct = default);

    /// <summary>
    /// Connects to a heart rate monitoring device.
    /// </summary>
    /// <param name="deviceAddress">Device address or identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ConnectHeartRateDeviceAsync(string deviceAddress, CancellationToken ct = default);

    /// <summary>
    /// Gets the next break reminder.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing break reminder.</returns>
    Task<Result<BreakReminder>> GetNextBreakReminderAsync(CancellationToken ct = default);

    /// <summary>
    /// Acknowledges a health alert.
    /// </summary>
    /// <param name="alertId">The alert identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> AcknowledgeAlertAsync(string alertId, CancellationToken ct = default);

    /// <summary>
    /// Gets all active alerts.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing active alerts.</returns>
    Task<Result<IReadOnlyList<HealthAlert>>> GetActiveAlertsAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates the health monitoring configuration.
    /// </summary>
    /// <param name="configuration">New configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> UpdateConfigurationAsync(GamingHealthConfiguration configuration, CancellationToken ct = default);

    /// <summary>
    /// Gets health statistics for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="periodStart">Start of the period.</param>
    /// <param name="periodEnd">End of the period.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing health statistics.</returns>
    Task<Result<HealthStatistics>> GetHealthStatisticsAsync(string userId, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default);

    /// <summary>
    /// Gets recommendations based on health data.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing health recommendations.</returns>
    Task<Result<IReadOnlyList<string>>> GetHealthRecommendationsAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Shuts down the health monitoring service.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ShutdownAsync(CancellationToken ct = default);
}
