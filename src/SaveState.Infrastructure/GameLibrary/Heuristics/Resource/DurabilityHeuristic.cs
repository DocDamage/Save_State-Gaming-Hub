using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting equipment durability/condition in RPGs and survival games.
/// Durability values typically:
/// - Are integers (0-100) or floats representing percentage
/// - Decrease slowly with use
/// - Can be repaired to restore
/// - At 0, item breaks or becomes unusable
/// </summary>
public sealed class DurabilityHeuristic : IValueHeuristic
{
    public string Name => "Durability/Condition Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int decreaseEvents = 0;
        int repairEvents = 0;
        int maxValue = 0;

        // Check value range (durability typically 0-100)
        if (IsInDurabilityRange(value.CurrentValue))
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

            // Track max value
            if (currVal > maxValue)
                maxValue = (int)currVal.Value;

            // Check for gradual decrease (wear and tear)
            if (currVal < prevVal)
            {
                decreaseEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Durability decreases by small amounts (1-5 typically)
                if (delta > 0 && delta <= 5)
                {
                    score += 0.1;
                }
                else if (delta > 5 && delta <= 10)
                {
                    // Larger decrease might be from intense use
                    score += 0.05;
                }
            }

            // Check for repair (increase, typically to max or significant amount)
            if (currVal > prevVal)
            {
                var delta = currVal.Value - prevVal.Value;
                // Repair typically restores significant amount
                if (delta > 10)
                {
                    repairEvents++;
                    score += 0.15;
                }
            }

            // Durability should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Durability typically caps at 100 (percentage)
            if (currVal > 1000)
            {
                score -= 0.3;
            }
        }

        // Bonus for wear pattern
        if (decreaseEvents >= 2)
            score += 0.15;

        // Bonus for repair events
        if (repairEvents >= 1)
            score += 0.1;

        // Strong indicator: max value is 100 (percentage-based durability)
        if (maxValue == 100 || (maxValue >= 98 && maxValue <= 102))
        {
            score += 0.25;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double" or "int16" or "short";
    }

    private static bool IsInDurabilityRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Durability typically in range 0-1000 (percentage or raw values)
            var val = doubleValue.Value;
            return val >= 0 && val <= 1000;
        }
        catch
        {
            return false;
        }
    }
}