namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Animation library data.
/// </summary>
public class AnimationLibrary
{
    public string Theme { get; set; } = default!;
    public IReadOnlyDictionary<string, AnimationData> Animations { get; set; } = default!;
}
