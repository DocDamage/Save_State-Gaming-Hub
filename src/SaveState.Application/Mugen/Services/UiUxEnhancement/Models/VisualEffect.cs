namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Visual effect data.
/// </summary>
public class VisualEffect
{
    public string Type { get; set; } = default!;
    public string Color { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public float Duration { get; set; } = default!;
}
