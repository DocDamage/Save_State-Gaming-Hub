using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting area ID values in game memory.
/// Area ID values typically:
/// - Are integers representing unique area identifiers
/// - Change when entering new areas
/// - Remain constant within an area
/// - Often non-sequential (IDs can jump)
/// </summary>
public sealed class AreaIDHeuristic : IValueHeuristic
{
    public string Name => "Area ID Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for area IDs
        if (IsInAreaRange(value.CurrentValue))
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
            int sequentialChanges = 0;
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
                    else
                    {
                        changes++;
                        if (Math.Abs(delta) == 1)
                        {
                            sequentialChanges++;
                        }
                    }
                }

                lastValue = currVal.Value;
            }

            // Area ID should remain constant for extended periods
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var constantRatio = (double)constants / totalComparisons;
                if (constantRatio > 0.7)
                {
                    score += 0.25;
                }
            }

            // Changes are expected but infrequent
            if (changes >= 1 && changes <= 5)
            {
                score += 0.05;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "byte" or "uint8";
    }

    private static bool IsInAreaRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Area IDs typically 0-9999
        return val >= 0 && val <= 9999;
    }
}