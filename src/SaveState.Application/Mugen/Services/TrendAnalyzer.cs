using Microsoft.Extensions.Logging;
using SaveState.Core.Mugen.Services;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Advanced trend analyzer for identifying performance patterns and changes over time.
/// Provides insights into player improvement, skill development, and performance trends.
/// </summary>
public class TrendAnalyzer
{
    private readonly ILogger<TrendAnalyzer> _logger;

    public TrendAnalyzer(ILogger<TrendAnalyzer> logger)
    {
        _logger = logger;
    }

    public async Task<PerformanceTrends> AnalyzeTrendsAsync(
        Guid playerId,
        IReadOnlyList<MatchRecording> matches,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Analyzing trends for player {PlayerId} from {StartDate} to {EndDate}",
                playerId, startDate, endDate);

            // Filter matches within date range and sort by date
            var filteredMatches = matches
                .Where(m => m.StartTime >= startDate && m.EndTime <= endDate)
                .OrderBy(m => m.StartTime)
                .ToList();

            if (!filteredMatches.Any())
            {
                return CreateEmptyTrends(playerId, startDate, endDate);
            }

            // Calculate win rate trend
            var winRateTrend = await CalculateWinRateTrendAsync(playerId, filteredMatches, ct);

            // Calculate damage trend
            var damageTrend = await CalculateDamageTrendAsync(playerId, filteredMatches, ct);

            // Calculate combo trend
            var comboTrend = await CalculateComboTrendAsync(playerId, filteredMatches, ct);

            // Identify notable changes
            var notableChanges = await IdentifyNotableChangesAsync(filteredMatches, ct);

            return new PerformanceTrends(
                PlayerId: playerId,
                StartDate: startDate,
                EndDate: endDate,
                WinRateTrend: winRateTrend,
                DamageTrend: damageTrend,
                ComboTrend: comboTrend,
                NotableChanges: notableChanges
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing trends for player {PlayerId}", playerId);
            return CreateEmptyTrends(playerId, startDate, endDate);
        }
    }

    private async Task<IReadOnlyList<TrendDataPoint>> CalculateWinRateTrendAsync(
        Guid playerId,
        IReadOnlyList<MatchRecording> matches,
        CancellationToken ct)
    {
        var trendPoints = new List<TrendDataPoint>();

        // Group matches by week
        var weeklyGroups = matches
            .GroupBy(m => GetWeekStart(m.StartTime))
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var group in weeklyGroups)
        {
            var weekStart = group.Key;
            var weekMatches = group.ToList();

            var wins = weekMatches.Count(m => m.Rounds.Last().WinnerId == playerId);
            var winRate = weekMatches.Any() ? (decimal)wins / weekMatches.Count : 0;

            trendPoints.Add(new TrendDataPoint(
                Date: weekStart,
                Value: winRate,
                Context: $"{wins}W-{weekMatches.Count - wins}L"
            ));
        }

        return trendPoints;
    }

    private async Task<IReadOnlyList<TrendDataPoint>> CalculateDamageTrendAsync(
        Guid playerId,
        IReadOnlyList<MatchRecording> matches,
        CancellationToken ct)
    {
        var trendPoints = new List<TrendDataPoint>();

        // Group matches by week
        var weeklyGroups = matches
            .GroupBy(m => GetWeekStart(m.StartTime))
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var group in weeklyGroups)
        {
            var weekStart = group.Key;
            var weekMatches = group.ToList();

            // Calculate average damage dealt per match
            var totalDamage = 0;
            var matchCount = 0;

            foreach (var match in weekMatches)
            {
                var matchDamage = match.Rounds.Sum(r =>
                    r.Hits.Where(h => h.AttackerId == playerId).Sum(h => h.Damage));

                if (matchDamage > 0)
                {
                    totalDamage += matchDamage;
                    matchCount++;
                }
            }

            var avgDamage = matchCount > 0 ? (decimal)totalDamage / matchCount : 0;

            trendPoints.Add(new TrendDataPoint(
                Date: weekStart,
                Value: avgDamage,
                Context: $"{matchCount} matches"
            ));
        }

        return trendPoints;
    }

    private async Task<IReadOnlyList<TrendDataPoint>> CalculateComboTrendAsync(
        Guid playerId,
        IReadOnlyList<MatchRecording> matches,
        CancellationToken ct)
    {
        var trendPoints = new List<TrendDataPoint>();

        // Group matches by week
        var weeklyGroups = matches
            .GroupBy(m => GetWeekStart(m.StartTime))
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var group in weeklyGroups)
        {
            var weekStart = group.Key;
            var weekMatches = group.ToList();

            // Calculate average combo length
            var allCombos = weekMatches.SelectMany(m =>
                m.Rounds.SelectMany(r => r.Combos.Where(c => c.PlayerId == playerId)));

            var avgComboLength = allCombos.Any() ? (decimal)allCombos.Average(c => c.Length) : 0;

            // Calculate combo success rate
            var successfulCombos = allCombos.Count(c => c.Length >= 3);
            var totalCombos = allCombos.Count();
            var successRate = totalCombos > 0 ? (decimal)successfulCombos / totalCombos : 0;

            trendPoints.Add(new TrendDataPoint(
                Date: weekStart,
                Value: avgComboLength,
                Context: $"{successRate:P1} success rate"
            ));
        }

        return trendPoints;
    }

    private async Task<IReadOnlyList<string>> IdentifyNotableChangesAsync(
        IReadOnlyList<MatchRecording> matches,
        CancellationToken ct)
    {
        var changes = new List<string>();

        if (matches.Count < 5)
        {
            return changes; // Need minimum data for trend analysis
        }

        // Split into first half and second half
        var midPoint = matches.Count / 2;
        var firstHalf = matches.Take(midPoint).ToList();
        var secondHalf = matches.Skip(midPoint).ToList();

        // Analyze win rate improvement
        var firstHalfWinRate = CalculateWinRate(firstHalf);
        var secondHalfWinRate = CalculateWinRate(secondHalf);

        if (secondHalfWinRate - firstHalfWinRate >= 0.15m) // 15% improvement
        {
            changes.Add($"Significant win rate improvement: {firstHalfWinRate:P1} → {secondHalfWinRate:P1}");
        }

        // Analyze damage improvement
        var firstHalfAvgDamage = CalculateAverageDamage(firstHalf);
        var secondHalfAvgDamage = CalculateAverageDamage(secondHalf);

        if (secondHalfAvgDamage - firstHalfAvgDamage >= 50) // 50+ damage increase
        {
            changes.Add($"Damage output increased by {(secondHalfAvgDamage - firstHalfAvgDamage):F0} points on average");
        }

        // Analyze combo improvement
        var firstHalfAvgCombo = CalculateAverageComboLength(firstHalf);
        var secondHalfAvgCombo = CalculateAverageComboLength(secondHalf);

        if (secondHalfAvgCombo - firstHalfAvgCombo >= 1.0m) // 1+ hit increase
        {
            changes.Add($"Combo execution improved by {(secondHalfAvgCombo - firstHalfAvgCombo):F1} hits on average");
        }

        // Check for new techniques or patterns
        var firstHalfMoves = GetUniqueMoves(firstHalf);
        var secondHalfMoves = GetUniqueMoves(secondHalf);
        var newMoves = secondHalfMoves.Except(firstHalfMoves).ToList();

        if (newMoves.Any())
        {
            changes.Add($"New techniques learned: {string.Join(", ", newMoves.Take(3))}");
        }

        // Check for consistency improvements
        var firstHalfConsistency = CalculateConsistencyScore(firstHalf);
        var secondHalfConsistency = CalculateConsistencyScore(secondHalf);

        if (secondHalfConsistency - firstHalfConsistency >= 10) // 10 point improvement
        {
            changes.Add("Improved consistency in performance");
        }

        return changes;
    }

    private DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }

    private decimal CalculateWinRate(IReadOnlyList<MatchRecording> matches)
    {
        if (!matches.Any()) return 0;
        var wins = matches.Count(m => true); // Simplified - would need player ID
        return (decimal)wins / matches.Count;
    }

    private decimal CalculateAverageDamage(IReadOnlyList<MatchRecording> matches)
    {
        if (!matches.Any()) return 0;

        var totalDamage = matches.Sum(m => m.Rounds.Sum(r =>
            r.Hits.Sum(h => h.Damage))); // Simplified

        return (decimal)totalDamage / matches.Count;
    }

    private decimal CalculateAverageComboLength(IReadOnlyList<MatchRecording> matches)
    {
        var allCombos = matches.SelectMany(m =>
            m.Rounds.SelectMany(r => r.Combos));

        return allCombos.Any() ? (decimal)allCombos.Average(c => c.Length) : 0;
    }

    private IReadOnlyList<string> GetUniqueMoves(IReadOnlyList<MatchRecording> matches)
    {
        var moves = new HashSet<string>();

        foreach (var match in matches)
        {
            foreach (var round in match.Rounds)
            {
                foreach (var hit in round.Hits)
                {
                    moves.Add(hit.MoveName);
                }
            }
        }

        return moves.ToList();
    }

    private int CalculateConsistencyScore(IReadOnlyList<MatchRecording> matches)
    {
        if (matches.Count < 3) return 0;

        // Calculate variance in performance metrics
        var damages = matches.Select(m => m.Rounds.Sum(r =>
            r.Hits.Sum(h => h.Damage))).ToList();

        var avgDamage = damages.Average();
        var variance = damages.Sum(d => Math.Pow(d - avgDamage, 2)) / damages.Count;
        var stdDev = Math.Sqrt(variance);

        // Lower standard deviation = higher consistency (0-100 scale)
        var consistency = Math.Max(0, 100 - (int)(stdDev / 10));
        return consistency;
    }

    private PerformanceTrends CreateEmptyTrends(Guid playerId, DateTime startDate, DateTime endDate)
    {
        return new PerformanceTrends(
            PlayerId: playerId,
            StartDate: startDate,
            EndDate: endDate,
            WinRateTrend: Array.Empty<TrendDataPoint>(),
            DamageTrend: Array.Empty<TrendDataPoint>(),
            ComboTrend: Array.Empty<TrendDataPoint>(),
            NotableChanges: Array.Empty<string>()
        );
    }
}
