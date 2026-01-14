using SaveState.Core.Common;
using SaveState.Core.Common.Base;

namespace SaveState.Core.Performance.Services;

/// <summary>
/// Service interface for audio optimization and profile management.
/// </summary>
public interface IAudioOptimizer
{
    /// <summary>
    /// Gets the current audio settings.
    /// </summary>
    Task<Result<AudioSettings>> GetCurrentSettingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates an audio profile for a specific game.
    /// </summary>
    Task<Result<AudioProfile>> CreateGameProfileAsync(Guid gameId, AudioSettings settings, CancellationToken ct = default);

    /// <summary>
    /// Gets an existing audio profile.
    /// </summary>
    Task<Result<AudioProfile>> GetProfileAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>
    /// Gets all audio profiles for a game.
    /// </summary>
    Task<Result<IReadOnlyList<AudioProfile>>> GetProfilesForGameAsync(Guid gameId, CancellationToken ct = default);

    /// <summary>
    /// Applies an audio profile.
    /// </summary>
    Task<Result> ApplyProfileAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>
    /// Applies audio settings directly.
    /// </summary>
    Task<Result> ApplySettingsAsync(AudioSettings settings, CancellationToken ct = default);

    /// <summary>
    /// Reverts to the original audio settings.
    /// </summary>
    Task<Result> RevertSettingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets available audio devices.
    /// </summary>
    Task<Result<IReadOnlyList<AudioDevice>>> GetAvailableDevicesAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the default audio device temporarily for gaming.
    /// </summary>
    Task<Result> SetTemporaryDeviceAsync(string deviceId, CancellationToken ct = default);

    /// <summary>
    /// Saves an audio profile.
    /// </summary>
    Task<Result> SaveProfileAsync(AudioProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Deletes an audio profile.
    /// </summary>
    Task<Result> DeleteProfileAsync(string profileName, CancellationToken ct = default);

    /// <summary>
    /// Gets all saved profiles.
    /// </summary>
    Task<Result<IReadOnlyList<AudioProfile>>> GetSavedProfilesAsync(CancellationToken ct = default);
}

/// <summary>
/// Represents audio settings for optimization.
/// </summary>
public sealed record AudioSettings(
    int SampleRate,
    int BitDepth,
    int BufferSize,
    int Channels,
    bool ExclusiveMode,
    bool SpatialAudio,
    AudioLatencyMode LatencyMode,
    string? PreferredDeviceId = null);

/// <summary>
/// Audio latency mode for gaming optimization.
/// </summary>
public enum AudioLatencyMode
{
    /// <summary>Default system latency.</summary>
    Default,

    /// <summary>Balanced latency and stability.</summary>
    Balanced,

    /// <summary>Low latency for competitive gaming.</summary>
    Low,

    /// <summary>Ultra-low latency (may cause audio issues).</summary>
    UltraLow
}

/// <summary>
/// Represents an audio device.
/// </summary>
public sealed record AudioDevice(
    string Id,
    string Name,
    string Description,
    AudioDeviceType Type,
    bool IsDefault,
    bool IsEnabled);

/// <summary>
/// Type of audio device.
/// </summary>
public enum AudioDeviceType
{
    Speakers,
    Headphones,
    Headset,
    Monitor,
    Other
}

/// <summary>
/// Represents a saved audio profile for a game.
/// </summary>
public class AudioProfile : EntityBase
{
    public Guid? GameId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }
    public AudioSettings Settings { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastAppliedAt { get; private set; }

    private AudioProfile() { }

    public static AudioProfile Create(Guid? gameId, string name, AudioSettings settings, bool isDefault = false)
    {
        return new AudioProfile
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Settings = settings,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkApplied() => LastAppliedAt = DateTime.UtcNow;
    public void SetAsDefault(bool isDefault) => IsDefault = isDefault;
    public void UpdateSettings(AudioSettings settings) => Settings = settings;
}
