using SaveState.Core.RgbSync.Models;

namespace SaveState.Core.RgbSync.Services;

public interface IRgbProvider
{
    string Id { get; }
    string Name { get; }
    string? Version { get; }
    bool IsAvailable { get; }
    
    Task InitializeAsync(CancellationToken ct = default);
    Task ShutdownAsync(CancellationToken ct = default);
    
    Task<IReadOnlyList<RgbDevice>> GetDevicesAsync(CancellationToken ct = default);
    Task SetDeviceColorAsync(RgbDevice device, RgbColor color, CancellationToken ct = default);
    Task SetDeviceEffectAsync(RgbDevice device, RgbEffect effect, CancellationToken ct = default);
    Task UpdateDeviceAsync(RgbDevice device, CancellationToken ct = default);
}
