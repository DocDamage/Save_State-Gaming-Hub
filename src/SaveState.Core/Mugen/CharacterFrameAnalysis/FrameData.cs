namespace SaveState.Core.Mugen.CharacterFrameAnalysis;

/// <summary>
/// Represents frame data for a single move/attack.
/// </summary>
public class MoveFrameData
{
    public string MoveName { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty; // e.g., "QCF + LP"
    
    // Frame counts
    public int StartupFrames { get; set; } // Before hitbox appears
    public int ActiveFrames { get; set; }  // Hitbox is active
    public int RecoveryFrames { get; set; } // Return to neutral
    public int TotalFrames => StartupFrames + ActiveFrames + RecoveryFrames;
    
    // Advantage
    public int HitAdvantage { get; set; } // Frame advantage on hit
    public int BlockAdvantage { get; set; } // Frame advantage on block (negative = punishable)
    
    // Damage
    public int Damage { get; set; }
    public int ChipDamage { get; set; } // Damage on block
    public int MeterGain { get; set; }
    
    // Properties
    public HitLevel HitLevel { get; set; } // Low/Mid/High/Overhead
    public bool IsAirborne { get; set; }
    public bool IsInvincible { get; set; }
    public int? InvincibilityFrames { get; set; }
    public bool Armor { get; set; }
    public int? ArmorHits { get; set; }
    
    // Special properties
    public bool IsProjectile { get; set; }
    public bool IsThrow { get; set; }
    public bool IsOverhead { get; set; }
    public bool CausesKnockdown { get; set; }
    public bool IsCancelable { get; set; }
    public CancelType CancelWindow { get; set; }
    
    // Notes
    public string? Notes { get; set; }
}

/// <summary>
/// Hit level for attacks.
/// </summary>
public enum HitLevel
{
    Low,      // Must block low
    Mid,      // Can block standing or crouching
    High,     // Must block standing
    Overhead, // Must block standing, hits crouching
    Throw,    // Unblockable throw
    Projectile
}

/// <summary>
/// Cancel types for moves.
/// </summary>
public enum CancelType
{
    None,
    Self,     // Can cancel into itself (target combo)
    Special,  // Can cancel into special moves
    Super,    // Can cancel into super moves
    Any       // Can cancel into anything
}

/// <summary>
/// Complete frame data for a character.
/// </summary>
public class CharacterFrameData
{
    public string CharacterName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty; // Game version
    public DateTime LastUpdated { get; set; }
    
    // Character attributes
    public int Health { get; set; }
    public int WalkSpeed { get; set; }
    public int BackWalkSpeed { get; set; }
    public int DashDistance { get; set; }
    public int JumpHeight { get; set; }
    public int PreJumpFrames { get; set; }
    
    // All moves
    public List<MoveFrameData> StandingNormals { get; set; } = new();
    public List<MoveFrameData> CrouchingNormals { get; set; } = new();
    public List<MoveFrameData> AirNormals { get; set; } = new();
    public List<MoveFrameData> CommandMoves { get; set; } = new();
    public List<MoveFrameData> SpecialMoves { get; set; } = new();
    public List<MoveFrameData> SuperMoves { get; set; } = new();
    public List<MoveFrameData> Throws { get; set; } = new();
    
    public IEnumerable<MoveFrameData> AllMoves => 
        StandingNormals
        .Concat(CrouchingNormals)
        .Concat(AirNormals)
        .Concat(CommandMoves)
        .Concat(SpecialMoves)
        .Concat(SuperMoves)
        .Concat(Throws);
}
