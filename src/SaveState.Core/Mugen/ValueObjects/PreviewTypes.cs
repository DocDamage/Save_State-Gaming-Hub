using SaveState.Core.Mugen.Services;

namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Data required for move preview visualization.
/// </summary>
public sealed record MovePreviewData(
    IReadOnlyList<PreviewFrame> Frames,
    MoveProperties Properties,
    IReadOnlyDictionary<string, string> Metadata);

/// <summary>
/// A single frame of preview data.
/// </summary>
public sealed record PreviewFrame(
    int FrameNumber,
    string Sprite,
    Position Position,
    IReadOnlyList<Hitbox> Hitboxes,
    IReadOnlyList<Hurtbox> Hurtboxes,
    IReadOnlyList<Projectile> Projectiles,
    IReadOnlyList<ParticleEffect> Effects);

/// <summary>
/// Options for preview generation.
/// </summary>
public sealed record PreviewOptions(
    int StartFrame,
    int EndFrame,
    bool ShowHitboxes = true,
    bool ShowHurtboxes = true,
    bool ShowProjectiles = true,
    bool ShowEffects = true,
    PreviewQuality Quality = PreviewQuality.Medium);

/// <summary>
/// Quality of the generated preview.
/// </summary>
public enum PreviewQuality
{
    Low,
    Medium,
    High,
    Ultra
}
