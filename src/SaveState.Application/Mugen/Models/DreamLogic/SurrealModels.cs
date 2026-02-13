namespace SaveState.Application.Mugen.Models.DreamLogic;

/// <summary>
/// Surreal element data.
/// </summary>
public class SurrealElement
{
    public string ElementId { get; set; } = default!;
    public SurrealElementType ElementType { get; set; } = default!;
    public System.Numerics.Vector3 Position { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}

/// <summary>
/// Surreal physics event data.
/// </summary>
public class SurrealPhysics
{
    public string PhysicsEventId { get; set; } = default!;
    public SurrealEventTrigger Trigger { get; set; } = default!;
    public IReadOnlyList<SurrealEffect> Effects { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public DateTime TriggeredAt { get; set; } = default!;
}

/// <summary>
/// Surreal effect data.
/// </summary>
public class SurrealEffect
{
    public SurrealEffectType EffectType { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
}

/// <summary>
/// Surreal event trigger.
/// </summary>
public class SurrealEventTrigger
{
    public SurrealEventType EventType { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public object TriggerData { get; set; } = default!;
}

/// <summary>
/// Surreal event data.
/// </summary>
public class SurrealEvent
{
    public string EventId { get; set; } = default!;
    public SurrealEventType EventType { get; set; } = default!;
    public IReadOnlyList<SurrealEffect> Effects { get; set; } = default!;
    public float Probability { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}
