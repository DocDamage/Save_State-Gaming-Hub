using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting unlocked fast travel point count.
/// Fast travel count values typically:
/// - Are integers (0-100)
/// - Increase when unlocking waypoints/shrines/sites of grace
/// - Never decrease
/// </summary>
public sealed class FastTravelCountHeuristic : IValueHeuristic
{
    public string Name => "Fast Travel Points Unlocked Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool onlyIncreases = true;
        int unlockEvents = 0;

        // Check value range (fast travel points typically 0-150)
        if (IsInFastTravelRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Must be integer
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.3;
        }
        else
        {
            score += 0.15;
        }

        // Analyze observation history
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

            // Should only increase by 1 (single unlock)
            var delta = currVal.Value - prevVal.Value;
            if (delta == 1)
            {
                unlockEvents++;
                score += 0.2;
            }
            else if (delta > 1 && delta <= 3)
            {
                score += 0.1;
            }
            else if (delta < 0)
            {
                onlyIncreases = false;
                score -= 0.4;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Reasonable maximum
            if (currVal > 300)
            {
                score -= 0.3;
            }
        }

        // Bonus for unlock patterns
        if (unlockEvents >= 1)
            score += 0.15;

        // Bonus for only increasing
        if (onlyIncreases && history.Count > 2)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInFastTravelRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 300;
        }
        catch
        {
            return false;
        }
    }
}