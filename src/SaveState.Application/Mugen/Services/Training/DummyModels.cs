namespace SaveState.Application.Mugen.Services.Training;

/// <summary>
/// AI dummy settings.
/// </summary>
public class DummySettings
{
    public DummyBehavior Behavior { get; set; } = DummyBehavior.Stand;
    public int GuardLevel { get; set; } = 100;
    public int RecoverySpeed { get; set; } = 100;
    public bool RandomReversal { get; set; } = false;
    public float ReversalChance { get; set; } = 0.0f;
    public bool AutoGuard { get; set; } = false;
    public int GuardBar { get; set; } = 1000;
    
    public List<DummyAction> ActionSequence { get; set; } = new();
    public bool LoopActions { get; set; } = false;
}

/// <summary>
/// Dummy action in a sequence.
/// </summary>
public class DummyAction
{
    public DummyBehavior Behavior { get; set; }
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan? DelayBefore { get; set; }
    public int? FrameStart { get; set; }
    public int? FrameEnd { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Dummy state tracking.
/// </summary>
public class DummyState
{
    public string DummyId { get; set; } = default!;
    public DummyBehavior CurrentBehavior { get; set; }
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentGuardBar { get; set; }
    public int MaxGuardBar { get; set; }
    public bool IsBlocking { get; set; }
    public bool IsHit { get; set; }
    public bool IsKnockedDown { get; set; }
    public TimeSpan StateTime { get; set; }
    public int CurrentActionIndex { get; set; }
    public int TimesHit { get; set; }
    public int ComboHitsTaken { get; set; }
    public double TotalDamageTaken { get; set; }
}

/// <summary>
/// Guard level options.
/// </summary>
public enum GuardLevel
{
    None = 0,
    Low = 25,
    Medium = 50,
    High = 75,
    Maximum = 100
}

/// <summary>
/// Recovery speed options.
/// </summary>
public enum RecoverySpeed
{
    Slowest = 50,
    Slow = 75,
    Normal = 100,
    Fast = 125,
    Fastest = 150
}
