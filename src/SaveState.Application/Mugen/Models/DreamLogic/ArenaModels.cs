namespace SaveState.Application.Mugen.Models.DreamLogic;

/// <summary>
/// Dream arena data model.
/// </summary>
public class DreamArena
{
    public string ArenaId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DreamTheme DreamTheme { get; set; } = default!;
    public ArenaGeometry BaseGeometry { get; set; } = default!;
    public float DreamPotential { get; set; } = default!;
    public float EmotionalResonance { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public float StabilityRating { get; set; } = default!;
}

/// <summary>
/// Arena geometry data.
/// </summary>
public record ArenaGeometry
{
    public System.Numerics.Vector3 Dimensions { get; set; } = default!;
    public System.Numerics.Vector3 GravityDirection { get; set; } = default!;
    public SurfaceType SurfaceType { get; set; } = default!;
    public IReadOnlyList<Boundary> Boundaries { get; set; } = default!;
}

/// <summary>
/// Boundary data.
/// </summary>
public class Boundary
{
    public BoundaryType Type { get; set; } = default!;
    public System.Numerics.Vector3 Position { get; set; } = default!;
    public System.Numerics.Vector3 Normal { get; set; } = default!;
}

/// <summary>
/// Dream arena creation request.
/// </summary>
public class DreamArenaRequest
{
    public string ArenaName { get; set; } = default!;
    public DreamTheme DreamTheme { get; set; } = default!;
    public System.Numerics.Vector3 Dimensions { get; set; } = default!;
    public IReadOnlyList<string> DreamElements { get; set; } = default!;
}

/// <summary>
/// Dream state data.
/// </summary>
public class DreamState
{
    public string ArenaId { get; set; } = default!;
    public ArenaGeometry CurrentGeometry { get; set; } = default!;
    public IReadOnlyList<SurrealElement> ActiveSurrealElements { get; set; } = default!;
    public IReadOnlyList<SymbolicElement> SymbolicManifestations { get; set; } = default!;
    public float EmotionalResonance { get; set; } = default!;
    public float StabilityIndex { get; set; } = default!;
    public DateTime LastUpdated { get; set; } = default!;
}

/// <summary>
/// Arena instability data.
/// </summary>
public class ArenaInstability
{
    public string ArenaId { get; set; } = default!;
    public float StabilityIndex { get; set; } = default!;
    public IReadOnlyList<string> InstabilityFactors { get; set; } = default!;
    public DreamRiskLevel DreamRiskLevel { get; set; } = default!;
    public TimeSpan EstimatedCollapseTime { get; set; } = default!;
    public IReadOnlyList<string> MitigationStrategies { get; set; } = default!;
    public DateTime LastAssessed { get; set; } = default!;
}
