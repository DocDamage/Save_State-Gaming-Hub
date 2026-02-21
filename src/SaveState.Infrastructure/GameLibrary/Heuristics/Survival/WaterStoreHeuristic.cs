using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting water/hydration reserve values in survival games.
/// Water store values typically:
/// - Are floats or integers (0.0-100.0 or 0-5000 ml)
/// - Increase when drinking
/// - Decrease constantly through metabolism and activity
/// - Critical for survival (faster depletion than food)
/// </summary>
public sealed class WaterStoreHeuristic : IValueHeuristic
{
    public string Name => "Water Store Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int drinkingEvents = 0;
        int consumptionEvents = 0;
        bool constantDepletionPattern = false;

        // Check value range (water store: 0-100 or 0-5000 ml)
        if (IsInWaterStoreRange(value.CurrentValue))
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

            // Check for drinking (sudden increase)
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.UsedItem)
            {
                var delta = currVal.Value - prevVal.Value;
                // Drinking adds to water stores
                if (delta > 50 && delta < 500)
                {
                    drinkingEvents++;
                    score += 0.2;
                }
            }

            // Check for water consumption at rest (constant need)
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = prevVal.Value - currVal.Value;
                // Water depletes even at rest
                if (delta > 0 && delta < 5)
                {
                    consumptionEvents++;
                    constantDepletionPattern = true;
                    score += 0.12;
                }
            }

            // Check for increased consumption during activity
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Sprinted)
            {
                var delta = prevVal.Value - currVal.Value;
                // Activity increases water loss
                if (delta > 2 && delta < 20)
                {
                    consumptionEvents++;
                    score += 0.15;
                }
            }

            // Water store should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Reasonable max values
            if (currVal > 10000)
            {
                score -= 0.3;
            }
        }

        // Strong bonus for drinking events
        if (drinkingEvents >= 1)
            score += 0.18;

        // Bonus for consumption events
        if (consumptionEvents >= 2)
            score += 0.12;

        // Strong bonus for constant depletion (distinctive of water)
        if (constantDepletionPattern)
            score += 0.2;

        // Check for value ranges
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        // Common caps: 100 (percentage), 2000-4000 ml (daily water)
        if (Math.Abs(maxValue - 100) < 5 || (maxValue >= 2000 && maxValue <= 4000))
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double" or "int16" or "short";
    }

    private static bool IsInWaterStoreRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Water store: 0-100 (%) or 0-4000 ml
            var val = doubleValue.Value;
            return val >= 0 && val <= 4000;
        }
        catch
        {
            return false;
        }
    }
}