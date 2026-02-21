using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting skip count values in game memory.
/// Skip count values typically:
/// - Are integers counting skipped content
/// - Only increase when player skips
/// - Track cutscenes, tutorials, or dialogue skipped
/// - Rarely decrease
/// </summary>
public sealed class SkipCountHeuristic : IValueHeuristic
{
    public string Name => "Skip Count Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for skip counts
        if (IsInSkipRange(value.CurrentValue))
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
                    // Skips are typically single increments
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

            // Skip count should only increase
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var increaseRatio = (double)increases / totalComparisons;
                if (increases > 0 && increaseRatio < 0.4)
                {
                    // Infrequent increases are expected
                    score += 0.25;
                }
                else if (increases > 0)
                {
                    score += 0.1;
                }

                // Bonus for single increments
                if (singleIncreases == increases && increases > 0)
                {
                    score += 0.1;
                }
            }

            // Decreases are invalid
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

    private static bool IsInSkipRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Skip counts typically 0-100
        return val >= 0 && val <= 100;
    }
}