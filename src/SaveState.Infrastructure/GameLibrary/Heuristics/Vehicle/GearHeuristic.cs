using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting gear/transmission in racing games.
/// Gear values typically:
/// - Are integers (-1 to 10)
/// - Change sequentially
/// - -1 is reverse, 0 is neutral
/// </summary>
public sealed class GearHeuristic : IValueHeuristic
{
    public string Name => "Transmission Gear Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool sequentialChanges = true;
        int gearChanges = 0;

        // Check value range (gears -1 to 10)
        if (IsInGearRange(value.CurrentValue))
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

            // Check for gear change
            if (currVal != prevVal)
            {
                gearChanges++;
                var delta = Math.Abs(currVal.Value - prevVal.Value);
                // Gears usually change by 1
                if (delta == 1)
                {
                    score += 0.2;
                }
                else if (delta > 2)
                {
                    sequentialChanges = false;
                }
            }

            // Check for reverse (-1)
            if (currVal == -1)
            {
                score += 0.15;
            }

            // Check for neutral (0)
            if (currVal == 0)
            {
                score += 0.1;
            }

            // Reasonable gear range
            if (currVal < -1 || currVal > 10)
            {
                score -= 0.4;
            }
        }

        // Bonus for sequential changes
        if (sequentialChanges && gearChanges > 0)
            score += 0.2;

        // Bonus for gear changes
        if (gearChanges >= 2)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "sbyte";
    }

    private static bool IsInGearRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -1 && val <= 10;
        }
        catch
        {
            return false;
        }
    }
}