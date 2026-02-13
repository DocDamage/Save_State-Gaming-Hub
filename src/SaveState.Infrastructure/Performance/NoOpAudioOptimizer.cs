using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Performance.Services;

namespace SaveState.Infrastructure.Performance;

/// <summary>
/// No-op implementation of audio optimization for non-Windows platforms.
/// Logs warnings but does not crash, allowing the application to run on Linux/macOS.
/// </summary>
public sealed class NoOpAudioOptimizer : IAudioOptimizer
{
    private readonly ILogger<NoOpAudioOptimizer> _logger;
    private readonly Dictionary<Guid, AudioProfile> _profiles = new();
    private readonly object _profilesLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NoOpAudioOptimizer"/> class.
    /// </summary>
    public NoOpAudioOptimizer(ILogger<NoOpAudioOptimizer> logger)
    {
        _logger = logger;
        _logger.LogWarning("Audio optimization is not available on this platform. Using no-op implementation.");
    }

    /// <inheritdoc />
    public Task<Result<AudioSettings>> GetCurrentSettingsAsync(CancellationToken ct = default)
    {
        // Return sensible defaults
        var settings = new AudioSettings(
            SampleRate: 48000,
            BitDepth: 24,
            BufferSize: 480,
            Channels: 2,
            ExclusiveMode: false,
            SpatialAudio: false,
            LatencyMode: AudioLatencyMode.Default,
            PreferredDeviceId: null);

        return Task.FromResult(Result.Success(settings));
    }

    /// <inheritdoc />
    public Task<Result<AudioProfile>> CreateGameProfileAsync(Guid gameId, AudioSettings settings, CancellationToken ct = default)
    {
        var profile = AudioProfile.Create(gameId, $"Audio Profile - {gameId:N}", settings, false);

        lock (_profilesLock)
        {
            _profiles[profile.Id] = profile;
        }

        _logger.LogInformation("Created audio profile {ProfileId} for game {GameId} (no-op mode)", profile.Id, gameId);
        return Task.FromResult(Result.Success(profile));
    }

    /// <inheritdoc />
    public Task<Result<AudioProfile>> GetProfileAsync(Guid profileId, CancellationToken ct = default)
    {
        lock (_profilesLock)
        {
            if (_profiles.TryGetValue(profileId, out var profile))
            {
                return Task.FromResult(Result.Success(profile));
            }
        }

        return Task.FromResult(Result.Failure<AudioProfile>($"Profile {profileId} not found", ErrorType.NotFound));
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<AudioProfile>>> GetProfilesForGameAsync(Guid gameId, CancellationToken ct = default)
    {
        lock (_profilesLock)
        {
            var profiles = _profiles.Values.Where(p => p.GameId == gameId).ToList();
            return Task.FromResult(Result.Success<IReadOnlyList<AudioProfile>>(profiles));
        }
    }

    /// <inheritdoc />
    public Task<Result> ApplyProfileAsync(Guid profileId, CancellationToken ct = default)
    {
        lock (_profilesLock)
        {
            if (_profiles.TryGetValue(profileId, out var profile))
            {
                profile.MarkApplied();
                _logger.LogInformation("Marked audio profile {ProfileId} as applied (no-op mode - settings not actually changed)", profileId);
                return Task.FromResult(Result.Success());
            }
        }

        return Task.FromResult(Result.Failure($"Profile {profileId} not found", ErrorType.NotFound));
    }

    /// <inheritdoc />
    public Task<Result> ApplySettingsAsync(AudioSettings settings, CancellationToken ct = default)
    {
        _logger.LogWarning("Audio settings application is not supported on this platform (no-op mode)");
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> RevertSettingsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Audio settings revert requested (no-op mode - no changes to revert)");
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<AudioDevice>>> GetAvailableDevicesAsync(CancellationToken ct = default)
    {
        // Return a single default device
        var devices = new List<AudioDevice>
        {
            new AudioDevice(
                Id: "default",
                Name: "Default Audio Device",
                Description: "System default audio output (platform-specific control unavailable)",
                Type: AudioDeviceType.Speakers,
                IsDefault: true,
                IsEnabled: true)
        };

        return Task.FromResult(Result.Success<IReadOnlyList<AudioDevice>>(devices));
    }

    /// <inheritdoc />
    public Task<Result> SetTemporaryDeviceAsync(string deviceId, CancellationToken ct = default)
    {
        _logger.LogWarning("Audio device switching is not supported on this platform");
        return Task.FromResult(Result.Failure(
            "Audio device switching is only available on Windows. On Linux, use PulseAudio/PipeWire. On macOS, use System Preferences.",
            ErrorType.NotImplemented));
    }

    /// <inheritdoc />
    public Task<Result> SaveProfileAsync(AudioProfile profile, CancellationToken ct = default)
    {
        lock (_profilesLock)
        {
            _profiles[profile.Id] = profile;
        }

        _logger.LogInformation("Saved audio profile '{ProfileName}' (no-op mode)", profile.Name);
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> DeleteProfileAsync(string profileName, CancellationToken ct = default)
    {
        lock (_profilesLock)
        {
            var profileToDelete = _profiles.Values.FirstOrDefault(p => p.Name == profileName);
            if (profileToDelete != null)
            {
                _profiles.Remove(profileToDelete.Id);
                _logger.LogInformation("Deleted audio profile '{ProfileName}' (no-op mode)", profileName);
                return Task.FromResult(Result.Success());
            }
        }

        return Task.FromResult(Result.Failure($"Profile '{profileName}' not found", ErrorType.NotFound));
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<AudioProfile>>> GetSavedProfilesAsync(CancellationToken ct = default)
    {
        lock (_profilesLock)
        {
            var profiles = _profiles.Values.OrderByDescending(p => p.CreatedAt).ToList();
            return Task.FromResult(Result.Success<IReadOnlyList<AudioProfile>>(profiles));
        }
    }
}
