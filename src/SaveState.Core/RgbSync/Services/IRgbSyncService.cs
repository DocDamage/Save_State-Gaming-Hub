using SaveState.Core.Common;
using SaveState.Core.RgbSync.Models;

namespace SaveState.Core.RgbSync.Services;

/// <summary>
/// Service that synchronizes RGB lighting across multiple vendor devices (Razer, Corsair, Logitech, etc.).
/// </summary>
public interface IRgbSyncService
{
    /// <summary>
    /// Initializes the RGB sync service and loads vendor SDKs.
    /// </summary>
    /// <param name="configuration">RGB sync configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> InitializeAsync(RgbSyncConfiguration configuration, CancellationToken ct = default);

    /// <summary>
    /// Gets information about available vendor SDKs.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing SDK information for each vendor.</returns>
    Task<Result<IReadOnlyList<RgbSdkInfo>>> GetSdkInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// Discovers and returns all connected RGB devices.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing connected RGB devices.</returns>
    Task<Result<IReadOnlyList<RgbDevice>>> GetDevicesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets devices filtered by vendor.
    /// </summary>
    /// <param name="vendor">Vendor to filter by.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing devices from the specified vendor.</returns>
    Task<Result<IReadOnlyList<RgbDevice>>> GetDevicesByVendorAsync(RgbVendor vendor, CancellationToken ct = default);

    /// <summary>
    /// Gets devices filtered by type.
    /// </summary>
    /// <param name="type">Device type to filter by.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing devices of the specified type.</returns>
    Task<Result<IReadOnlyList<RgbDevice>>> GetDevicesByTypeAsync(RgbDeviceType type, CancellationToken ct = default);

    /// <summary>
    /// Sets a solid color on a specific device.
    /// </summary>
    /// <param name="deviceId">Device identifier.</param>
    /// <param name="color">Color to set.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SetDeviceColorAsync(string deviceId, RgbColor color, CancellationToken ct = default);

    /// <summary>
    /// Sets colors on specific LEDs of a device.
    /// </summary>
    /// <param name="deviceId">Device identifier.</param>
    /// <param name="ledColors">Dictionary mapping LED indices to colors.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SetDeviceLedsAsync(string deviceId, IReadOnlyDictionary<int, RgbColor> ledColors, CancellationToken ct = default);

    /// <summary>
    /// Applies an effect to a device.
    /// </summary>
    /// <param name="deviceId">Device identifier.</param>
    /// <param name="effect">Effect to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ApplyEffectAsync(string deviceId, RgbEffect effect, CancellationToken ct = default);

    /// <summary>
    /// Applies an effect to all devices.
    /// </summary>
    /// <param name="effect">Effect to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ApplyEffectToAllAsync(RgbEffect effect, CancellationToken ct = default);

    /// <summary>
    /// Stops all effects on a device and sets it to static color.
    /// </summary>
    /// <param name="deviceId">Device identifier.</param>
    /// <param name="color">Static color to set.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> StopEffectsAsync(string deviceId, RgbColor? color = null, CancellationToken ct = default);

    /// <summary>
    /// Triggers a game event RGB effect.
    /// </summary>
    /// <param name="gameEvent">Game event data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> TriggerGameEventAsync(GameRgbEvent gameEvent, CancellationToken ct = default);

    /// <summary>
    /// Sets up health indicator on specified device.
    /// </summary>
    /// <param name="config">Health indicator configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SetupHealthIndicatorAsync(HealthIndicatorConfig config, CancellationToken ct = default);

    /// <summary>
    /// Updates health indicator based on current health percentage.
    /// </summary>
    /// <param name="healthPercentage">Current health percentage (0-100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> UpdateHealthIndicatorAsync(int healthPercentage, CancellationToken ct = default);

    /// <summary>
    /// Registers a custom effect for a specific game event.
    /// </summary>
    /// <param name="eventType">Game event type.</param>
    /// <param name="effect">Effect to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> RegisterGameEventEffectAsync(string eventType, RgbEffect effect, CancellationToken ct = default);

    /// <summary>
    /// Sets the global brightness for all devices.
    /// </summary>
    /// <param name="brightness">Brightness value (0.0 to 1.0).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SetGlobalBrightnessAsync(float brightness, CancellationToken ct = default);

    /// <summary>
    /// Updates the RGB sync configuration.
    /// </summary>
    /// <param name="configuration">New configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> UpdateConfigurationAsync(RgbSyncConfiguration configuration, CancellationToken ct = default);

    /// <summary>
    /// Gets the current RGB sync configuration.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing current configuration.</returns>
    Task<Result<RgbSyncConfiguration>> GetConfigurationAsync(CancellationToken ct = default);

    /// <summary>
    /// Refreshes the list of connected devices.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> RefreshDevicesAsync(CancellationToken ct = default);

    /// <summary>
    /// Shuts down the RGB sync service.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ShutdownAsync(CancellationToken ct = default);
}
