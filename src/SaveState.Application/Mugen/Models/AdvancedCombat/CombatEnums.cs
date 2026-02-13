namespace SaveState.Application.Mugen.Models.AdvancedCombat;

/// <summary>
/// Z-axis movement directions for 3D positioning.
/// </summary>
public enum ZDirection
{
    Forward,
    Backward,
    Left,
    Right
}

/// <summary>
/// Combat session status.
/// </summary>
public enum CombatStatus
{
    Preparing,
    Active,
    Paused,
    Completed,
    Failed
}

/// <summary>
/// Frame data display modes.
/// </summary>
public enum DisplayMode
{
    Simple,
    Detailed,
    Advanced,
    Training
}

/// <summary>
/// Analysis depth levels.
/// </summary>
public enum AnalysisDepth
{
    Basic,
    Detailed,
    Comprehensive
}

/// <summary>
/// Parry types for counter mechanics.
/// </summary>
public enum ParryType
{
    Light,
    Heavy,
    Special,
    Perfect
}

/// <summary>
/// Juggle state for combo system.
/// </summary>
public enum JuggleStateType
{
    Grounded,
    Airborne,
    WallBounce,
    GroundBounce,
    Recovery
}

/// <summary>
/// Combo types for validation.
/// </summary>
public enum ComboType
{
    Normal,
    Juggle,
    WallCombo,
    GroundCombo,
    Special
}

/// <summary>
/// Attack properties.
/// </summary>
public enum AttackProperty
{
    High,
    Mid,
    Low,
    Overhead,
    Unblockable
}

/// <summary>
/// Defense options.
/// </summary>
public enum DefenseType
{
    Standing,
    Crouching,
    Jumping,
    Blocking,
    Parrying
}
