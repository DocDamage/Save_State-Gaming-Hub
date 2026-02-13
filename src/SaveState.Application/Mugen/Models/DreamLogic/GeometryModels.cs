namespace SaveState.Application.Mugen.Models.DreamLogic;

/// <summary>
/// Impossible geometry transformation result.
/// </summary>
public class ImpossibleGeometry
{
    public string TransformationId { get; set; } = default!;
    public GeometryType GeometryType { get; set; } = default!;
    public System.Numerics.Vector3 AffectedArea { get; set; } = default!;
    public IReadOnlyDictionary<string, object> TransformationParameters { get; set; } = default!;
    public ArenaGeometry ResultingGeometry { get; set; } = default!;
    public float StabilityChange { get; set; } = default!;
    public DateTime AppliedAt { get; set; } = default!;
}

/// <summary>
/// Geometry transformation request.
/// </summary>
public class GeometryTransformationRequest
{
    public GeometryType TransformationType { get; set; } = default!;
    public System.Numerics.Vector3 AffectedArea { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
}
