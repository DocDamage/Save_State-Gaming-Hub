namespace SaveState.Application.Mugen.Models.RealityWarping;

/// <summary>
/// Represents a physics modification.
/// </summary>
public class PhysicsModification
{
    public string ModId { get; set; } = default!;
    public PhysicsModType Type { get; set; }
    public float Value { get; set; }
    public string TargetEntityId { get; set; } = default!;
    public TimeSpan Duration { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Active { get; set; }
}

/// <summary>
/// Represents a gravity change.
/// </summary>
public class GravityChange
{
    public float OriginalGravity { get; set; }
    public float NewGravity { get; set; }
    public Vector3 Direction { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime AppliedAt { get; set; }
}

/// <summary>
/// Gravity well data.
/// </summary>
public class GravityWell
{
    public string WellId { get; set; } = default!;
    public string CreatorId { get; set; } = default!;
    public RealityVector3 Position { get; set; } = default!;
    public float Strength { get; set; }
    public float Radius { get; set; }
    public TimeSpan Duration { get; set; }
    public WellType WellType { get; set; }
    public IReadOnlyList<string> AffectedEntities { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public bool Active { get; set; }
    public float GravitationalPull { get; set; }
    public bool OrbitalMechanics { get; set; }
}

/// <summary>
/// Gravity well creation request.
/// </summary>
public class GravityWellRequest
{
    public string CreatorId { get; set; } = default!;
    public RealityVector3 Position { get; set; } = default!;
    public float Strength { get; set; }
    public float Radius { get; set; }
    public TimeSpan Duration { get; set; }
    public WellType WellType { get; set; }
}

/// <summary>
/// Metrics for physics distortion analysis.
/// </summary>
public class PhysicsDistortionMetrics
{
    public int GravityWellsActive { get; set; }
    public float AverageGravitationalPull { get; set; }
    public int TimeZonesActive { get; set; }
    public float AverageTimeDilation { get; set; }
    public int PhysicsAnomalies { get; set; }
}
