namespace SaveState.Application.Mugen.Services.BalanceTuning.Engines;

using Microsoft.Extensions.Logging;

/// <summary>
/// Engine for calculating ELO ratings.
/// </summary>
public class EloCalculator
{
    private readonly ILogger<EloCalculator> _logger;
    private const float KFactor = 32; // Standard K-factor

    public EloCalculator(ILogger<EloCalculator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculates new ratings after a match.
    /// </summary>
    public (float newRating1, float newRating2) CalculateRatings(
        float rating1,
        float rating2,
        float score1,
        float score2)
    {
        var expected1 = CalculateExpectedScore(rating1, rating2);
        var expected2 = CalculateExpectedScore(rating2, rating1);

        var actual1 = score1 > score2 ? 1.0f : score1 == score2 ? 0.5f : 0.0f;
        var actual2 = 1.0f - actual1;

        var newRating1 = rating1 + KFactor * (actual1 - expected1);
        var newRating2 = rating2 + KFactor * (actual2 - expected2);

        return (newRating1, newRating2);
    }

    /// <summary>
    /// Calculates expected score for a player.
    /// </summary>
    public float CalculateExpectedScore(float playerRating, float opponentRating)
    {
        return 1.0f / (1.0f + MathF.Pow(10, (opponentRating - playerRating) / 400.0f));
    }
}
