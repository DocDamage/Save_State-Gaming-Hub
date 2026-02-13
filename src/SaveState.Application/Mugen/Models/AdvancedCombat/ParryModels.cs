namespace SaveState.Application.Mugen.Models.AdvancedCombat;

/// <summary>
/// Parry window configuration.
/// </summary>
public class ParryWindow
{
    public string WindowId { get; set; } = default!;
    public string SessionId { get; set; } = default!;
    public ParryType Type { get; set; } = default!;
    public int ActiveFrames { get; set; } = default!;
    public int StartupFrames { get; set; } = default!;
    public int RecoveryFrames { get; set; } = default!;
    public DateTime ActivatedAt { get; set; } = default!;
    public bool IsActive { get; set; } = default!;
}

/// <summary>
/// Counter attack data.
/// </summary>
public class CounterAttack
{
    public string CounterId { get; set; } = default!;
    public string SessionId { get; set; } = default!;
    public string OriginalAttack { get; set; } = default!;
    public string CounterMove { get; set; } = default!;
    public float DamageMultiplier { get; set; } = default!;
    public int FrameAdvantage { get; set; } = default!;
    public bool IsGuaranteed { get; set; } = default!;
    public DateTime ExecutedAt { get; set; } = default!;
}

/// <summary>
/// Parry result.
/// </summary>
public class ParryResult
{
    public bool Success { get; set; } = default!;
    public string ParryId { get; set; } = default!;
    public string SessionId { get; set; } = default!;
    public ParryType Type { get; set; } = default!;
    public int TimingPrecision { get; set; } = default!;
    public CounterAttack? Counter { get; set; }
    public DateTime ExecutedAt { get; set; } = default!;
}

/// <summary>
/// Parry request.
/// </summary>
public class ParryRequest
{
    public string SessionId { get; set; } = default!;
    public ParryType Type { get; set; } = default!;
    public int InputFrame { get; set; } = default!;
    public string ExpectedAttack { get; set; } = default!;
    public int ReactionWindow { get; set; } = default!;
}
