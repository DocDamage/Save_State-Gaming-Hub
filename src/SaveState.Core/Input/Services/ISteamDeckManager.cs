using SaveState.Core.Common;
using SaveState.Core.Input.Entities;

namespace SaveState.Core.Input.Services;

public interface ISteamDeckManager
{
    Task<Result<bool>> DetectSteamDeckAsync(CancellationToken ct = default);
    Task<Result> EnableSteamDeckModeAsync(CancellationToken ct = default);
    Task<Result> DisableSteamDeckModeAsync(CancellationToken ct = default);
    Task<Result<SteamDeckProfile>> CreateProfileAsync(SteamDeckConfig config, CancellationToken ct = default);
    Task<Result<SteamDeckProfile?>> GetCurrentProfileAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<SteamDeckProfile>>> GetAllProfilesAsync(CancellationToken ct = default);
    bool IsSteamDeckModeActive { get; }
    event EventHandler<SteamDeckModeChangedEventArgs>? SteamDeckModeChanged;
}

public sealed record SteamDeckConfig(
    GyroSensitivity GyroSensitivity,
    TouchSensitivity TouchSensitivity,
    bool EnableHaptics,
    bool OptimizeForBattery,
    bool EnableSteamInput,
    bool ForceDesktopMode);

public sealed record SteamDeckProfile(
    Guid Id,
    string Name,
    SteamDeckConfig Config,
    DateTime CreatedAt,
    bool IsActive);

public enum GyroSensitivity { Off, Low, Medium, High, Maximum }
public enum TouchSensitivity { Low, Medium, High, Maximum }

public sealed class SteamDeckModeChangedEventArgs : EventArgs
{
    public bool IsActive { get; init; }
    public SteamDeckConfig? ActiveConfig { get; init; }
}