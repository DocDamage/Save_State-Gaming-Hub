using SaveState.Core.Mugen.CharacterFrameAnalysis;

namespace SaveState.Infrastructure.Mugen.Services;

/// <summary>
/// Move type classification.
/// </summary>
public enum MoveType
{
    StandingNormal,
    CrouchingNormal,
    AirNormal,
    CommandMove,
    SpecialMove,
    SuperMove,
    Throw
}

/// <summary>
/// Database entity for character frame data.
/// </summary>
public class CharacterFrameDataEntity
{
    public int Id { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public int Health { get; set; }
    public int WalkSpeed { get; set; }
    public int BackWalkSpeed { get; set; }
    public int DashDistance { get; set; }
    public int JumpHeight { get; set; }
    public int PreJumpFrames { get; set; }
    public List<MoveFrameDataEntity> Moves { get; set; } = new();
}

/// <summary>
/// Database entity for move frame data.
/// </summary>
public class MoveFrameDataEntity
{
    public int Id { get; set; }
    public int CharacterFrameDataId { get; set; }
    public CharacterFrameDataEntity CharacterFrameData { get; set; } = null!;
    public string MoveName { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public MoveType MoveType { get; set; }
    public int StartupFrames { get; set; }
    public int ActiveFrames { get; set; }
    public int RecoveryFrames { get; set; }
    public int HitAdvantage { get; set; }
    public int BlockAdvantage { get; set; }
    public int Damage { get; set; }
    public int ChipDamage { get; set; }
    public int MeterGain { get; set; }
    public HitLevel HitLevel { get; set; }
    public bool IsAirborne { get; set; }
    public bool IsInvincible { get; set; }
    public int? InvincibilityFrames { get; set; }
    public bool Armor { get; set; }
    public int? ArmorHits { get; set; }
    public bool IsProjectile { get; set; }
    public bool IsThrow { get; set; }
    public bool IsOverhead { get; set; }
    public bool CausesKnockdown { get; set; }
    public bool IsCancelable { get; set; }
    public CancelType CancelWindow { get; set; }
    public string? Notes { get; set; }
}
