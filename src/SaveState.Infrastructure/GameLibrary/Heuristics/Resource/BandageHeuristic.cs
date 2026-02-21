using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting bandage/medical supply count in survival/action games.
/// Bandage values typically:
/// - Are integers (0-50)
/// - Increase when looting or crafting
/// - Decrease when healing wounds
/// </summary>
public sealed class BandageHeuristic : IValueHeuristic
{
    public string Name => "Bandages/Medical Supplies Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gainEvents = 0;
        int useEvents = 0;

        // Check value range (bandages typically 0-50, very limited stack)
        if (IsInBandageRange(value.CurrentValue))
        {
            score += 0.45;
        }

        // Must be integer
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.2;
        }
        else
        {
            score += 0.1;
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

            // Check for gain (looting/crafting)
            if (currVal > prevVal)
            {
                gainEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Usually gain 1-3 bandages at a time
                if (delta >= 1 && delta <= 5)
                {
                    score += 0.15;
                }
            }

            // Check for use (applying bandage)
            if (currVal < prevVal)
            {
                useEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Using bandage decreases by exactly 1
                if (delta == 1)
                {
                    score += 0.25;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for usage pattern (bandages used one at a time for healing)
        if (useEvents >= 2)
            score += 0.2;
        if (gainEvents >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInBandageRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 500;
        }
        catch
        {
            return false;
        }
    }
}