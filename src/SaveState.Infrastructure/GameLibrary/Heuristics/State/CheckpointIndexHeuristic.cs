using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting checkpoint index values in game memory.
/// Checkpoint index values typically:
/// - Are integers representing checkpoint IDs
/// - Increase as player reaches new checkpoints
/// - Reset to 0 at level start
/// - Never decrease during a level
/// </summary>
public sealed class CheckpointIndexHeuristic : IValueHeuristic
{
    public string Name => "Checkpoint Index Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for checkpoint indices
        if (IsInCheckpointRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Should be integer
        if (HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score += 0.25;
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
                    if (currVal.Value == 0)
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

            // Checkpoint index should mostly increase or stay constant
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var increaseConstRatio = (double)(increases + constants) / totalComparisons;
                if (increaseConstRatio > 0.9)
                {
                    score += 0.2;
                }
            }

            // Resets on level restart are acceptable
            if (resets >= 1)
            {
                score += 0.05;
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

    private static bool IsInCheckpointRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Checkpoints typically 0-50 per level
        return val >= 0 && val <= 50;
    }
}