namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Particle effect library data.
/// </summary>
public class ParticleEffectLibrary
{
    public string Theme { get; set; } = default!;
    public IReadOnlyDictionary<string, ParticleEffect> Effects { get; set; } = default!;
}
