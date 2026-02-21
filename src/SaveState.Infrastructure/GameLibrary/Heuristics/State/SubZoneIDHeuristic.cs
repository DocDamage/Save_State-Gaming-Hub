using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting sub-zone ID values in game memory.
/// Sub-zone ID values typically:
/// - Are integers representing sub-areas within zones
/// - Change frequently during exploration
/// - Often small values (0-50)
/// - Can change back and forth
/// </summary>
public sealed class SubZoneIDHeuristic : IValueHeuristic
{
    public string Name => "Sub-Zone ID Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for sub-zone IDs
        if (IsInSubZoneRange(value.CurrentValue))
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
            int smallChanges = 0;
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
                        if (Math.Abs(delta) <= 5)
                        {
                            smallChanges++;
                        }
                    }
                }

                lastValue = currVal.Value;
            }

            // Sub-zone ID should have periods of constancy
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var constantRatio = (double)constants / totalComparisons;
                if (constantRatio > 0.4 && constantRatio < 0.8)
                {
                    score += 0.2;
                }
            }

            // Changes should be relatively small
            if (changes > 0)
            {
                var smallChangeRatio = (double)smallChanges / changes;
                if (smallChangeRatio > 0.7)
                {
                    score += 0.1;
                }
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "byte" or "uint8";
    }

    private static bool IsInSubZoneRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Sub-zone IDs typically 0-100
        return val >= 0 && val <= 100;
    }
}