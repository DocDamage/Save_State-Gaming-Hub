using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting objective count values in game memory.
/// Objective count values typically:
/// - Are integers counting completed objectives
/// - Only increase during gameplay
/// - Often reset between missions
/// - Track quest/mission progress
/// </summary>
public sealed class ObjectiveCountHeuristic : IValueHeuristic
{
    public string Name => "Objective Count Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for objective counts
        if (IsInObjectiveRange(value.CurrentValue))
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
            int resets = 0;
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
                    if (currVal.Value == 0 && prevVal.Value > 0)
                    {
                        resets++;
                    }
                    else
                    {
                        decreases++;
                    }
                }
                else
                {
                    constants++;
                }
            }

            // Objective count should mostly increase
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var increaseRatio = (double)increases / totalComparisons;
                if (increaseRatio > 0.5)
                {
                    score += 0.2;
                }
            }

            // Occasional resets between missions are acceptable
            if (resets >= 1)
            {
                score += 0.1;
            }

            // Should have constant periods
            if (constants > 0)
            {
                score += 0.05;
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

    private static bool IsInObjectiveRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Objective counts typically 0-20 per mission
        return val >= 0 && val <= 20;
    }
}