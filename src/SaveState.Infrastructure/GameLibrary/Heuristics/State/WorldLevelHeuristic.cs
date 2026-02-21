using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting world level values in game memory.
/// World level values typically:
/// - Are integers representing world/hub numbers
/// - Increase as player progresses
/// - Remain constant within a world
/// - Often reset or carry over between playthroughs
/// </summary>
public sealed class WorldLevelHeuristic : IValueHeuristic
{
    public string Name => "World Level Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for world levels
        if (IsInWorldRange(value.CurrentValue))
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
            int sequentialIncreases = 0;
            int nonSequentialChanges = 0;
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
                    var delta = currVal.Value - lastValue.Value;

                    if (delta == 0)
                    {
                        constants++;
                    }
                    else if (delta == 1)
                    {
                        sequentialIncreases++;
                    }
                    else if (delta != 0 && Math.Abs(delta) > 1)
                    {
                        nonSequentialChanges++;
                    }
                }

                lastValue = currVal.Value;
            }

            // World level should remain constant for extended periods
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var constantRatio = (double)constants / totalComparisons;
                if (constantRatio > 0.8)
                {
                    score += 0.2;
                }
            }

            // Sequential progression is ideal
            if (sequentialIncreases >= 1)
            {
                score += 0.1;
            }

            // Penalty for non-sequential changes
            if (nonSequentialChanges > 0)
            {
                score -= 0.15 * nonSequentialChanges;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "byte" or "uint8";
    }

    private static bool IsInWorldRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // World levels typically 1-20 or 0-19
        return val >= 0 && val <= 20;
    }
}