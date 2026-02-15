namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Dream analytics data.
/// </summary>
public class DreamAnalytics
{
    public string ArenaId { get; set; } = default!;
    public TimeSpan Period { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
    public float AverageStability { get; set; }
    public int SurrealEventCount { get; set; }
    public int SymbolicManifestationCount { get; set; }
    public List<string> TopEmotionalStates { get; set; } = default!;
}
