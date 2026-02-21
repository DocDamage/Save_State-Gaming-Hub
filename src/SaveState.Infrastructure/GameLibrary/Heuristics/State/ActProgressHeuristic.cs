using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting act progress values in game memory.
/// Act progress values typically:
/// - Are integers representing act numbers (1-5 typically)
/// - Increase sequentially
/// - Remain constant throughout an act
/// - Often correlate with major story milestones
/// </summary>
public sealed class ActProgressHeuristic : IValueHeuristic
{
    public string Name => "Act Progress Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for act numbers
        if (IsInActRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Acts should be integers
        if (HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score += 0.25;
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

            // Acts should remain constant for long periods
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var constantRatio = (double)constants / totalComparisons;
                if (constantRatio > 0.8)
                {
                    score += 0.25;
                }
                else if (constantRatio > 0.6)
                {
                    score += 0.1;
                }
            }

            // Sequential act progression is ideal
            if (sequentialIncreases >= 1)
            {
                score += 0.15;
            }

            // Penalty for non-sequential jumps
            if (nonSequentialChanges > 0)
            {
                score -= 0.2 * nonSequentialChanges;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "byte" or "uint8";
    }

    private static bool IsInActRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Acts typically numbered 1-5, sometimes 0-4
        return val >= 0 && val <= 10;
    }
}