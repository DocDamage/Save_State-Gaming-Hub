namespace SaveState.Application.Mugen.Models.AdvancedCombat;

/// <summary>
/// Juggle state data.
/// </summary>
public class JuggleState
{
    public string JuggleId { get; set; } = default!;
    public string SessionId { get; set; } = default!;
    public float CurrentHeight { get; set; } = default!;
    public float GravityMultiplier { get; set; } = default!;
    public int ComboLength { get; set; } = default!;
    public float MomentumFactor { get; set; } = default!;
    public DateTime AppliedAt { get; set; } = default!;
    public bool Active { get; set; } = default!;
    public JuggleStateType State { get; set; } = default!;
}

/// <summary>
/// Juggle request.
/// </summary>
public class JuggleRequest
{
    public float CurrentHeight { get; set; } = default!;
    public int ComboLength { get; set; } = default!;
    public bool ApplyScaling { get; set; } = default!;
}

/// <summary>
/// Gravity modifier for juggle physics.
/// </summary>
public class GravityMod
{
    public string ModId { get; set; } = default!;
    public string SessionId { get; set; } = default!;
    public float Multiplier { get; set; } = default!;
    public int DurationFrames { get; set; } = default!;
    public bool IsDecay { get; set; } = default!;
    public DateTime AppliedAt { get; set; } = default!;
}

/// <summary>
/// Physics state data.
/// </summary>
public class PhysicsState
{
    public float GravityScale { get; set; } = default!;
    public float JuggleHeight { get; set; } = default!;
    public float AirControl { get; set; } = default!;
    public float LandingLag { get; set; } = default!;
    public DateTime MeasuredAt { get; set; } = default!;
}

/// <summary>
/// Z-axis movement data.
/// </summary>
public class ZAxisMovement
{
    public string MovementId { get; set; } = default!;
    public string SessionId { get; set; } = default!;
    public ZDirection Direction { get; set; } = default!;
    public float Distance { get; set; } = default!;
    public float Speed { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public DateTime ExecutedAt { get; set; } = default!;
    public bool Success { get; set; } = default!;
}

/// <summary>
/// Sidestep request.
/// </summary>
public class SidestepRequest
{
    public ZDirection Direction { get; set; } = default!;
    public float Distance { get; set; } = default!;
    public float Speed { get; set; } = default!;
}

/// <summary>
/// Z-axis positioning data.
/// </summary>
public class ZAxisPositioning
{
    public float CurrentZPosition { get; set; } = default!;
    public float AvailableRange { get; set; } = default!;
    public float[] OptimalPositions { get; set; } = default!;
    public IReadOnlyList<TacticalAdvantage> TacticalAdvantages { get; set; } = default!;
    public DateTime MeasuredAt { get; set; } = default!;
}

/// <summary>
/// Tactical advantage data.
/// </summary>
public class TacticalAdvantage
{
    public string Type { get; set; } = default!;
    public float Strength { get; set; } = default!;
}

/// <summary>
/// Juggle mechanics analysis data.
/// </summary>
public class JuggleMechanics
{
    public int TotalJuggles { get; set; } = default!;
    public float AverageGravityScale { get; set; } = default!;
    public float MaxHeightAchieved { get; set; } = default!;
    public float ComboExtensionRate { get; set; } = default!;
    public float PhysicsManipulation { get; set; } = default!;
}
