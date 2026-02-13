namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Particle effect data.
/// </summary>
public class ParticleEffect
{
    public string Id { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Color { get; set; } = default!;
    public int ParticleCount { get; set; } = default!;
    public float Duration { get; set; } = default!;
}
