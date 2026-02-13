using SaveState.Core.BiometricGaming.Models;
using SaveState.Core.Common;

namespace SaveState.Core.BiometricGaming.Services;

/// <summary>
/// Hub that integrates biometric sensors (EEG, GSR, heart rate) for adaptive gaming experiences.
/// </summary>
public interface IBiometricGamingHub
{
    /// <summary>
    /// Initializes the biometric gaming hub.
    /// </summary>
    /// <param name="configuration">Biometric gaming configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> InitializeAsync(BiometricGamingConfiguration configuration, CancellationToken ct = default);

    /// <summary>
    /// Discovers and returns all available biometric sensors.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing available sensors.</returns>
    Task<Result<IReadOnlyList<BiometricSensor>>> DiscoverSensorsAsync(CancellationToken ct = default);

    /// <summary>
    /// Connects to a biometric sensor.
    /// </summary>
    /// <param name="sensorId">Sensor identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ConnectSensorAsync(string sensorId, CancellationToken ct = default);

    /// <summary>
    /// Disconnects from a biometric sensor.
    /// </summary>
    /// <param name="sensorId">Sensor identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DisconnectSensorAsync(string sensorId, CancellationToken ct = default);

    /// <summary>
    /// Gets all connected sensors.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing connected sensors.</returns>
    Task<Result<IReadOnlyList<BiometricSensor>>> GetConnectedSensorsAsync(CancellationToken ct = default);

    /// <summary>
    /// Starts a biometric gaming session.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="gameId">Game identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the session ID.</returns>
    Task<Result<string>> StartSessionAsync(string userId, string gameId, CancellationToken ct = default);

    /// <summary>
    /// Ends the current biometric gaming session.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing session summary.</returns>
    Task<Result<BiometricSessionSummary>> EndSessionAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Gets the latest biometric data from all connected sensors.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing current biometric data.</returns>
    Task<Result<BiometricData>> GetCurrentBiometricDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current cognitive state.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing cognitive state.</returns>
    Task<Result<CognitiveState>> GetCognitiveStateAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current emotional state.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing emotional state.</returns>
    Task<Result<EmotionalState>> GetEmotionalStateAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current physiological state.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing physiological state.</returns>
    Task<Result<PhysiologicalState>> GetPhysiologicalStateAsync(CancellationToken ct = default);

    /// <summary>
    /// Calculates adaptive difficulty based on current biometric state.
    /// </summary>
    /// <param name="sessionId">Current session ID.</param>
    /// <param name="currentDifficulty">Current game difficulty (0.0 to 1.0).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing difficulty adjustment recommendation.</returns>
    Task<Result<AdaptiveDifficultyAdjustment>> CalculateAdaptiveDifficultyAsync(string sessionId, float currentDifficulty, CancellationToken ct = default);

    /// <summary>
    /// Applies a difficulty adjustment to the current session.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="adjustment">Difficulty adjustment to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ApplyDifficultyAdjustmentAsync(string sessionId, AdaptiveDifficultyAdjustment adjustment, CancellationToken ct = default);

    /// <summary>
    /// Subscribes to real-time biometric data updates.
    /// </summary>
    /// <param name="callback">Callback to receive data updates.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing subscription ID.</returns>
    Task<Result<string>> SubscribeToBiometricDataAsync(Action<BiometricData> callback, CancellationToken ct = default);

    /// <summary>
    /// Unsubscribes from biometric data updates.
    /// </summary>
    /// <param name="subscriptionId">Subscription identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> UnsubscribeFromBiometricDataAsync(string subscriptionId, CancellationToken ct = default);

    /// <summary>
    /// Calibrates sensors for the current user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating calibration success.</returns>
    Task<Result> CalibrateSensorsAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets baseline biometric data for a user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing baseline data.</returns>
    Task<Result<BiometricData>> GetUserBaselineAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets session history for a user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing session history.</returns>
    Task<Result<IReadOnlyList<BiometricGamingSession>>> GetSessionHistoryAsync(string userId, DateTime startDate, DateTime endDate, CancellationToken ct = default);

    /// <summary>
    /// Updates the biometric gaming configuration.
    /// </summary>
    /// <param name="configuration">New configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> UpdateConfigurationAsync(BiometricGamingConfiguration configuration, CancellationToken ct = default);

    /// <summary>
    /// Gets the current configuration.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing current configuration.</returns>
    Task<Result<BiometricGamingConfiguration>> GetConfigurationAsync(CancellationToken ct = default);

    /// <summary>
    /// Shuts down the biometric gaming hub.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ShutdownAsync(CancellationToken ct = default);
}
