namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Playtime distribution analysis.
/// </summary>
public class PlaytimeDistribution
{
    public TimeSpan AverageMatchDuration { get; set; }
    public TimeSpan ShortestMatch { get; set; }
    public TimeSpan LongestMatch { get; set; }
    public Dictionary<string, TimeSpan> DistributionByMechanic { get; set; } = default!;
    public double TotalPlaytimeMinutes { get; set; } = default!;
    public double AverageMatchLength { get; set; } = default!;
    public int MatchCount { get; set; } = default!;
    public IReadOnlyDictionary<string, double> PlaytimeByMechanic { get; set; } = default!;
}
