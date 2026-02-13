namespace SaveState.Application.Mugen.Models.RealityWarping;

/// <summary>
/// Represents the current state of reality in an area.
/// </summary>
public class RealityState
{
    public string AreaId { get; set; } = default!;
    public float DistortionLevel { get; set; }
    public float StabilityIndex { get; set; }
    public int ActiveWarps { get; set; }
    public IReadOnlyList<string> Anomalies { get; set; } = default!;
    public DateTime MeasuredAt { get; set; }
}

/// <summary>
/// Represents an active reality effect.
/// </summary>
public class RealityEffect
{
    public string EffectId { get; set; } = default!;
    public RealityType Type { get; set; }
    public float Intensity { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Active { get; set; }
}

/// <summary>
/// Represents a reality warp operation.
/// </summary>
public class RealityWarp
{
    public string WarpId { get; set; } = default!;
    public string InitiatorId { get; set; } = default!;
    public WarpType WarpType { get; set; }
    public Vector3 AffectedArea { get; set; }
    public float Intensity { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Active { get; set; }
    public float RealityDistortion { get; set; }
    public float StabilityIndex { get; set; }
}

/// <summary>
/// Request to create a reality warp.
/// </summary>
public class RealityWarpRequest
{
    public string InitiatorId { get; set; } = default!;
    public WarpType WarpType { get; set; }
    public Vector3 AffectedArea { get; set; }
    public float Intensity { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Analytics data for reality warping operations.
/// </summary>
public class RealityWarpingAnalytics
{
    public TimeSpan Period { get; set; }
    public int TotalGravityWells { get; set; }
    public int TotalTimeZones { get; set; }
    public int TotalRifts { get; set; }
    public int TotalWarps { get; set; }
    public float RealityStabilityIndex { get; set; }
    public PhysicsDistortionMetrics PhysicsDistortionMetrics { get; set; } = default!;
    public TemporalAnomalyStats TemporalAnomalyStats { get; set; } = default!;
    public float DimensionalIntegrity { get; set; }
    public int CausalityViolationCount { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// 3D vector for positions.
/// </summary>
public class RealityVector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}
