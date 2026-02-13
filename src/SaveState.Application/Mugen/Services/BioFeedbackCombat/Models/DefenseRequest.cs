namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Request to power defense with muscles.
/// </summary>
public class DefenseRequest
{
    public string BlockType { get; set; } = default!;
    public float Timing { get; set; } = default!;
    public bool PerfectBlock { get; set; } = default!;
}
