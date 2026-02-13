namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Audio cue data.
/// </summary>
public class AudioCue
{
    public string SoundId { get; set; } = default!;
    public float Volume { get; set; } = default!;
    public float Pitch { get; set; } = default!;
}
