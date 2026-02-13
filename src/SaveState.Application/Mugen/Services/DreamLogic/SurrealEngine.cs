using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.DreamLogic;
namespace SaveState.Application.Mugen.Services.DreamLogic;

/// <summary>
/// Engine for managing surreal elements and events.
/// </summary>
public class SurrealEngine
{
    private readonly ILogger<SurrealEngine> _logger;
    private readonly Random _random = new();

    public SurrealEngine(ILogger<SurrealEngine> logger)
    {
        _logger = logger;
    }

    public Task<SurrealElement> GenerateSurrealElementAsync(SurrealElementType type, System.Numerics.Vector3 position, CancellationToken ct = default)
    {
        _logger.LogDebug("Generating surreal element of type {ElementType}", type);

        var element = new SurrealElement
        {
            ElementId = Guid.NewGuid().ToString(),
            ElementType = type,
            Position = position,
            Intensity = _random.NextSingle(),
            Duration = TimeSpan.FromMinutes(_random.Next(1, 10)),
            CreatedAt = DateTime.UtcNow
        };

        return Task.FromResult(element);
    }

    public Task<SurrealEvent> TriggerSurrealEventAsync(SurrealEventType eventType, float intensity, CancellationToken ct = default)
    {
        _logger.LogInformation("Triggering surreal event: {EventType} at intensity {Intensity}", eventType, intensity);

        var surrealEvent = new SurrealEvent
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = eventType,
            Effects = GenerateEffectsForEvent(eventType, intensity),
            Probability = intensity,
            GeneratedAt = DateTime.UtcNow
        };

        return Task.FromResult(surrealEvent);
    }

    public Task<float> CalculateSurrealIntensityAsync(DreamState state, CancellationToken ct = default)
    {
        var baseIntensity = state.ActiveSurrealElements.Count * 0.1f;
        var stabilityFactor = 1.0f - state.StabilityIndex;
        return Task.FromResult(Math.Min(baseIntensity * stabilityFactor, 1.0f));
    }

    public Task<SurrealPhysics> TriggerPhysicsAsync(DreamState state, SurrealEventTrigger trigger, CancellationToken ct = default)
    {
        _logger.LogInformation("Triggering surreal physics in arena {ArenaId}", state.ArenaId);

        var physics = new SurrealPhysics
        {
            PhysicsEventId = Guid.NewGuid().ToString(),
            Trigger = trigger,
            Effects = GenerateEffectsForEvent(trigger.EventType, trigger.Intensity),
            Duration = TimeSpan.FromSeconds(30),
            Intensity = trigger.Intensity,
            TriggeredAt = DateTime.UtcNow
        };

        return Task.FromResult(physics);
    }

    public Task<SurrealEvent> GenerateRandomEventAsync(DreamState state, CancellationToken ct = default)
    {
        var eventTypes = Enum.GetValues<SurrealEventType>();
        var randomType = eventTypes[_random.Next(eventTypes.Length)];
        return TriggerSurrealEventAsync(randomType, _random.NextSingle(), ct);
    }

    public Task ApplySurrealEffectAsync(DreamState state, SurrealEffect effect, CancellationToken ct = default)
    {
        switch (effect.EffectType)
        {
            case SurrealEffectType.GravityShift:
                if (effect.Parameters.ContainsKey("direction"))
                {
                    state.CurrentGeometry = state.CurrentGeometry with
                    {
                        GravityDirection = new System.Numerics.Vector3(0f, 1f, 0f)
                    };
                }
                break;

            case SurrealEffectType.ObjectManifestation:
                if (effect.Parameters.ContainsKey("object") && effect.Parameters["object"] is SurrealElement element)
                {
                    var activeElements = state.ActiveSurrealElements?.ToList() ?? new List<SurrealElement>();
                    activeElements.Add(element);
                    state.ActiveSurrealElements = activeElements;
                }
                break;

            case SurrealEffectType.TimeDistortion:
                break;
        }

        return Task.CompletedTask;
    }

    public Task ApplySurrealEventAsync(DreamState state, SurrealEvent surrealEvent, CancellationToken ct = default)
    {
        foreach (var effect in surrealEvent.Effects)
        {
            ApplySurrealEffectAsync(state, effect, ct);
        }
        return Task.CompletedTask;
    }

    private List<SurrealEffect> GenerateEffectsForEvent(SurrealEventType eventType, float intensity)
    {
        var effects = new List<SurrealEffect>();

        switch (eventType)
        {
            case SurrealEventType.CombatIntensity:
                effects.Add(new SurrealEffect
                {
                    EffectType = SurrealEffectType.GravityShift,
                    Parameters = new Dictionary<string, object> { ["direction"] = "inverted" },
                    Duration = TimeSpan.FromSeconds(30 * intensity)
                });
                break;
            case SurrealEventType.TimeAnomaly:
                effects.Add(new SurrealEffect
                {
                    EffectType = SurrealEffectType.TimeDistortion,
                    Parameters = new Dictionary<string, object> { ["factor"] = 0.5f },
                    Duration = TimeSpan.FromSeconds(60 * intensity)
                });
                break;
            case SurrealEventType.EmotionalPeak:
                effects.Add(new SurrealEffect
                {
                    EffectType = SurrealEffectType.ObjectManifestation,
                    Parameters = new Dictionary<string, object> { ["object"] = "emotional_echo" },
                    Duration = TimeSpan.FromSeconds(45 * intensity)
                });
                break;
        }

        return effects;
    }
}

/// <summary>
/// Legacy alias for backward compatibility.
/// </summary>
public class DreamLogicArenaServiceSurrealEngine : SurrealEngine
{
    public DreamLogicArenaServiceSurrealEngine(ILogger<SurrealEngine> logger) : base(logger) { }
}
