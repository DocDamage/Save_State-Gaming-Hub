using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting thirst/hydration values in survival games.
/// Thirst values typically:
/// - Are floats (0.0-100.0) or integers (0-100)
/// - Decrease over time (often faster than hunger)
/// - Can be restored by drinking beverages
/// - Critical for survival (dehydration effects)
/// </summary>
public sealed class ThirstHeuristic : IValueHeuristic
{
    public string Name => "Thirst/Hydration Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int drinkingEvents = 0;
        int decreaseCount = 0;
        double totalDecreaseRate = 0;

        // Check value range (thirst typically 0-100)
        if (IsInThirstRange(value.CurrentValue))
        {
            score += 0.3;
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

            // Check for drinking (significant increase)
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.UsedItem)
            {
                var delta = currVal.Value - prevVal.Value;
                // Drinking typically restores 10-50 units
                if (delta >= 10 && delta <= 50)
                {
                    drinkingEvents++;
                    score += 0.15;
                }
            }

            // Check for decrease over time
            if (currVal < prevVal)
            {
                var delta = prevVal.Value - currVal.Value;
                // Thirst decreases gradually
                if (delta > 0 && delta < 5)
                {
                    decreaseCount++;
                    totalDecreaseRate += delta;
                }
            }

            // Thirst values should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Thirst values typically cap at 100
            if (currVal > 200)
            {
                score -= 0.3;
            }
        }

        // Bonus for drinking pattern
        if (drinkingEvents >= 1)
            score += 0.2;

        // Thirst typically decreases faster than hunger
        if (decreaseCount >= 3)
        {
            var avgDecrease = totalDecreaseRate / decreaseCount;
            // If decreasing at reasonable rate (0.1-2.0 per tick)
            if (avgDecrease > 0.1 && avgDecrease < 2.0)
            {
                score += 0.15;
            }
        }

        // Check for common max value (100)
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        if (Math.Abs(maxValue - 100) < 5)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double" or "int16" or "short";
    }

    private static bool IsInThirstRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Thirst typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}