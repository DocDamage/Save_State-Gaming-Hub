namespace SaveState.Application.Mugen.Services.UiUxEnhancement.Engines;

using Microsoft.Extensions.Logging;

/// <summary>
/// Engine responsible for generating visual feedback, audio cues, and haptic feedback.
/// </summary>
public class FeedbackEngine
{
    private readonly ILogger<FeedbackEngine>? _logger;
    private readonly Dictionary<string, AnimationLibrary> _animationCache = new();
    private readonly Dictionary<string, SoundLibrary> _soundCache = new();
    private readonly Dictionary<string, ParticleEffectLibrary> _particleCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedbackEngine"/> class.
    /// </summary>
    public FeedbackEngine()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedbackEngine"/> class with a logger.
    /// </summary>
    public FeedbackEngine(ILogger<FeedbackEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates feedback rules for enabled mechanics.
    /// </summary>
    /// <param name="enabledMechanics">List of enabled mechanic identifiers.</param>
    /// <returns>List of feedback rules.</returns>
    public IReadOnlyList<FeedbackRule> GenerateFeedbackRules(IEnumerable<string> enabledMechanics)
    {
        var rules = new List<FeedbackRule>();
        var mechanicsList = enabledMechanics.ToList();

        _logger?.LogDebug("Generating feedback rules for {Count} mechanics", mechanicsList.Count);

        // Core feedback rules
        rules.Add(new FeedbackRule
        {
            Id = "hit_confirm",
            Mechanic = "combat",
            Trigger = "hit_confirm",
            FeedbackType = "visual",
            Intensity = 0.7f,
            Duration = 0.3f
        });

        rules.Add(new FeedbackRule
        {
            Id = "damage_taken",
            Mechanic = "combat",
            Trigger = "damage_taken",
            FeedbackType = "screen_shake",
            Intensity = 0.5f,
            Duration = 0.2f
        });

        // Generate mechanic-specific rules
        foreach (var mechanic in mechanicsList)
        {
            var mechanicRules = GenerateRulesForMechanic(mechanic);
            rules.AddRange(mechanicRules);
        }

        _logger?.LogInformation("Generated {Count} feedback rules", rules.Count);
        return rules;
    }

    /// <summary>
    /// Loads the animation library for the specified theme.
    /// </summary>
    /// <param name="theme">The theme identifier.</param>
    /// <returns>The animation library.</returns>
    public AnimationLibrary LoadAnimationLibrary(string theme)
    {
        _logger?.LogDebug("Loading animation library for theme: {Theme}", theme);

        if (_animationCache.TryGetValue(theme, out var cached))
        {
            _logger?.LogDebug("Returning cached animation library for theme: {Theme}", theme);
            return cached;
        }

        var animations = new Dictionary<string, AnimationData>
        {
            ["fade_in"] = new()
            {
                Id = "fade_in",
                Type = "opacity",
                Duration = 0.3f,
                Easing = "ease_out"
            },
            ["fade_out"] = new()
            {
                Id = "fade_out",
                Type = "opacity",
                Duration = 0.2f,
                Easing = "ease_in"
            },
            ["slide_in"] = new()
            {
                Id = "slide_in",
                Type = "transform",
                Duration = 0.4f,
                Easing = "ease_out_cubic"
            },
            ["slide_out"] = new()
            {
                Id = "slide_out",
                Type = "transform",
                Duration = 0.3f,
                Easing = "ease_in_cubic"
            },
            ["scale_up"] = new()
            {
                Id = "scale_up",
                Type = "scale",
                Duration = 0.2f,
                Easing = "ease_out_back"
            },
            ["pulse"] = new()
            {
                Id = "pulse",
                Type = "scale",
                Duration = 0.5f,
                Easing = "ease_in_out"
            },
            ["shake"] = new()
            {
                Id = "shake",
                Type = "transform",
                Duration = 0.3f,
                Easing = "linear"
            },
            ["flash"] = new()
            {
                Id = "flash",
                Type = "color",
                Duration = 0.15f,
                Easing = "linear"
            }
        };

        // Add theme-specific animations
        AddThemeAnimations(animations, theme);

        var library = new AnimationLibrary
        {
            Theme = theme,
            Animations = animations
        };

        _animationCache[theme] = library;
        _logger?.LogInformation("Loaded animation library with {Count} animations for theme: {Theme}",
            animations.Count, theme);

        return library;
    }

    /// <summary>
    /// Loads the sound library.
    /// </summary>
    /// <param name="audioEnabled">Whether audio is enabled.</param>
    /// <returns>The sound library.</returns>
    public SoundLibrary LoadSoundLibrary(bool audioEnabled)
    {
        _logger?.LogDebug("Loading sound library (audio enabled: {AudioEnabled})", audioEnabled);

        if (!audioEnabled)
        {
            return new SoundLibrary
            {
                Theme = "silent",
                Sounds = new Dictionary<string, SoundData>()
            };
        }

        if (_soundCache.TryGetValue("default", out var cached))
        {
            return cached;
        }

        var sounds = new Dictionary<string, SoundData>
        {
            ["ui_hover"] = new()
            {
                Id = "ui_hover",
                FilePath = "sounds/ui/hover.wav",
                Volume = 0.5f
            },
            ["ui_select"] = new()
            {
                Id = "ui_select",
                FilePath = "sounds/ui/select.wav",
                Volume = 0.7f
            },
            ["ui_cancel"] = new()
            {
                Id = "ui_cancel",
                FilePath = "sounds/ui/cancel.wav",
                Volume = 0.6f
            },
            ["ui_error"] = new()
            {
                Id = "ui_error",
                FilePath = "sounds/ui/error.wav",
                Volume = 0.8f
            },
            ["hit_light"] = new()
            {
                Id = "hit_light",
                FilePath = "sounds/combat/hit_light.wav",
                Volume = 0.6f
            },
            ["hit_heavy"] = new()
            {
                Id = "hit_heavy",
                FilePath = "sounds/combat/hit_heavy.wav",
                Volume = 0.8f
            },
            ["block"] = new()
            {
                Id = "block",
                FilePath = "sounds/combat/block.wav",
                Volume = 0.7f
            },
            ["parry"] = new()
            {
                Id = "parry",
                FilePath = "sounds/combat/parry.wav",
                Volume = 0.9f
            },
            ["super_ready"] = new()
            {
                Id = "super_ready",
                FilePath = "sounds/combat/super_ready.wav",
                Volume = 1.0f
            },
            ["notification"] = new()
            {
                Id = "notification",
                FilePath = "sounds/ui/notification.wav",
                Volume = 0.6f
            }
        };

        var library = new SoundLibrary
        {
            Theme = "default",
            Sounds = sounds
        };

        _soundCache["default"] = library;
        _logger?.LogInformation("Loaded sound library with {Count} sounds", sounds.Count);

        return library;
    }

    /// <summary>
    /// Loads the particle effects library.
    /// </summary>
    /// <param name="particlesEnabled">Whether particles are enabled.</param>
    /// <returns>The particle effect library.</returns>
    public ParticleEffectLibrary LoadParticleEffects(bool particlesEnabled)
    {
        _logger?.LogDebug("Loading particle effects (enabled: {ParticlesEnabled})", particlesEnabled);

        if (!particlesEnabled)
        {
            return new ParticleEffectLibrary
            {
                Theme = "none",
                Effects = new Dictionary<string, ParticleEffect>()
            };
        }

        if (_particleCache.TryGetValue("default", out var cached))
        {
            return cached;
        }

        var effects = new Dictionary<string, ParticleEffect>
        {
            ["hit_spark"] = new()
            {
                Id = "hit_spark",
                Type = "burst",
                Color = "#FFFF00",
                ParticleCount = 8,
                Duration = 0.3f
            },
            ["hit_blood"] = new()
            {
                Id = "hit_blood",
                Type = "spray",
                Color = "#FF0000",
                ParticleCount = 12,
                Duration = 0.5f
            },
            ["block_spark"] = new()
            {
                Id = "block_spark",
                Type = "burst",
                Color = "#00FFFF",
                ParticleCount = 6,
                Duration = 0.2f
            },
            ["parry_flash"] = new()
            {
                Id = "parry_flash",
                Type = "ring",
                Color = "#FFD700",
                ParticleCount = 16,
                Duration = 0.4f
            },
            ["super_charge"] = new()
            {
                Id = "super_charge",
                Type = "aura",
                Color = "#FF00FF",
                ParticleCount = 32,
                Duration = 1.0f
            },
            ["dash_trail"] = new()
            {
                Id = "dash_trail",
                Type = "trail",
                Color = "#00FF00",
                ParticleCount = 20,
                Duration = 0.6f
            },
            ["buff_glow"] = new()
            {
                Id = "buff_glow",
                Type = "aura",
                Color = "#00FF88",
                ParticleCount = 24,
                Duration = 2.0f
            },
            ["debuff_smoke"] = new()
            {
                Id = "debuff_smoke",
                Type = "smoke",
                Color = "#8800FF",
                ParticleCount = 15,
                Duration = 1.5f
            }
        };

        var library = new ParticleEffectLibrary
        {
            Theme = "default",
            Effects = effects
        };

        _particleCache["default"] = library;
        _logger?.LogInformation("Loaded particle effect library with {Count} effects", effects.Count);

        return library;
    }

    /// <summary>
    /// Generates visual effects based on a feedback trigger.
    /// </summary>
    /// <param name="trigger">The feedback trigger.</param>
    /// <returns>List of visual effects.</returns>
    public IReadOnlyList<VisualEffect> GenerateVisualEffects(FeedbackTrigger trigger)
    {
        var effects = new List<VisualEffect>();

        _logger?.LogDebug("Generating visual effects for trigger: {TriggerType}", trigger.TriggerType);

        switch (trigger.TriggerType.ToLowerInvariant())
        {
            case "hit_confirm":
            case "hit":
                effects.Add(new VisualEffect
                {
                    Type = "flash",
                    Color = "#FFFF00",
                    Intensity = trigger.Intensity,
                    Duration = 0.1f
                });
                effects.Add(new VisualEffect
                {
                    Type = "particle_burst",
                    Color = "#FF8800",
                    Intensity = trigger.Intensity,
                    Duration = 0.3f
                });
                break;

            case "critical_hit":
            case "heavy_hit":
                effects.Add(new VisualEffect
                {
                    Type = "screen_shake",
                    Color = "#FF0000",
                    Intensity = trigger.Intensity * 1.5f,
                    Duration = 0.4f
                });
                effects.Add(new VisualEffect
                {
                    Type = "slow_motion",
                    Color = "#FFFFFF",
                    Intensity = trigger.Intensity,
                    Duration = 0.5f
                });
                break;

            case "block":
                effects.Add(new VisualEffect
                {
                    Type = "shield_flash",
                    Color = "#00FFFF",
                    Intensity = trigger.Intensity,
                    Duration = 0.2f
                });
                break;

            case "parry":
                effects.Add(new VisualEffect
                {
                    Type = "time_freeze",
                    Color = "#FFD700",
                    Intensity = trigger.Intensity,
                    Duration = 0.3f
                });
                effects.Add(new VisualEffect
                {
                    Type = "ring_expansion",
                    Color = "#FFD700",
                    Intensity = trigger.Intensity,
                    Duration = 0.5f
                });
                break;

            case "super_activated":
                effects.Add(new VisualEffect
                {
                    Type = "fullscreen_flash",
                    Color = "#FF00FF",
                    Intensity = trigger.Intensity,
                    Duration = 0.8f
                });
                effects.Add(new VisualEffect
                {
                    Type = "character_aura",
                    Color = "#FF00FF",
                    Intensity = trigger.Intensity,
                    Duration = 3.0f
                });
                break;

            case "combo_milestone":
                effects.Add(new VisualEffect
                {
                    Type = "text_popup",
                    Color = "#FF8800",
                    Intensity = trigger.Intensity,
                    Duration = 1.0f
                });
                break;

            case "hud_update":
                effects.Add(new VisualEffect
                {
                    Type = "fade_transition",
                    Color = "#FFFFFF",
                    Intensity = trigger.Intensity * 0.5f,
                    Duration = 0.3f
                });
                break;

            case "damage_taken":
                effects.Add(new VisualEffect
                {
                    Type = "red_flash",
                    Color = "#FF0000",
                    Intensity = trigger.Intensity * 0.5f,
                    Duration = 0.2f
                });
                break;

            default:
                // Generic visual feedback
                effects.Add(new VisualEffect
                {
                    Type = "generic",
                    Color = "#FFFFFF",
                    Intensity = trigger.Intensity,
                    Duration = 0.3f
                });
                break;
        }

        return effects;
    }

    /// <summary>
    /// Generates audio cues based on a feedback trigger.
    /// </summary>
    /// <param name="trigger">The feedback trigger.</param>
    /// <returns>List of audio cues.</returns>
    public IReadOnlyList<AudioCue> GenerateAudioCues(FeedbackTrigger trigger)
    {
        var cues = new List<AudioCue>();

        _logger?.LogDebug("Generating audio cues for trigger: {TriggerType}", trigger.TriggerType);

        switch (trigger.TriggerType.ToLowerInvariant())
        {
            case "hit_confirm":
            case "hit":
                cues.Add(new AudioCue
                {
                    SoundId = "hit_light",
                    Volume = 0.6f * trigger.Intensity,
                    Pitch = 1.0f
                });
                break;

            case "critical_hit":
            case "heavy_hit":
                cues.Add(new AudioCue
                {
                    SoundId = "hit_heavy",
                    Volume = 0.9f * trigger.Intensity,
                    Pitch = 0.9f
                });
                break;

            case "block":
                cues.Add(new AudioCue
                {
                    SoundId = "block",
                    Volume = 0.7f * trigger.Intensity,
                    Pitch = 1.0f
                });
                break;

            case "parry":
                cues.Add(new AudioCue
                {
                    SoundId = "parry",
                    Volume = 1.0f * trigger.Intensity,
                    Pitch = 1.1f
                });
                break;

            case "super_activated":
                cues.Add(new AudioCue
                {
                    SoundId = "super_ready",
                    Volume = 1.0f * trigger.Intensity,
                    Pitch = 1.0f
                });
                break;

            case "ui_select":
                cues.Add(new AudioCue
                {
                    SoundId = "ui_select",
                    Volume = 0.7f * trigger.Intensity,
                    Pitch = 1.0f
                });
                break;

            case "ui_hover":
                cues.Add(new AudioCue
                {
                    SoundId = "ui_hover",
                    Volume = 0.4f * trigger.Intensity,
                    Pitch = 1.0f
                });
                break;

            case "ui_cancel":
                cues.Add(new AudioCue
                {
                    SoundId = "ui_cancel",
                    Volume = 0.6f * trigger.Intensity,
                    Pitch = 0.95f
                });
                break;

            case "notification":
                cues.Add(new AudioCue
                {
                    SoundId = "notification",
                    Volume = 0.6f * trigger.Intensity,
                    Pitch = 1.0f
                });
                break;
        }

        return cues;
    }

    /// <summary>
    /// Generates haptic feedback based on a feedback trigger.
    /// </summary>
    /// <param name="trigger">The feedback trigger.</param>
    /// <returns>List of haptic feedback patterns.</returns>
    public IReadOnlyList<HapticFeedback> GenerateHapticFeedback(FeedbackTrigger trigger)
    {
        var feedback = new List<HapticFeedback>();

        _logger?.LogDebug("Generating haptic feedback for trigger: {TriggerType}", trigger.TriggerType);

        switch (trigger.TriggerType.ToLowerInvariant())
        {
            case "hit_confirm":
            case "hit":
                feedback.Add(new HapticFeedback
                {
                    Pattern = "short_pulse",
                    Intensity = 0.5f * trigger.Intensity,
                    Duration = 0.1f,
                    Frequency = 100
                });
                break;

            case "critical_hit":
            case "heavy_hit":
                feedback.Add(new HapticFeedback
                {
                    Pattern = "strong_impact",
                    Intensity = 0.9f * trigger.Intensity,
                    Duration = 0.3f,
                    Frequency = 150
                });
                break;

            case "block":
                feedback.Add(new HapticFeedback
                {
                    Pattern = "dull_thud",
                    Intensity = 0.6f * trigger.Intensity,
                    Duration = 0.15f,
                    Frequency = 80
                });
                break;

            case "parry":
                feedback.Add(new HapticFeedback
                {
                    Pattern = "sharp_tick",
                    Intensity = 0.8f * trigger.Intensity,
                    Duration = 0.05f,
                    Frequency = 200
                });
                feedback.Add(new HapticFeedback
                {
                    Pattern = "resonance",
                    Intensity = 0.4f * trigger.Intensity,
                    Duration = 0.2f,
                    Frequency = 120
                });
                break;

            case "super_activated":
                feedback.Add(new HapticFeedback
                {
                    Pattern = "rumble_buildup",
                    Intensity = 0.7f * trigger.Intensity,
                    Duration = 0.5f,
                    Frequency = 60
                });
                feedback.Add(new HapticFeedback
                {
                    Pattern = "explosion",
                    Intensity = 1.0f * trigger.Intensity,
                    Duration = 0.4f,
                    Frequency = 180
                });
                break;

            case "damage_taken":
                feedback.Add(new HapticFeedback
                {
                    Pattern = "damage_pulse",
                    Intensity = 0.7f * trigger.Intensity,
                    Duration = 0.2f,
                    Frequency = 90
                });
                break;

            case "combo_milestone":
                feedback.Add(new HapticFeedback
                {
                    Pattern = "rapid_taps",
                    Intensity = 0.5f * trigger.Intensity,
                    Duration = 0.15f,
                    Frequency = 150
                });
                break;

            case "ui_select":
                feedback.Add(new HapticFeedback
                {
                    Pattern = "light_tick",
                    Intensity = 0.3f * trigger.Intensity,
                    Duration = 0.05f,
                    Frequency = 80
                });
                break;

            case "ui_hover":
                feedback.Add(new HapticFeedback
                {
                    Pattern = "micro_pulse",
                    Intensity = 0.15f * trigger.Intensity,
                    Duration = 0.02f,
                    Frequency = 60
                });
                break;
        }

        return feedback;
    }

    private IEnumerable<FeedbackRule> GenerateRulesForMechanic(string mechanic)
    {
        var rules = new List<FeedbackRule>();

        switch (mechanic.ToLowerInvariant())
        {
            case "parry":
                rules.Add(new FeedbackRule
                {
                    Id = "parry_success",
                    Mechanic = mechanic,
                    Trigger = "parry_success",
                    FeedbackType = "visual",
                    Intensity = 0.9f,
                    Duration = 0.5f
                });
                break;

            case "stance":
                rules.Add(new FeedbackRule
                {
                    Id = "stance_change",
                    Mechanic = mechanic,
                    Trigger = "stance_changed",
                    FeedbackType = "audio",
                    Intensity = 0.5f,
                    Duration = 0.3f
                });
                break;

            case "combo":
                rules.Add(new FeedbackRule
                {
                    Id = "combo_increase",
                    Mechanic = mechanic,
                    Trigger = "combo_increased",
                    FeedbackType = "visual",
                    Intensity = 0.6f,
                    Duration = 0.4f
                });
                rules.Add(new FeedbackRule
                {
                    Id = "combo_break",
                    Mechanic = mechanic,
                    Trigger = "combo_broken",
                    FeedbackType = "haptic",
                    Intensity = 0.4f,
                    Duration = 0.2f
                });
                break;

            case "meter":
                rules.Add(new FeedbackRule
                {
                    Id = "meter_full",
                    Mechanic = mechanic,
                    Trigger = "meter_maxed",
                    FeedbackType = "visual",
                    Intensity = 1.0f,
                    Duration = 1.0f
                });
                break;

            case "buff":
                rules.Add(new FeedbackRule
                {
                    Id = "buff_applied",
                    Mechanic = mechanic,
                    Trigger = "buff_applied",
                    FeedbackType = "visual",
                    Intensity = 0.7f,
                    Duration = 0.5f
                });
                rules.Add(new FeedbackRule
                {
                    Id = "buff_expired",
                    Mechanic = mechanic,
                    Trigger = "buff_expired",
                    FeedbackType = "audio",
                    Intensity = 0.4f,
                    Duration = 0.3f
                });
                break;

            case "cooldown":
                rules.Add(new FeedbackRule
                {
                    Id = "cooldown_ready",
                    Mechanic = mechanic,
                    Trigger = "cooldown_complete",
                    FeedbackType = "visual",
                    Intensity = 0.5f,
                    Duration = 0.4f
                });
                break;
        }

        return rules;
    }

    private void AddThemeAnimations(Dictionary<string, AnimationData> animations, string theme)
    {
        switch (theme.ToLowerInvariant())
        {
            case "modern":
                animations["slide_in"] = new AnimationData
                {
                    Id = "slide_in",
                    Type = "transform",
                    Duration = 0.25f,
                    Easing = "ease_out_expo"
                };
                break;

            case "retro":
                animations["fade_in"] = new AnimationData
                {
                    Id = "fade_in",
                    Type = "opacity",
                    Duration = 0.05f,
                    Easing = "step"
                };
                break;

            case "minimal":
                animations["scale_up"] = new AnimationData
                {
                    Id = "scale_up",
                    Type = "scale",
                    Duration = 0.15f,
                    Easing = "ease_out_quad"
                };
                break;
        }
    }
}
