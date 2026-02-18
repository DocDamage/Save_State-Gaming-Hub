namespace SaveState.Application.Mugen.Services.BalanceTuning.Engines;

using Microsoft.Extensions.Logging;

/// <summary>
/// Engine for balancing matchmaking.
/// </summary>
public class MatchmakingBalance
{
    private readonly ILogger<MatchmakingBalance> _logger;

    public MatchmakingBalance(ILogger<MatchmakingBalance> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculates match quality based on rating difference.
    /// </summary>
    public float CalculateMatchQuality(float rating1, float rating2)
    {
        var diff = Math.Abs(rating1 - rating2);
        return Math.Max(0, 1 - diff / 400); // Perfect match at 0 diff, unacceptable at 400+
    }

    /// <summary>
    /// Finds optimal match for a player.
    /// </summary>
    public string? FindOptimalMatch(string playerId, float playerRating, IReadOnlyList<(string id, float rating)> candidates)
    {
        if (candidates.Count == 0) return null;

        var bestMatch = candidates
            .OrderBy(c => Math.Abs(c.rating - playerRating))
            .First();

        return bestMatch.id;
    }
}
