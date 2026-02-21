using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting energy/calorie reserve values in survival games.
/// Energy store values typically:
/// - Are floats or integers (0.0-100.0 or 0-5000 calories)
/// - Built up from eating nutritious food
/// - Consumed during physical activity and cold exposure
/// - Buffer between hunger and health loss
/// </summary>
public sealed class EnergyStoreHeuristic : IValueHeuristic
{
    public string Name => "Energy Store Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int storageEvents = 0;
        int consumptionEvents = 0;
        bool longTermPattern = false;

        // Check value range (energy store: 0-100 or 0-5000 calories)
        if (IsInEnergyStoreRange(value.CurrentValue))
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

            // Check for energy storage from eating
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.UsedItem)
            {
                var delta = currVal.Value - prevVal.Value;
                // Nutritious food adds to energy stores
                if (delta > 50 && delta < 500)
                {
                    storageEvents++;
                    score += 0.18;
                }
            }

            // Check for energy consumption during activity
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Sprinted)
            {
                var delta = prevVal.Value - currVal.Value;
                // Activity burns stored energy
                if (delta > 1 && delta < 50)
                {
                    consumptionEvents++;
                    score += 0.12;
                }
            }

            // Check for slow consumption at rest (basal metabolic rate)
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = prevVal.Value - currVal.Value;
                // Basal consumption is very slow
                if (delta > 0 && delta < 2)
                {
                    consumptionEvents++;
                    longTermPattern = true;
                    score += 0.08;
                }
            }

            // Energy store should not go negative
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

        // Bonus for storage events
        if (storageEvents >= 1)
            score += 0.15;

        // Bonus for consumption events
        if (consumptionEvents >= 2)
            score += 0.12;

        // Bonus for long-term pattern (energy stores persist)
        if (longTermPattern)
            score += 0.15;

        // Check for value ranges that suggest calories vs percentage
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        // Common caps: 100 (percentage), 2000-5000 (calories)
        if (Math.Abs(maxValue - 100) < 5 || (maxValue >= 2000 && maxValue <= 5000))
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

    private static bool IsInEnergyStoreRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Energy store: 0-100 (%) or 0-5000 (calories)
            var val = doubleValue.Value;
            return val >= 0 && val <= 5000;
        }
        catch
        {
            return false;
        }
    }
}