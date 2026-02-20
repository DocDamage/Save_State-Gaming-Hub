using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services.SoundDesign;

/// <summary>
/// Manages audio effects and presets.
/// </summary>
public class SoundEffectManager
{
    private readonly ILogger<SoundEffectManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, SoundDesignStudioAudioEffect> _audioEffects;

    public SoundEffectManager(
        ILogger<SoundEffectManager> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _audioEffects = new Dictionary<string, SoundDesignStudioAudioEffect>();
        
        InitializeDefaultEffects();
    }

    /// <summary>
    /// Gets all audio effects.
    /// </summary>
    public IReadOnlyDictionary<string, SoundDesignStudioAudioEffect> AudioEffects => _audioEffects;

    /// <summary>
    /// Creates a new audio effect.
    /// </summary>
    /// <param name="request">The effect creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the created audio effect.</returns>
    public async Task<Result<SoundDesignStudioAudioEffect>> CreateAudioEffectAsync(SoundDesignStudioAudioEffectRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating audio effect: {Name} ({Type})", request.Name, request.Type);

            var effect = new SoundDesignStudioAudioEffect
            {
                EffectId = Guid.NewGuid().ToString(),
                Name = request.Name,
                Type = request.Type,
                Parameters = request.Parameters,
                Enabled = true,
                WetDryMix = request.WetDryMix,
                Bypass = false,
                PresetName = request.PresetName,
                CreatedAt = _timeProvider.UtcNow
            };

            _audioEffects[effect.EffectId] = effect;

            _logger.LogInformation("Audio effect created: {EffectId}", effect.EffectId);
            return Result.Success<SoundDesignStudioAudioEffect>(effect);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audio effect {Name}", request.Name);
            return Result.Failure<SoundDesignStudioAudioEffect>($"Failed to create effect: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets an effect by its ID.
    /// </summary>
    /// <param name="effectId">The effect ID.</param>
    /// <param name="effect">The effect if found.</param>
    /// <returns>True if the effect was found, false otherwise.</returns>
    public bool GetEffect(string effectId, out SoundDesignStudioAudioEffect? effect)
    {
        return _audioEffects.TryGetValue(effectId, out effect);
    }

    /// <summary>
    /// Checks if an effect with the specified ID exists.
    /// </summary>
    /// <param name="effectId">The effect ID.</param>
    /// <returns>True if the effect exists, false otherwise.</returns>
    public bool EffectExists(string effectId)
    {
        return _audioEffects.ContainsKey(effectId);
    }

    /// <summary>
    /// Initializes default audio effect presets.
    /// </summary>
    private void InitializeDefaultEffects()
    {
        // Initialize with professional audio effects
        var defaultEffects = new[]
        {
            new SoundDesignStudioAudioEffect
            {
                EffectId = "compressor",
                Name = "Compressor",
                Type = SoundDesignStudioAudioEffectType.Dynamics,
                Parameters = new Dictionary<string, object>
                {
                    ["threshold"] = -20.0f,
                    ["ratio"] = 4.0f,
                    ["attack"] = 10.0f,
                    ["release"] = 100.0f,
                    ["makeup_gain"] = 3.0f
                },
                Enabled = true,
                WetDryMix = 1.0f,
                Bypass = false,
                PresetName = "Vocal Compressor"
            },
            new SoundDesignStudioAudioEffect
            {
                EffectId = "reverb",
                Name = "Reverb",
                Type = SoundDesignStudioAudioEffectType.Reverb,
                Parameters = new Dictionary<string, object>
                {
                    ["room_size"] = 0.5f,
                    ["damping"] = 0.3f,
                    ["wet"] = 0.3f,
                    ["dry"] = 0.7f,
                    ["pre_delay"] = 20.0f
                },
                Enabled = true,
                WetDryMix = 0.3f,
                Bypass = false,
                PresetName = "Concert Hall"
            },
            new SoundDesignStudioAudioEffect
            {
                EffectId = "eq",
                Name = "Parametric EQ",
                Type = SoundDesignStudioAudioEffectType.EQ,
                Parameters = new Dictionary<string, object>
                {
                    ["low_freq"] = 100.0f,
                    ["low_gain"] = 2.0f,
                    ["mid_freq"] = 1000.0f,
                    ["mid_gain"] = -1.5f,
                    ["high_freq"] = 5000.0f,
                    ["high_gain"] = 1.0f
                },
                Enabled = true,
                WetDryMix = 1.0f,
                Bypass = false,
                PresetName = "Vocal EQ"
            },
            new SoundDesignStudioAudioEffect
            {
                EffectId = "distortion",
                Name = "Distortion",
                Type = SoundDesignStudioAudioEffectType.Distortion,
                Parameters = new Dictionary<string, object>
                {
                    ["drive"] = 0.4f,
                    ["tone"] = 0.6f,
                    ["mix"] = 0.2f
                },
                Enabled = true,
                WetDryMix = 0.2f,
                Bypass = false,
                PresetName = "Guitar Overdrive"
            }
        };

        foreach (var effect in defaultEffects)
        {
            _audioEffects[effect.EffectId] = effect;
        }
    }
}
