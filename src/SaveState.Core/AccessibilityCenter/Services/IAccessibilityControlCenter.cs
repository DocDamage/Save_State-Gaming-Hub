using SaveState.Core.AccessibilityCenter.Models;
using SaveState.Core.Common;

namespace SaveState.Core.AccessibilityCenter.Services;

/// <summary>
/// Central service that manages all accessibility features including one-switch mode, eye-gaze control, voice control, and colorblind modes.
/// </summary>
public interface IAccessibilityControlCenter
{
    /// <summary>
    /// Initializes the accessibility control center.
    /// </summary>
    /// <param name="configuration">Accessibility configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> InitializeAsync(AccessibilityConfiguration configuration, CancellationToken ct = default);

    // One-Switch Mode

    /// <summary>
    /// Enables one-switch mode for accessibility.
    /// </summary>
    /// <param name="configuration">One-switch configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> EnableOneSwitchModeAsync(OneSwitchConfiguration configuration, CancellationToken ct = default);

    /// <summary>
    /// Disables one-switch mode.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DisableOneSwitchModeAsync(CancellationToken ct = default);

    /// <summary>
    /// Triggers the switch action (for one-switch mode).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> TriggerSwitchAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current scan state for one-switch mode.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing scan state.</returns>
    Task<Result<OneSwitchScanState>> GetScanStateAsync(CancellationToken ct = default);

    /// <summary>
    /// Registers scannable elements for one-switch mode.
    /// </summary>
    /// <param name="elements">Elements to register.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> RegisterScannableElementsAsync(IReadOnlyList<ScannableElement> elements, CancellationToken ct = default);

    // Eye-Gaze Control

    /// <summary>
    /// Initializes eye-gaze tracking.
    /// </summary>
    /// <param name="configuration">Eye-gaze configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> InitializeEyeGazeAsync(EyeGazeConfiguration configuration, CancellationToken ct = default);

    /// <summary>
    /// Starts eye-gaze tracking.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> StartEyeGazeTrackingAsync(CancellationToken ct = default);

    /// <summary>
    /// Stops eye-gaze tracking.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> StopEyeGazeTrackingAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets current eye-gaze data.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing eye-gaze data.</returns>
    Task<Result<EyeGazeData>> GetEyeGazeDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Calibrates eye-gaze tracking.
    /// </summary>
    /// <param name="calibrationPoints">Calibration points.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating calibration success.</returns>
    Task<Result> CalibrateEyeGazeAsync(IReadOnlyList<(float X, float Y)> calibrationPoints, CancellationToken ct = default);

    // Voice Control

    /// <summary>
    /// Initializes voice control.
    /// </summary>
    /// <param name="configuration">Voice control configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> InitializeVoiceControlAsync(VoiceControlConfiguration configuration, CancellationToken ct = default);

    /// <summary>
    /// Starts voice control listening.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> StartVoiceControlAsync(CancellationToken ct = default);

    /// <summary>
    /// Stops voice control listening.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> StopVoiceControlAsync(CancellationToken ct = default);

    /// <summary>
    /// Registers a custom voice command.
    /// </summary>
    /// <param name="mapping">Voice command mapping.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> RegisterVoiceCommandAsync(VoiceCommandMapping mapping, CancellationToken ct = default);

    /// <summary>
    /// Removes a custom voice command.
    /// </summary>
    /// <param name="commandId">Command identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> UnregisterVoiceCommandAsync(string commandId, CancellationToken ct = default);

    /// <summary>
    /// Processes a voice command.
    /// </summary>
    /// <param name="audioData">Audio data of the command.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing command recognition result.</returns>
    Task<Result<VoiceCommandResult>> ProcessVoiceCommandAsync(byte[] audioData, CancellationToken ct = default);

    // Colorblind Mode

    /// <summary>
    /// Sets the colorblind mode.
    /// </summary>
    /// <param name="mode">Colorblind mode to enable.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SetColorblindModeAsync(ColorblindMode mode, CancellationToken ct = default);

    /// <summary>
    /// Gets the current colorblind mode.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing current colorblind mode.</returns>
    Task<Result<ColorblindMode>> GetColorblindModeAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the color correction matrix for a colorblind mode.
    /// </summary>
    /// <param name="mode">Colorblind mode.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing color correction matrix.</returns>
    Task<Result<ColorCorrectionMatrix>> GetColorCorrectionMatrixAsync(ColorblindMode mode, CancellationToken ct = default);

    // Profile Management

    /// <summary>
    /// Saves an accessibility profile.
    /// </summary>
    /// <param name="profile">Profile to save.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SaveProfileAsync(AccessibilityProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Loads an accessibility profile.
    /// </summary>
    /// <param name="profileId">Profile identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the profile.</returns>
    Task<Result<AccessibilityProfile>> LoadProfileAsync(string profileId, CancellationToken ct = default);

    /// <summary>
    /// Gets all accessibility profiles for a user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing profiles.</returns>
    Task<Result<IReadOnlyList<AccessibilityProfile>>> GetUserProfilesAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Applies an accessibility profile.
    /// </summary>
    /// <param name="profileId">Profile identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ApplyProfileAsync(string profileId, CancellationToken ct = default);

    /// <summary>
    /// Gets the current accessibility configuration.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing current configuration.</returns>
    Task<Result<AccessibilityConfiguration>> GetConfigurationAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates the accessibility configuration.
    /// </summary>
    /// <param name="configuration">New configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> UpdateConfigurationAsync(AccessibilityConfiguration configuration, CancellationToken ct = default);

    /// <summary>
    /// Shuts down the accessibility control center.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ShutdownAsync(CancellationToken ct = default);
}
