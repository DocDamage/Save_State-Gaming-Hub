namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Season statistics data.
/// </summary>
public class SeasonStatistics
{
    public int TotalMatches { get; set; } = default!;
    public float AverageRating { get; set; } = default!;
    public float HighestRating { get; set; } = default!;
    public float LowestRating { get; set; } = default!;
    public float RatingVolatility { get; set; } = default!;
}
