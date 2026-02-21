using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting season progress values in game memory.
/// Season progress values typically:
/// - Are floats from 0.0 to 100.0 (percentage) or integers (levels)
/// - Increase slowly over time
/// - Show tier/level progression in seasonal content
/// - May have seasonal resets
/// </summary>
public sealed class SeasonProgressHeuristic : IValueHeuristic
{
    public string Name => "Season Progress Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range
        if (IsInSeasonRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Non-negative
        if (HeuristicUtilities.IsNonNegative(value.CurrentValue))
        {
            score += 0.1;
        }

        // Analyze observation history
        if (history.Count >= 2)
        {
            int increases = 0;
            int decreases = 0;
            int smallIncreases = 0;

            for (int i = 1; i < history.Count; i++)
            {
                var prev = history[i - 1];
                var curr = history[i];

                if (prev.Value == null || curr.Value == null)
                    continue;

                double? prevVal = HeuristicUtilities.ConvertToDouble(prev.Value);
                double? currVal = HeuristicUtilities.ConvertToDouble(curr.Value);

                if (!prevVal.HasValue || !currVal.HasValue)
                    continue;

                var delta = currVal.Value - prevVal.Value;

                if (delta > 0)
                {
                    increases++;
                    // Small increments typical of season progress
                    if (delta <= 10)
                    {
                        smallIncreases++;
                    }
                }
                else if (delta < 0)
                {
                    decreases++;
                }
            }

            // Season progress should mostly increase
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var increaseRatio = (double)increases / totalComparisons;
                if (increaseRatio > 0.6)
                {
                    score += 0.25;
                }

                // Bonus for gradual progression
                var smallIncreaseRatio = (double)smallIncreases / totalComparisons;
                if (smallIncreaseRatio > 0.5)
                {
                    score += 0.2;
                }
            }

            // Penalty for decreases (except seasonal reset)
            if (decreases > 1)
            {
                score -= 0.15 * (decreases - 1);
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "float" or "single" or "double";
    }

    private static bool IsInSeasonRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Season tier (0-1000) or percentage (0-100)
        return (val >= 0 && val <= 100) || (val >= 0 && val <= 1000);
    }
}