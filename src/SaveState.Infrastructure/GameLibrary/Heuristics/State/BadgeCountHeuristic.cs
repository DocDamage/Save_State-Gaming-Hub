using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting badge count values in game memory.
/// Badge count values typically:
/// - Are integers representing earned badges
/// - Only increase over time
/// - Rarely reset
/// - Often correlate with player milestones
/// </summary>
public sealed class BadgeCountHeuristic : IValueHeuristic
{
    public string Name => "Badge Count Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for badge counts
        if (IsInBadgeRange(value.CurrentValue))
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
            int constants = 0;

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
                }
                else if (delta < 0)
                {
                    decreases++;
                }
                else
                {
                    constants++;
                }
            }

            // Badge counts should only increase or stay constant
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var increaseRatio = (double)increases / totalComparisons;
                var constantRatio = (double)constants / totalComparisons;

                if (increaseRatio > 0.3 && increaseRatio < 0.7)
                {
                    score += 0.25;
                }
                else if (constantRatio > 0.5)
                {
                    score += 0.15;
                }
            }

            // Decreases are very rare for badges
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

    private static bool IsInBadgeRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Badge counts typically 0-500
        return val >= 0 && val <= 500;
    }
}