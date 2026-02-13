namespace SaveState.Application.Mugen.Models.RealityWarping;

/// <summary>
/// Represents a temporal effect.
/// </summary>
public class TemporalEffect
{
    public string EffectId { get; set; } = default!;
    public TemporalEffectType Type { get; set; }
    public float TimeScale { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Active { get; set; }
}

/// <summary>
/// Types of temporal effects.
/// </summary>
public enum TemporalEffectType
{
    Slowdown,
    Acceleration,
    Pause,
    Rewind,
    Loop
}

/// <summary>
/// Represents a time modification.
/// </summary>
public class TimeModification
{
    public string ModId { get; set; } = default!;
    public float OriginalTimeScale { get; set; }
    public float NewTimeScale { get; set; }
    public TimeSpan AffectedDuration { get; set; }
    public DateTime AppliedAt { get; set; }
}

/// <summary>
/// Time dilation zone data.
/// </summary>
public class TimeDilationZone
{
    public string ZoneId { get; set; } = default!;
    public string CreatorId { get; set; } = default!;
    public RealityVector3 CenterPosition { get; set; } = default!;
    public float Radius { get; set; }
    public float TimeScale { get; set; }
    public TimeSpan Duration { get; set; }
    public ZoneType ZoneType { get; set; }
    public IReadOnlyList<string> AffectedEntities { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public bool Active { get; set; }
    public float TemporalDistortion { get; set; }
    public bool CausalityEffects { get; set; }
}

/// <summary>
/// Time dilation zone creation request.
/// </summary>
public class TimeDilationRequest
{
    public string CreatorId { get; set; } = default!;
    public RealityVector3 CenterPosition { get; set; } = default!;
    public float Radius { get; set; }
    public float TimeScale { get; set; }
    public TimeSpan Duration { get; set; }
    public ZoneType ZoneType { get; set; }
}

/// <summary>
/// Causality paradox data.
/// </summary>
public class CausalityParadox
{
    public string ParadoxId { get; set; } = default!;
    public ParadoxType ParadoxType { get; set; }
    public string AffectedTimeline { get; set; } = default!;
    public float Severity { get; set; }
    public ParadoxResolution Resolution { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Causality paradox creation request.
/// </summary>
public class CausalityParadoxRequest
{
    public ParadoxType ParadoxType { get; set; }
    public string AffectedTimeline { get; set; } = default!;
    public object ParadoxTrigger { get; set; } = default!;
}

/// <summary>
/// Temporal anomaly statistics.
/// </summary>
public class TemporalAnomalyStats
{
    public int CausalityParadoxes { get; set; }
    public int TimelineBranches { get; set; }
    public int TemporalLoops { get; set; }
    public float ChronalStability { get; set; }
    public int TimeDistortionEvents { get; set; }
}
