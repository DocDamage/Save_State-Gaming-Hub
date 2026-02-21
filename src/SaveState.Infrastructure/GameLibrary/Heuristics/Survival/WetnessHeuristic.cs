using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting wetness level in survival games.
/// Wetness values typically:
/// - Are floats (0.0-100.0) representing percentage
/// - Increase in rain/water
/// - Decrease over time or near fire
/// </summary>
public sealed class WetnessHeuristic : IValueHeuristic
{
    public string Name => "Wetness Level Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increaseEvents = 0;
        int decreaseEvents = 0;

        // Check value range (wetness typically 0-100)
        if (IsInWetnessRange(value.CurrentValue))
        {
            score += 0.35;
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

            // Check for increase (getting wet)
            if (currVal > prevVal)
            {
                increaseEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Getting wet happens relatively quickly
                if (delta > 5 && delta <= 50)
                {
                    score += 0.12;
                }
            }

            // Check for decrease (drying)
            if (currVal < prevVal)
            {
                decreaseEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Drying happens gradually
                if (delta > 0 && delta < 10)
                {
                    score += 0.1;
                }
            }

            // Should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Typically caps at 100
            if (currVal > 200)
            {
                score -= 0.3;
            }
        }

        // Bonus for patterns
        if (increaseEvents >= 2)
            score += 0.15;
        if (decreaseEvents >= 2)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double";
    }

    private static bool IsInWetnessRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 200;
        }
        catch
        {
            return false;
        }
    }
}