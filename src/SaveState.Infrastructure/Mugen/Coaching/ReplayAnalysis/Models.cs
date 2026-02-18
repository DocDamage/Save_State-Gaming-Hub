namespace SaveState.Infrastructure.Mugen.Coaching.ReplayAnalysis;

/// <summary>
/// Replay metadata information.
/// </summary>
public class ReplayMetadata
{
    public string? Player1 { get; set; }
    public string? Player2 { get; set; }
    public string? Winner { get; set; }
    public string? Stage { get; set; }
    public string? Game { get; set; }
    public DateTimeOffset? RecordedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? Source { get; set; }
}

/// <summary>
/// Replay analysis result.
/// </summary>
public record ReplayAnalysisResult(
    ReplayMetadata Metadata,
    IReadOnlyList<ReplayEvent> Events,
    PlayerReplaySummary[] Players,
    IReadOnlyList<MoveSequenceSummary> Sequences,
    ReplayOutcome Outcome);

/// <summary>
/// Individual replay event.
/// </summary>
public record ReplayEvent(
    int PlayerIndex,
    ReplayEventType Type,
    string? Move,
    string? Command,
    int? Damage,
    int? Frame,
    double? TimeSeconds,
    string? Raw);

/// <summary>
/// Summary of a move sequence.
/// </summary>
public record MoveSequenceSummary(
    int PlayerIndex,
    IReadOnlyList<string> Moves,
    int Hits,
    int Damage,
    int Occurrences,
    int Drops);

/// <summary>
/// Player summary from replay analysis.
/// </summary>
public class PlayerReplaySummary
{
    public PlayerReplaySummary(int playerIndex)
    {
        PlayerIndex = playerIndex;
    }

    public int PlayerIndex { get; }
    public string? Name { get; set; }
    public int TotalMoves { get; set; }
    public int Hits { get; set; }
    public int Blocks { get; set; }
    public int Whiffs { get; set; }
    public int DamageDealt { get; set; }
    public int DamageTaken { get; set; }
    public int Throws { get; set; }
    public int Projectiles { get; set; }
    public int AntiAirs { get; set; }
    public int Knockdowns { get; set; }
    public int Combos { get; set; }
    public int ComboDrops { get; set; }
    public float HitRate => TotalMoves == 0 ? 0f : (float)Hits / TotalMoves;
    public float WhiffRate => TotalMoves == 0 ? 0f : (float)Whiffs / TotalMoves;
}

/// <summary>
/// Replay outcome enumeration.
/// </summary>
public enum ReplayOutcome
{
    Unknown,
    Player1Win,
    Player2Win,
    Draw
}

/// <summary>
/// Replay event type enumeration.
/// </summary>
public enum ReplayEventType
{
    Unknown,
    Move,
    Hit,
    Block,
    Whiff,
    Throw,
    Projectile,
    AntiAir,
    Knockdown,
    Movement
}
