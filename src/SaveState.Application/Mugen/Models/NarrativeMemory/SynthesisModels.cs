namespace SaveState.Application.Mugen.Models.NarrativeMemory;

/// <summary>
/// Synthesized move data.
/// </summary>
public class SynthesizedMove
{
    public string MoveId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int Power { get; set; } = default!;
    public IReadOnlyList<string> Effects { get; set; } = default!;
    public CrystalRarity Rarity { get; set; } = default!;
    public IReadOnlyList<string> SourceCrystals { get; set; } = default!;
    public DateTime SynthesizedAt { get; set; } = default!;
    public float Stability { get; set; } = default!;
}

/// <summary>
/// Crystal synthesis request.
/// </summary>
public class CrystalSynthesisRequest
{
    public string PlayerId { get; set; } = default!;
    public IReadOnlyList<string> CrystalIds { get; set; } = default!;
    public string DesiredMoveType { get; set; } = default!;
}

/// <summary>
/// Move synthesis request (alias for CrystalSynthesisRequest).
/// </summary>
public class MoveSynthesisRequest : CrystalSynthesisRequest
{
}
