using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting achievement count values in game memory.
/// Achievement count values typically:
/// - Are integers from 0 to 100+
/// - Only increase (unlock new achievements)
/// - Rarely decrease
/// - Often increase by 1 at a time
/// </summary>
public sealed class AchievementCountHeuristic : IValueHeuristic
{
    public string Name => "Achievement Count Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for achievement counts
        if (IsInAchievementRange(value.CurrentValue))
        {
            score += 0.3;
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
        if (history.Count >= 2)
        {
            int increases = 0;
            int decreases = 0;
            int singleIncreases = 0;

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
                    // Achievements typically unlock one at a time
                    if (delta == 1)
                    {
                        singleIncreases++;
                    }
                }
                else if (delta < 0)
                {
                    decreases++;
                }
            }

            // Achievement count should only increase
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var increaseRatio = (double)increases / totalComparisons;
                if (increaseRatio > 0.8)
                {
                    score += 0.25;
                }

                // Bonus for single increments
                var singleIncreaseRatio = (double)singleIncreases / totalComparisons;
                if (singleIncreaseRatio > 0.5)
                {
                    score += 0.15;
                }
            }

            // Decreases are very rare
            if (decreases > 0)
            {
                score -= 0.2 * decreases;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "byte" or "uint8";
    }

    private static bool IsInAchievementRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Achievement counts typically 0-1000
        return val >= 0 && val <= 1000;
    }
}