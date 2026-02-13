namespace SaveState.Application.Mugen.Models.RealityWarping;

/// <summary>
/// Represents an environmental change.
/// </summary>
public class EnvironmentChange
{
    public string ChangeId { get; set; } = default!;
    public string AreaId { get; set; } = default!;
    public EnvironmentChangeType ChangeType { get; set; }
    public float Intensity { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Active { get; set; }
}

/// <summary>
/// Types of environmental changes.
/// </summary>
public enum EnvironmentChangeType
{
    Temperature,
    Pressure,
    Humidity,
    LightLevel,
    Weather,
    Atmosphere
}

/// <summary>
/// Represents a terrain modification.
/// </summary>
public class TerrainModification
{
    public string ModId { get; set; } = default!;
    public string AreaId { get; set; } = default!;
    public TerrainModType ModificationType { get; set; }
    public Vector3 Center { get; set; }
    public float Radius { get; set; }
    public float HeightDelta { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Active { get; set; }
}

/// <summary>
/// Types of terrain modifications.
/// </summary>
public enum TerrainModType
{
    Raise,
    Lower,
    Flatten,
    Roughen,
    Invert,
    Smooth
}

/// <summary>
/// Dimensional rift data.
/// </summary>
public class DimensionalRift
{
    public string RiftId { get; set; } = default!;
    public string CreatorId { get; set; } = default!;
    public Vector3 SourcePosition { get; set; }
    public Vector3 TargetPosition { get; set; }
    public string SourceDimension { get; set; } = default!;
    public string TargetDimension { get; set; } = default!;
    public RiftType RiftType { get; set; }
    public float Size { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Active { get; set; }
    public float Stability { get; set; }
    public string EnergySignature { get; set; } = default!;
}

/// <summary>
/// Dimensional rift creation request.
/// </summary>
public class DimensionalRiftRequest
{
    public string CreatorId { get; set; } = default!;
    public Vector3 SourcePosition { get; set; }
    public Vector3 TargetPosition { get; set; }
    public string SourceDimension { get; set; } = default!;
    public string TargetDimension { get; set; } = default!;
    public RiftType RiftType { get; set; }
    public float Size { get; set; }
    public TimeSpan Duration { get; set; }
}
