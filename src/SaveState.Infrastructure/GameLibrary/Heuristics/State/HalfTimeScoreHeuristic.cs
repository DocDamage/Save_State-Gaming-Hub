using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting half-time score values in game memory.
/// Half-time score values typically:
/// - Are integers
/// - Remain constant after half-time
/// - Show team scores (0-20 range typical)
/// - Often appear in pairs (home and away)
/// </summary>
public sealed class HalfTimeScoreHeuristic : IValueHeuristic
{
    public string Name => "Half-Time Score Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for sports scores
        if (IsInScoreRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Scores should be integers
        if (HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score += 0.2;
        }

        // Non-negative values only
        if (HeuristicUtilities.IsNonNegative(value.CurrentValue))
        {
            score += 0.1;
        }

        // Analyze observation history
        if (history.Count >= 3)
        {
            int constants = 0;
            int increases = 0;

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

                // Half-time scores should be mostly constant
                if (delta == 0)
                {
                    constants++;
                }
                // May increase slightly during second half analysis
                else if (delta > 0 && delta <= 5)
                {
                    increases++;
                }
            }

            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var constantRatio = (double)constants / totalComparisons;
                if (constantRatio > 0.85)
                {
                    score += 0.3;
                }
                else if (constantRatio > 0.6)
                {
                    score += 0.1;
                }
            }

            // Occasional small increases are acceptable
            if (increases <= 2)
            {
                score += 0.1;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long";
    }

    private static bool IsInScoreRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        return val >= 0 && val <= 50;
    }
}