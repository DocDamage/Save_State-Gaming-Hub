namespace SaveState.Application.Mugen.Models.NarrativeMemory;

/// <summary>
/// Butterfly effect data.
/// </summary>
public class ButterflyEffect
{
    public string EffectId { get; set; } = default!;
    public string SourceCrystalId { get; set; } = default!;
    public IReadOnlyList<string> AffectedCrystals { get; set; } = default!;
    public float Magnitude { get; set; } = default!;
    public int CascadeDepth { get; set; } = default!;
    public DateTime TriggeredAt { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
}

/// <summary>
/// Butterfly effect request.
/// </summary>
public class ButterflyEffectRequest
{
    public float Intensity { get; set; } = default!;
    public int CascadeDepth { get; set; } = default!;
    public string TriggerReason { get; set; } = default!;
}
