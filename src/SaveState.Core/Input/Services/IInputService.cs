using SaveState.Core.Common;
using SaveState.Core.Input.Entities;

namespace SaveState.Core.Input.Services;

/// <summary>
/// Service for managing input devices and controller mappings.
/// Handles the application of controller profiles to the runtime environment.
/// </summary>
public interface IInputService
{
    /// <summary>
    /// Applies controller button mappings to the current session.
    /// </summary>
    /// <param name="mappings">Dictionary of button/action mappings (e.g., "A" -> "Confirm").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure of the mapping application.</returns>
    Task<Result> ApplyControllerMappingsAsync(
        IReadOnlyDictionary<string, string> mappings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all active controller mappings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ClearMappingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active controller mappings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the current mappings or an error.</returns>
    Task<Result<IReadOnlyDictionary<string, string>>> GetCurrentMappingsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects connected input devices.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing information about detected devices.</returns>
    Task<Result<IReadOnlyList<DetectedInputDevice>>> DetectDevicesAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a detected input device.
/// </summary>
public record DetectedInputDevice(
    string DeviceId,
    string DeviceName,
    ControllerType Type,
    bool IsConnected);
