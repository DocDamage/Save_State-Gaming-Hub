using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting energy/stamina values in game memory.
/// Energy values typically:
/// - Are floats (0.0-100.0) or integers (0-100)
/// - Decrease when performing actions (attacks, sprinting, abilities)
/// - Regenerate automatically over time when not in use
/// - Often have a cap/maximum value
/// </summary>
public sealed class EnergyHeuristic : IValueHeuristic
{
    public string Name => "Energy/Stamina Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int consumptionEvents = 0;
        int regenerationEvents = 0;
        bool hasRegenerationPattern = false;

        // Check value range (energy typically 0-100 or 0-1000)
        if (IsInEnergyRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Prefer floats for energy
        if (IsFloatType(value.ValueType))
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

            // Check for consumption (decrease after action)
            if (currVal < prevVal && (curr.RelatedAction == PlayerAction.Attacked || 
                                       curr.RelatedAction == PlayerAction.Sprinted ||
                                       curr.RelatedAction == PlayerAction.UsedAbility))
            {
                consumptionEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Energy typically decreases by reasonable amounts
                if (delta > 0.1 && delta < 50)
                {
                    score += 0.08;
                }
            }

            // Check for regeneration (increase while idle)
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                regenerationEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Regeneration is typically gradual
                if (delta > 0 && delta < 10)
                {
                    hasRegenerationPattern = true;
                    score += 0.05;
                }
            }

            // Energy should not exceed typical caps
            if (currVal > 1000)
            {
                score -= 0.3;
            }

            // Energy should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for consumption pattern
        if (consumptionEvents >= 2)
            score += 0.2;

        // Bonus for regeneration pattern
        if (hasRegenerationPattern && regenerationEvents >= 2)
            score += 0.15;

        // Bonus for consistent range behavior
        if (consumptionEvents + regenerationEvents >= 3)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double" or "int16" or "short";
    }

    private static bool IsInEnergyRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Energy typically in range 0-100 or 0-1000
            var val = doubleValue.Value;
            return val >= 0 && val <= 1000;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsFloatType(string valueType)
    {
        var normalized = valueType.ToLowerInvariant();
        return normalized is "float" or "single" or "double";
    }
}