namespace SaveState.Application.Mugen.Models.DreamLogic;

/// <summary>
/// Symbolic element data.
/// </summary>
public class SymbolicElement
{
    public string ElementId { get; set; } = default!;
    public SymbolType SymbolType { get; set; } = default!;
    public string RepresentedEmotion { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public System.Numerics.Vector3 Position { get; set; } = default!;
    public DateTime ManifestedAt { get; set; } = default!;
}

/// <summary>
/// Symbolic manifestation data.
/// </summary>
public class SymbolicManifestation
{
    public string ManifestationId { get; set; } = default!;
    public SymbolicElement Element { get; set; } = default!;
    public string TriggerCondition { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}

/// <summary>
/// Symbolic manifestation request.
/// </summary>
public class SymbolicRequest
{
    public SymbolType SymbolType { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public System.Numerics.Vector3 Position { get; set; } = default!;
    public string TriggerCondition { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
}
