namespace SaveState.Application.Mugen.Models.NarrativeMemory;

/// <summary>
/// Alternate timeline data.
/// </summary>
public class AlternateTimeline
{
    public string TimelineId { get; set; } = default!;
    public string CreatorId { get; set; } = default!;
    public string SourceCrystalId { get; set; } = default!;
    public string BranchPoint { get; set; } = default!;
    public IReadOnlyList<string> AlternateEvents { get; set; } = default!;
    public float Probability { get; set; } = default!;
    public float Stability { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public bool Explored { get; set; } = default!;
}

/// <summary>
/// Timeline branch request.
/// </summary>
public class TimelineBranchRequest
{
    public string PlayerId { get; set; } = default!;
    public string BranchPoint { get; set; } = default!;
    public string DesiredOutcome { get; set; } = default!;
    public float Probability { get; set; } = default!;
}

/// <summary>
/// Timeline replay data.
/// </summary>
public class TimelineReplay
{
    public string ReplayId { get; set; } = default!;
    public string TimelineId { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public IReadOnlyList<string> KeyDifferences { get; set; } = default!;
    public string Outcome { get; set; } = default!;
    public IReadOnlyList<string> Insights { get; set; } = default!;
    public DateTime ReplayedAt { get; set; } = default!;
}

/// <summary>
/// Replay request.
/// </summary>
public class ReplayRequest
{
    public string PlayerId { get; set; } = default!;
    public bool IncludeCommentary { get; set; } = default!;
    public float PlaybackSpeed { get; set; } = default!;
}
