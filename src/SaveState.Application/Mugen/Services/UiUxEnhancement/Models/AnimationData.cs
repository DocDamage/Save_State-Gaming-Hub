namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Animation data.
/// </summary>
public class AnimationData
{
    public string Id { get; set; } = default!;
    public string Type { get; set; } = default!;
    public float Duration { get; set; } = default!;
    public string Easing { get; set; } = default!;
}
