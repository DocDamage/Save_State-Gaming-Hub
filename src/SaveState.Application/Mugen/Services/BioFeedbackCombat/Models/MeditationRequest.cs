namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Request to enter meditation mode.
/// </summary>
public class MeditationRequest
{
    public MeditationTechnique Technique { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public float FocusTarget { get; set; } = default!;
}
