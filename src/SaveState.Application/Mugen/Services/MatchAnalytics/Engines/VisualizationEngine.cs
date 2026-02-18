namespace SaveState.Application.Mugen.Services.MatchAnalytics.Engines;

using Microsoft.Extensions.Logging;

/// <summary>
/// Engine for preparing trend visualizations from match data.
/// </summary>
public class VisualizationEngine
{
    private readonly ILogger<VisualizationEngine> _logger;

    public VisualizationEngine(ILogger<VisualizationEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Prepares trend visualization data for a player over a specified time period.
    /// </summary>
    /// <param name="playerId">The player ID.</param>
    /// <param name="matches">The matches to visualize.</param>
    /// <param name="start">Start date/time.</param>
    /// <param name="end">End date/time.</param>
    /// <returns>Trend visualization data.</returns>
    public TrendVisualization PrepareTrendVisualization(
        Guid playerId,
        IReadOnlyList<MatchData> matches,
        DateTime start,
        DateTime end)
    {
        _logger.LogInformation("Preparing trend visualization for player {PlayerId} with {MatchCount} matches",
            playerId, matches.Count);

        if (!matches.Any())
        {
            return new TrendVisualization(
                PlayerId: playerId,
                StartDate: start,
                EndDate: end,
                WinRateTrend: Array.Empty<TrendPoint>(),
                DamageTrend: Array.Empty<TrendPoint>(),
                ComboTrend: Array.Empty<TrendPoint>(),
                NotableChanges: Array.Empty<string>()
            );
        }

        // Sort matches chronologically
        var sortedMatches = matches.OrderBy(m => m.StartTime).ToList();

        // Calculate win rate trend (rolling window)
        var winRateTrend = CalculateWinRateTrend(playerId, sortedMatches);

        // Calculate damage trend
        var damageTrend = CalculateDamageTrend(playerId, sortedMatches);

        // Calculate combo trend
        var comboTrend = CalculateComboTrend(playerId, sortedMatches);

        // Identify notable changes
        var notableChanges = IdentifyNotableChanges(winRateTrend, damageTrend, comboTrend);

        var visualization = new TrendVisualization(
            PlayerId: playerId,
            StartDate: start,
            EndDate: end,
            WinRateTrend: winRateTrend,
            DamageTrend: damageTrend,
            ComboTrend: comboTrend,
            NotableChanges: notableChanges
        );

        _logger.LogDebug("Trend visualization prepared with {WinRatePoints} win rate points, {DamagePoints} damage points, {ComboPoints} combo points",
            winRateTrend.Count, damageTrend.Count, comboTrend.Count);

        return visualization;
    }

    private IReadOnlyList<TrendPoint> CalculateWinRateTrend(Guid playerId, List<MatchData> sortedMatches)
    {
        var trendPoints = new List<TrendPoint>();
        const int windowSize = 5; // Rolling window of 5 matches

        for (int i = 0; i < sortedMatches.Count; i++)
        {
            var windowStart = Math.Max(0, i - windowSize + 1);
            var windowMatches = sortedMatches.Skip(windowStart).Take(i - windowStart + 1).ToList();

            var wins = windowMatches.Count(m => m.Rounds.LastOrDefault()?.WinnerId == playerId);
            var winRate = windowMatches.Count > 0 ? (decimal)wins / windowMatches.Count * 100 : 0;

            var context = GenerateWinRateContext(winRate, wins, windowMatches.Count);

            trendPoints.Add(new TrendPoint(
                Date: sortedMatches[i].StartTime,
                Value: winRate,
                Context: context
            ));
        }

        return trendPoints;
    }

    private string GenerateWinRateContext(decimal winRate, int wins, int total)
    {
        return winRate switch
        {
            >= 80m => $"Dominant: {wins}/{total} wins",
            >= 60m => $"Strong: {wins}/{total} wins",
            >= 40m => $"Even: {wins}/{total} wins",
            >= 20m => $"Struggling: {wins}/{total} wins",
            _ => $"Needs improvement: {wins}/{total} wins"
        };
    }

    private IReadOnlyList<TrendPoint> CalculateDamageTrend(Guid playerId, List<MatchData> sortedMatches)
    {
        var trendPoints = new List<TrendPoint>();

        foreach (var match in sortedMatches)
        {
            var totalDamage = match.Rounds
                .SelectMany(r => r.Hits)
                .Where(h => h.AttackerId == playerId)
                .Sum(h => h.Damage);

            var context = GenerateDamageContext(totalDamage);

            trendPoints.Add(new TrendPoint(
                Date: match.StartTime,
                Value: totalDamage,
                Context: context
            ));
        }

        return trendPoints;
    }

    private string GenerateDamageContext(int totalDamage)
    {
        return totalDamage switch
        {
            >= 1000 => "Exceptional damage output",
            >= 750 => "High damage output",
            >= 500 => "Average damage output",
            >= 250 => "Low damage output",
            _ => "Very low damage output"
        };
    }

    private IReadOnlyList<TrendPoint> CalculateComboTrend(Guid playerId, List<MatchData> sortedMatches)
    {
        var trendPoints = new List<TrendPoint>();

        foreach (var match in sortedMatches)
        {
            var combos = match.Rounds
                .SelectMany(r => r.Combos)
                .Where(c => c.PlayerId == playerId)
                .ToList();

            var avgComboLength = combos.Any() ? (decimal)combos.Average(c => c.Length) : 0;
            var longestCombo = combos.Any() ? combos.Max(c => c.Length) : 0;

            var context = GenerateComboContext(avgComboLength, longestCombo);

            trendPoints.Add(new TrendPoint(
                Date: match.StartTime,
                Value: avgComboLength,
                Context: context
            ));
        }

        return trendPoints;
    }

    private string GenerateComboContext(decimal avgLength, int longestCombo)
    {
        return avgLength switch
        {
            >= 8m => $"Masterful combos (avg: {avgLength:F1}, max: {longestCombo})",
            >= 5m => $"Strong combos (avg: {avgLength:F1}, max: {longestCombo})",
            >= 3m => $"Good combos (avg: {avgLength:F1}, max: {longestCombo})",
            >= 1m => $"Developing combos (avg: {avgLength:F1}, max: {longestCombo})",
            _ => "No combos executed"
        };
    }

    private IReadOnlyList<string> IdentifyNotableChanges(
        IReadOnlyList<TrendPoint> winRateTrend,
        IReadOnlyList<TrendPoint> damageTrend,
        IReadOnlyList<TrendPoint> comboTrend)
    {
        var changes = new List<string>();

        // Check for significant win rate changes
        if (winRateTrend.Count >= 5)
        {
            var earlyWinRate = winRateTrend.Take(3).Average(t => t.Value);
            var lateWinRate = winRateTrend.Skip(winRateTrend.Count - 3).Average(t => t.Value);
            var winRateChange = lateWinRate - earlyWinRate;

            if (winRateChange >= 30m)
            {
                changes.Add($"Significant improvement in win rate (+{winRateChange:F1}%)");
            }
            else if (winRateChange <= -30m)
            {
                changes.Add($"Significant decline in win rate ({winRateChange:F1}%)");
            }
        }

        // Check for damage output changes
        if (damageTrend.Count >= 5)
        {
            var earlyDamage = damageTrend.Take(3).Average(t => t.Value);
            var lateDamage = damageTrend.Skip(damageTrend.Count - 3).Average(t => t.Value);
            var damageChange = lateDamage - earlyDamage;

            if (damageChange >= 200m)
            {
                changes.Add($"Increased damage output (+{damageChange:F0} per match)");
            }
            else if (damageChange <= -200m)
            {
                changes.Add($"Decreased damage output ({damageChange:F0} per match)");
            }
        }

        // Check for combo improvement
        if (comboTrend.Count >= 5)
        {
            var earlyCombo = comboTrend.Take(3).Average(t => t.Value);
            var lateCombo = comboTrend.Skip(comboTrend.Count - 3).Average(t => t.Value);
            var comboChange = lateCombo - earlyCombo;

            if (comboChange >= 2m)
            {
                changes.Add($"Improved combo execution (+{comboChange:F1} average length)");
            }
            else if (comboChange <= -2m)
            {
                changes.Add($"Declined combo execution ({comboChange:F1} average length)");
            }
        }

        // Check for consistency
        if (winRateTrend.Count >= 10)
        {
            var recentWinRates = winRateTrend.Skip(winRateTrend.Count - 5).Select(t => t.Value).ToList();
            var volatility = CalculateVolatility(recentWinRates);

            if (volatility < 10m)
            {
                changes.Add("Very consistent recent performance");
            }
            else if (volatility > 40m)
            {
                changes.Add("Inconsistent recent performance");
            }
        }

        // Check for peak performance
        if (winRateTrend.Any())
        {
            var maxWinRate = winRateTrend.Max(t => t.Value);
            if (maxWinRate >= 90m && winRateTrend.Last().Value >= 80m)
            {
                changes.Add("Peak performance maintained");
            }
        }

        return changes;
    }

    private decimal CalculateVolatility(List<decimal> values)
    {
        if (values.Count < 2)
            return 0;

        var average = values.Average();
        var sumSquaredDifferences = values.Sum(v => (v - average) * (v - average));
        var variance = sumSquaredDifferences / values.Count;
        return (decimal)Math.Sqrt((double)variance);
    }
}

/// <summary>
/// Trend visualization data container.
/// </summary>
public record TrendVisualization(
    Guid PlayerId,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<TrendPoint> WinRateTrend,
    IReadOnlyList<TrendPoint> DamageTrend,
    IReadOnlyList<TrendPoint> ComboTrend,
    IReadOnlyList<string> NotableChanges);
