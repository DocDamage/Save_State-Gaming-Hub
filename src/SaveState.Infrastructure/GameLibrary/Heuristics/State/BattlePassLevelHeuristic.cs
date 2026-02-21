using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting battle pass level values in game memory.
/// Battle pass level values typically:
/// - Are integers from 1 to 100+ (tier levels)
/// - Only increase
/// - Reset at the start of each season
/// - Often correlate with experience gained
/// </summary>
public sealed class BattlePassLevelHeuristic : IValueHeuristic
{
    public string Name => "Battle Pass Level Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for battle pass tiers
        if (IsInBattlePassRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Should be integer level
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
            int sequentialIncreases = 0;

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
                    if (delta == 1)
                    {
                        sequentialIncreases++;
                    }
                }
                else if (delta < 0)
                {
                    decreases++;
                }
            }

            // Battle pass levels should mostly increase
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var increaseRatio = (double)increases / totalComparisons;
                if (increaseRatio > 0.7)
                {
                    score += 0.25;
                }
                else if (increaseRatio > 0.5)
                {
                    score += 0.1;
                }
            }

            // Sequential level ups are ideal
            if (sequentialIncreases >= 1)
            {
                score += 0.15;
            }

            // Decreases are rare (only on season reset)
            if (decreases > 1)
            {
                score -= 0.3;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "byte" or "uint8";
    }

    private static bool IsInBattlePassRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Battle pass typically 1-200 tiers
        return val >= 0 && val <= 200;
    }
}