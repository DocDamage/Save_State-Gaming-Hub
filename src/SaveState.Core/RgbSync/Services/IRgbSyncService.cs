using SaveState.Core.Common;
using SaveState.Core.RgbSync.Models;

namespace SaveState.Core.RgbSync.Services;

/// <summary>
/// Service for managing RGB lighting devices, effects, profiles, and synchronization.
/// </summary>
public interface IRgbSyncService
{
    // Device Discovery
    Task<Result<IReadOnlyList<RgbDevice>>> DiscoverDevicesAsync(CancellationToken ct = default);

    // Device Management
    Task<Result<IReadOnlyList<RgbDevice>>> GetDevicesAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<RgbDevice>>> GetConnectedDevicesAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<RgbDevice>>> GetDevicesByTypeAsync(RgbDeviceType type, CancellationToken ct = default);
    Task<Result<RgbDevice>> GetDeviceAsync(Guid deviceId, CancellationToken ct = default);
    Task<Result> RefreshDevicesAsync(CancellationToken ct = default);
    Task<Result> ConnectDeviceAsync(Guid deviceId, CancellationToken ct = default);
    Task<Result> DisconnectDeviceAsync(Guid deviceId, CancellationToken ct = default);

    // Effect Control
    Task<Result> ApplyEffectAsync(Guid deviceId, RgbEffect effect, CancellationToken ct = default);
    Task<Result> ApplyEffectToMultipleAsync(IReadOnlyList<Guid> deviceIds, RgbEffect effect, CancellationToken ct = default);
    Task<Result> ClearEffectAsync(Guid deviceId, CancellationToken ct = default);
    Task<Result> SetDeviceColorAsync(Guid deviceId, RgbColor color, CancellationToken ct = default);
    Task<Result> SetDeviceBrightnessAsync(Guid deviceId, float brightness, CancellationToken ct = default);
    Task<Result> SetEffectSpeedAsync(Guid deviceId, float speed, CancellationToken ct = default);
    Task<Result> SetDeviceLedsAsync(Guid deviceId, Dictionary<int, RgbColor> ledColors, CancellationToken ct = default);
    Task<Result> SetDeviceEffectAsync(Guid deviceId, RgbEffect effect, CancellationToken ct = default);
    Task<Result> SetAllDevicesColorAsync(RgbColor color, CancellationToken ct = default);
    Task<Result> SetAllDevicesEffectAsync(RgbEffect effect, CancellationToken ct = default);

    // Profile Management
    Task<Result<RgbProfile>> CreateProfileAsync(RgbProfile profile, CancellationToken ct = default);
    Task<Result<RgbProfile>> CreateProfileAsync(string name, CancellationToken ct = default);
    Task<Result> ApplyProfileAsync(Guid profileId, CancellationToken ct = default);
    Task<Result<RgbProfile>> GetProfileAsync(Guid profileId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<RgbProfile>>> GetAllProfilesAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<RgbProfile>>> GetProfilesAsync(CancellationToken ct = default);
    Task<Result> UpdateProfileAsync(RgbProfile profile, CancellationToken ct = default);
    Task<Result> DeleteProfileAsync(Guid profileId, CancellationToken ct = default);
    Task<Result> SetDefaultProfileAsync(Guid profileId, CancellationToken ct = default);
    Task<Result<RgbProfile>> DuplicateProfileAsync(Guid profileId, string newName, CancellationToken ct = default);
    Task<Result<string>> ExportProfileAsync(Guid profileId, CancellationToken ct = default);
    Task<Result<RgbProfile>> ImportProfileAsync(string profileData, CancellationToken ct = default);

    // Sync Groups
    Task<Result<RgbSyncGroup>> CreateSyncGroupAsync(RgbSyncGroup group, CancellationToken ct = default);
    Task<Result<RgbSyncGroup>> CreateSyncGroupAsync(string name, List<Guid> deviceIds, CancellationToken ct = default);
    Task<Result<RgbSyncGroup>> GetSyncGroupAsync(Guid groupId, CancellationToken ct = default);
    Task<Result> UpdateSyncGroupAsync(RgbSyncGroup group, CancellationToken ct = default);
    Task<Result> UpdateSyncGroupEffectAsync(Guid groupId, RgbEffect effect, CancellationToken ct = default);
    Task<Result> DeleteSyncGroupAsync(Guid groupId, CancellationToken ct = default);
    Task<Result> AddDeviceToSyncGroupAsync(Guid groupId, Guid deviceId, CancellationToken ct = default);
    Task<Result> RemoveDeviceFromSyncGroupAsync(Guid groupId, Guid deviceId, CancellationToken ct = default);
    Task<Result> ApplySyncGroupEffectAsync(Guid groupId, CancellationToken ct = default);

    // Game State Integration
    Task<Result> SetGameStateTriggerAsync(GameStateRgbConfig config, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GameStateRgbConfig>>> GetGameStateTriggersAsync(CancellationToken ct = default);
    Task<Result> RemoveGameStateTriggerAsync(GameStateRgbTrigger trigger, CancellationToken ct = default);
    Task<Result> TriggerGameStateAsync(GameStateRgbTrigger trigger, CancellationToken ct = default);
    Task<Result> TriggerGameStateEffectAsync(GameStateRgbTrigger trigger, CancellationToken ct = default);
    Task<Result> ConfigureGameStateEffectsAsync(List<GameStateRgbConfig> configs, CancellationToken ct = default);

    // Provider Management
    Task<Result<IReadOnlyList<RgbProviderInfo>>> GetAvailableProvidersAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<RgbProviderInfo>>> GetProvidersAsync(CancellationToken ct = default);
    Task<Result> EnableProviderAsync(string providerId, CancellationToken ct = default);
    Task<Result> DisableProviderAsync(string providerId, CancellationToken ct = default);

    // Control
    Task<Result> StartAsync(CancellationToken ct = default);
    Task<Result> StopAsync(CancellationToken ct = default);
    Task<bool> IsRunningAsync(CancellationToken ct = default);
}
