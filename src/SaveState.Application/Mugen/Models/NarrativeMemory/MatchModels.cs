namespace SaveState.Application.Mugen.Models.NarrativeMemory;

/// <summary>
/// Match result data.
/// </summary>
public class NarrativeMatchResult
{
    public string MatchId { get; set; } = default!;
    public int RoundNumber { get; set; } = default!;
    public MatchOutcome Outcome { get; set; } = default!;
    public int DamageDealt { get; set; } = default!;
    public int DamageReceived { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public IReadOnlyList<string> CombosUsed { get; set; } = default!;
    public EmotionalContext EmotionalContext { get; set; } = default!;
}

/// <summary>
/// Narrative analytics data.
/// </summary>
public class NarrativeAnalytics
{
    public string PlayerId { get; set; } = default!;
    public TimeSpan Period { get; set; } = default!;
    public int CrystalsCollected { get; set; } = default!;
    public int TimelinesExplored { get; set; } = default!;
    public int MovesSynthesized { get; set; } = default!;
    public int ButterflyEffectsTriggered { get; set; } = default!;
    public float NarrativeDiversity { get; set; } = default!;
    public float StoryCompletion { get; set; } = default!;
    public int AlternateOutcomesExplored { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}
