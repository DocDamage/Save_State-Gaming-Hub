using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting map ID values in game memory.
/// Map ID values typically:
/// - Are integers representing current map/level identifiers
/// - Change when loading new maps
/// - Remain constant on the same map
/// - Often non-contiguous values
/// </summary>
public sealed class MapIDHeuristic : IValueHeuristic
{
    public string Name => "Map ID Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for map IDs
        if (IsInMapIDRange(value.CurrentValue))
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
        if (history.Count >= 3)
        {
            int constants = 0;
            int changes = 0;
            double? lastValue = null;

            for (int i = 0; i < history.Count; i++)
            {
                var obs = history[i];
                if (obs.Value == null)
                    continue;

                var currVal = HeuristicUtilities.ConvertToDouble(obs.Value);
                if (!currVal.HasValue)
                    continue;

                if (lastValue.HasValue)
                {
                    if (currVal.Value == lastValue.Value)
                    {
                        constants++;
                    }
                    else
                    {
                        changes++;
                    }
                }

                lastValue = currVal.Value;
            }

            // Map ID should remain constant for extended periods
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

            // Changes are expected but infrequent
            if (changes >= 1 && changes <= 3)
            {
                score += 0.1;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "byte" or "uint8";
    }

    private static bool IsInMapIDRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Map IDs typically 0-999
        return val >= 0 && val <= 999;
    }
}