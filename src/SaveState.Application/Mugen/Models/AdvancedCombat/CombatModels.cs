namespace SaveState.Application.Mugen.Models.AdvancedCombat;

/// <summary>
/// Advanced combat session data.
/// </summary>
public class AdvancedCombatSession
{
    public string SessionId { get; set; } = default!;
    public string Player1Id { get; set; } = default!;
    public string Player2Id { get; set; } = default!;
    public float CurrentZPosition { get; set; } = default!;
    public float GravityScale { get; set; } = default!;
    public float JuggleHeight { get; set; } = default!;
    public int BufferWindow { get; set; } = default!;
    public bool EnableZAxisMovement { get; set; } = default!;
    public bool EnableJuggleScaling { get; set; } = default!;
    public bool EnableFrameDataDisplay { get; set; } = default!;
    public bool EnableInputBuffering { get; set; } = default!;
    public DateTime StartedAt { get; set; } = default!;
    public DateTime? EndedAt { get; set; } = default!;
    public CombatStatus Status { get; set; } = default!;
    public string? LastBufferedInput { get; set; } = default!;
}

/// <summary>
/// Combat session request.
/// </summary>
public class AdvancedCombatSessionRequest
{
    public string Player1Id { get; set; } = default!;
    public string Player2Id { get; set; } = default!;
    public bool EnableZAxisMovement { get; set; } = default!;
    public bool EnableJuggleScaling { get; set; } = default!;
    public bool EnableFrameDataDisplay { get; set; } = default!;
    public bool EnableInputBuffering { get; set; } = default!;
}

/// <summary>
/// Attack data for combat calculations.
/// </summary>
public class AttackData
{
    public string MoveName { get; set; } = default!;
    public int Damage { get; set; } = default!;
    public AttackProperty Property { get; set; } = default!;
    public int StartupFrames { get; set; } = default!;
    public int ActiveFrames { get; set; } = default!;
    public int RecoveryFrames { get; set; } = default!;
    public int Blockstun { get; set; } = default!;
    public int Hitstun { get; set; } = default!;
    public float Pushback { get; set; } = default!;
    public bool IsSpecial { get; set; } = default!;
    public bool IsSuper { get; set; } = default!;
}

/// <summary>
/// Defense data for combat calculations.
/// </summary>
public class DefenseData
{
    public DefenseType Type { get; set; } = default!;
    public int Health { get; set; } = default!;
    public bool IsBlocking { get; set; } = default!;
    public bool IsParrying { get; set; } = default!;
    public int BlockstunRemaining { get; set; } = default!;
    public float GuardMeter { get; set; } = default!;
}

/// <summary>
/// Combat state snapshot.
/// </summary>
public class CombatState
{
    public string SessionId { get; set; } = default!;
    public AdvancedCombatSession? Session { get; set; }
    public float CurrentZPosition { get; set; } = default!;
    public float GravityScale { get; set; } = default!;
    public float JuggleHeight { get; set; } = default!;
    public DateTime CapturedAt { get; set; } = default!;
}
