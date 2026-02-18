namespace SaveState.Application.Mugen.Services.BalanceTuning.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Engine for analyzing game balance based on match data.
/// </summary>
public class BalanceAnalysisEngine
{
    private readonly ILogger<BalanceAnalysisEngine> _logger;

    public BalanceAnalysisEngine(ILogger<BalanceAnalysisEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyzes mechanic usage patterns from match data.
    /// </summary>
    public Dictionary<string, MechanicUsage> AnalyzeMechanicUsage(IReadOnlyList<MatchData> matchData)
    {
        var usage = new Dictionary<string, MechanicUsage>();
        var mechanicCounts = new Dictionary<string, int>();

        foreach (var match in matchData)
        {
            foreach (var mechanic in match.MechanicsUsed)
            {
                if (!mechanicCounts.ContainsKey(mechanic))
                    mechanicCounts[mechanic] = 0;
                mechanicCounts[mechanic]++;
            }
        }

        foreach (var kvp in mechanicCounts)
        {
            usage[kvp.Key] = new MechanicUsage
            {
                MechanicName = kvp.Key,
                UsageCount = kvp.Value,
                UsageRate = (float)kvp.Value / matchData.Count
            };
        }

        return usage;
    }

    /// <summary>
    /// Calculates win rates for different mechanics.
    /// </summary>
    public Dictionary<string, WinRateData> CalculateWinRates(IReadOnlyList<MatchData> matchData)
    {
        var winRates = new Dictionary<string, WinRateData>();
        var mechanicWins = new Dictionary<string, int>();
        var mechanicUses = new Dictionary<string, int>();

        foreach (var match in matchData)
        {
            foreach (var mechanic in match.MechanicsUsed)
            {
                if (!mechanicUses.ContainsKey(mechanic))
                {
                    mechanicUses[mechanic] = 0;
                    mechanicWins[mechanic] = 0;
                }
                mechanicUses[mechanic]++;
                if (match.WinnerUsedMechanic(mechanic))
                    mechanicWins[mechanic]++;
            }
        }

        foreach (var mechanic in mechanicUses.Keys)
        {
            winRates[mechanic] = new WinRateData
            {
                MechanicName = mechanic,
                Wins = mechanicWins[mechanic],
                TotalUses = mechanicUses[mechanic],
                WinRate = (float)mechanicWins[mechanic] / mechanicUses[mechanic]
            };
        }

        return winRates;
    }

    /// <summary>
    /// Analyzes playtime distribution across mechanics.
    /// </summary>
    public PlaytimeDistribution AnalyzePlaytimeDistribution(IReadOnlyList<MatchData> matchData)
    {
        var durations = matchData.Select(m => m.Duration.TotalMinutes).ToList();
        return new PlaytimeDistribution
        {
            AverageMatchDuration = TimeSpan.FromMinutes(durations.Average()),
            ShortestMatch = TimeSpan.FromMinutes(durations.Min()),
            LongestMatch = TimeSpan.FromMinutes(durations.Max()),
            DistributionByMechanic = new Dictionary<string, TimeSpan>()
        };
    }

    /// <summary>
    /// Analyzes skill gaps between players.
    /// </summary>
    public SkillGapAnalysis AnalyzeSkillGaps(IReadOnlyList<MatchData> matchData)
    {
        var ratingDiffs = matchData.Select(m => Math.Abs(m.Player1Rating - m.Player2Rating)).ToList();
        return new SkillGapAnalysis
        {
            AverageRatingGap = ratingDiffs.Average(),
            UpsetRate = matchData.Count(m => m.Player1Rating > m.Player2Rating && m.Winner == "Player2") / (float)matchData.Count,
            CloseMatchRate = ratingDiffs.Count(d => d < 100) / (float)ratingDiffs.Count
        };
    }

    /// <summary>
    /// Calculates overall balance score.
    /// </summary>
    public float CalculateBalanceScore(IReadOnlyList<MatchData> matchData)
    {
        if (matchData.Count == 0) return 0.5f;

        var winRates = CalculateWinRates(matchData);
        if (winRates.Count == 0) return 0.5f;

        // Calculate variance in win rates - lower variance = better balance
        var rates = winRates.Values.Select(w => w.WinRate).ToList();
        var avg = rates.Average();
        var variance = rates.Select(r => Math.Pow(r - avg, 2)).Average();

        // Score between 0 and 1, where 1 is perfect balance (0.5 win rate for all)
        return (float)Math.Max(0, 1 - variance * 4);
    }

    /// <summary>
    /// Generates balance recommendations based on analysis.
    /// </summary>
    public List<BalanceRecommendation> GenerateBalanceRecommendations(IReadOnlyList<MatchData> matchData)
    {
        var recommendations = new List<BalanceRecommendation>();
        var winRates = CalculateWinRates(matchData);

        foreach (var mechanic in winRates)
        {
            if (mechanic.Value.WinRate > 0.6f)
            {
                recommendations.Add(new BalanceRecommendation
                {
                    MechanicName = mechanic.Key,
                    RecommendationType = "Nerf",
                    Priority = RecommendationPriority.High,
                    Reasoning = $"Win rate of {mechanic.Value.WinRate:P} is too high",
                    SuggestedChange = "Reduce effectiveness by 10-15%"
                });
            }
            else if (mechanic.Value.WinRate < 0.4f)
            {
                recommendations.Add(new BalanceRecommendation
                {
                    MechanicName = mechanic.Key,
                    RecommendationType = "Buff",
                    Priority = RecommendationPriority.Medium,
                    Reasoning = $"Win rate of {mechanic.Value.WinRate:P} is too low",
                    SuggestedChange = "Increase effectiveness by 10-15%"
                });
            }
        }

        return recommendations;
    }
}
