using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting trophy count values in game memory.
/// Trophy count values typically:
/// - Are integers from 0 to 50+ (platinum/achievements)
/// - Only increase when trophies are earned
/// - Often grouped by type (bronze, silver, gold, platinum)
/// - Remain constant between trophy unlocks
/// </summary>
public sealed class TrophyCountHeuristic : IValueHeuristic
{
    public string Name => "Trophy Count Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for trophy counts
        if (IsInTrophyRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Should be integer
        if (HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score += 0.2;
        }

        // Non-negative
        if (HeuristicUtilities.IsNonNegative(value.CurrentValue))
        {
            score += 0.1;
        }

        // Analyze observation history
        if (history.Count >= 3)
        {
            int constants = 0;
            int increases = 0;
            int decreases = 0;

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

                if (delta == 0)
                {
                    constants++;
                }
                else if (delta > 0)
                {
                    increases++;
                }
                else if (delta < 0)
                {
                    decreases++;
                }
            }

            // Trophy counts are mostly constant
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var constantRatio = (double)constants / totalComparisons;
                if (constantRatio > 0.8)
                {
                    score += 0.25;
                }
            }

            // Only increase when earned
            if (increases >= 1 && increases <= 3)
            {
                score += 0.1;
            }

            // Decreases are invalid
            if (decreases > 0)
            {
                score -= 0.3 * decreases;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "byte" or "uint8";
    }

    private static bool IsInTrophyRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Trophy counts typically 0-100 per type
        return val >= 0 && val <= 100;
    }
}