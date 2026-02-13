namespace SaveState.Application.Mugen.Models.RealityWarping;

/// <summary>
/// Represents a distortion effect applied to reality.
/// </summary>
public class DistortionEffect
{
    public string EffectId { get; set; } = default!;
    public DistortionType Type { get; set; }
    public float Magnitude { get; set; }
    public Vector3 Center { get; set; }
    public float Radius { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Active { get; set; }
}

/// <summary>
/// Represents a zone of distortion.
/// </summary>
public class DistortionZone
{
    public string ZoneId { get; set; } = default!;
    public string CreatorId { get; set; } = default!;
    public RealityVector3 CenterPosition { get; set; } = default!;
    public float Radius { get; set; }
    public DistortionType Type { get; set; }
    public float Intensity { get; set; }
    public TimeSpan Duration { get; set; }
    public IReadOnlyList<string> AffectedEntities { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public bool Active { get; set; }
    public float DistortionFactor { get; set; }
}

/// <summary>
/// Matter phasing effect data.
/// </summary>
public class PhasingEffect
{
    public string EntityId { get; set; } = default!;
    public PhasingType PhasingType { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Intangible { get; set; }
    public DateTime AppliedAt { get; set; }
}

/// <summary>
/// Request for matter phasing.
/// </summary>
public class PhasingRequest
{
    public PhasingType PhasingType { get; set; }
    public TimeSpan Duration { get; set; }
    public float IntangibilityLevel { get; set; }
}
