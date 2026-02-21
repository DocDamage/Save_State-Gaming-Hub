namespace SaveState.Application.Mugen.Services.MatchAnalytics;

/// <summary>
/// A detected pattern in player behavior.
/// </summary>
public record DetectedPattern(
    PatternType Type,
    string Name,
    string Description,
    decimal Frequency,
    decimal Confidence,
    IReadOnlyList<string> AssociatedMoves,
    string Impact,
    DateTime DetectedAt);

/// <summary>
/// A specific match where a pattern was observed.
/// </summary>
public record PatternMatch(
    Guid MatchId,
    DateTime MatchDate,
    decimal PatternStrength,
    IReadOnlyList<string> Evidence);

/// <summary>
/// Collection of related patterns forming a playstyle profile.
/// </summary>
public record PatternProfile(
    Guid PlayerId,
    string DominantStyle,
    IReadOnlyList<DetectedPattern> Patterns,
    DateTime GeneratedAt);

/// <summary>
/// Pattern definition for internal analysis.
/// </summary>
internal class PatternDefinition
{
    public string PatternType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float Threshold { get; set; }
    public required Func<Guid, IReadOnlyList<MatchData>, CancellationToken, Task<float>> AnalysisFunction { get; set; }
}

/// <summary>
/// Player pattern data from legacy system.
/// </summary>
public record PlayerPattern(
    string PatternType,
    string Description,
    decimal Frequency,
    IReadOnlyList<string> AssociatedMoves,
    string Impact);
