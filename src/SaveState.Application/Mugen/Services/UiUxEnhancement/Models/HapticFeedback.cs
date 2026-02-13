namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Haptic feedback data.
/// </summary>
public class HapticFeedback
{
    public string Pattern { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public float Duration { get; set; } = default!;
    public int Frequency { get; set; } = default!;
}
