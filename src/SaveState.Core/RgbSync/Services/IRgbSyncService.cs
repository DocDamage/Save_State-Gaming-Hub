using SaveState.Core.Common;
using SaveState.Core.RgbSync.Models;

namespace SaveState.Core.RgbSync.Services;

public interface IRgbSyncService
{
    // Device Management
    Task<Result<IReadOnlyList<RgbDevice>>> GetDevicesAsync(CancellationToken ct = default);
    Task<Result<RgbDevice>> GetDeviceAsync(Guid deviceId, CancellationToken ct = default);
    Task<Result> RefreshDevicesAsync(CancellationToken ct = default);
    
    // Effect Control
    Task<Result> SetDeviceColorAsync(Guid deviceId, RgbColor color, CancellationToken ct = default);
    Task<Result> SetDeviceEffectAsync(Guid deviceId, RgbEffect effect, CancellationToken ct = default);
    Task<Result> SetAllDevicesColorAsync(RgbColor color, CancellationToken ct = default);
    Task<Result> SetAllDevicesEffectAsync(RgbEffect effect, CancellationToken ct = default);
    
    // Profile Management
    Task<Result<RgbProfile>> CreateProfileAsync(string name, CancellationToken ct = default);
    Task<Result> ApplyProfileAsync(Guid profileId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<RgbProfile>>> GetProfilesAsync(CancellationToken ct = default);
    Task<Result> DeleteProfileAsync(Guid profileId, CancellationToken ct = default);
    
    // Sync Groups
    Task<Result<RgbSyncGroup>> CreateSyncGroupAsync(string name, List<Guid> deviceIds, CancellationToken ct = default);
    Task<Result> UpdateSyncGroupEffectAsync(Guid groupId, RgbEffect effect, CancellationToken ct = default);
    Task<Result> DeleteSyncGroupAsync(Guid groupId, CancellationToken ct = default);
    
    // Game State Integration
    Task<Result> TriggerGameStateEffectAsync(GameStateRgbTrigger trigger, CancellationToken ct = default);
    Task<Result> ConfigureGameStateEffectsAsync(List<GameStateRgbConfig> configs, CancellationToken ct = default);
    
    // Provider Management
    Task<Result<IReadOnlyList<RgbProviderInfo>>> GetProvidersAsync(CancellationToken ct = default);
    Task<Result> EnableProviderAsync(string providerId, CancellationToken ct = default);
    Task<Result> DisableProviderAsync(string providerId, CancellationToken ct = default);
    
    // Control
    Task<Result> StartAsync(CancellationToken ct = default);
    Task<Result> StopAsync(CancellationToken ct = default);
    Task<bool> IsRunningAsync(CancellationToken ct = default);
}
