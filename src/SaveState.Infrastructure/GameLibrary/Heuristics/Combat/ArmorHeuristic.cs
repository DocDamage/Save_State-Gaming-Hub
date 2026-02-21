using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting armor/damage reduction values in games.
/// Armor values typically:
/// - Are integers (0-1000) or floats representing percentage
/// - Reduce incoming damage when equipped
/// - Can degrade with use or be repaired
/// - Stack with other damage reduction sources
/// </summary>
public sealed class ArmorHeuristic : IValueHeuristic
{
    public string Name => "Armor/Damage Reduction Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int damageReductionEvents = 0;
        bool stableDuringCombat = true;

        // Check value range (armor typically 0-1000)
        if (IsInArmorRange(value.CurrentValue))
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

            // Armor typically stays stable during combat (unlike health/shield)
            if (curr.RelatedAction == PlayerAction.TookDamage)
            {
                if (Math.Abs(currVal.Value - prevVal.Value) > 1)
                {
                    stableDuringCombat = false;
                }
                else
                {
                    // Armor might degrade slightly when taking damage
                    if (currVal < prevVal)
                    {
                        damageReductionEvents++;
                        var delta = prevVal.Value - currVal.Value;
                        if (delta > 0 && delta < 5)
                        {
                            score += 0.1;
                        }
                    }
                }
            }

            // Armor should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Armor values rarely exceed 1000
            if (currVal > 10000)
            {
                score -= 0.3;
            }
        }

        // Bonus for stability during combat (armor doesn't fluctuate like health)
        if (stableDuringCombat)
            score += 0.2;

        // Bonus for damage reduction pattern
        if (damageReductionEvents >= 1 && damageReductionEvents <= 5)
            score += 0.15;

        // Check for common armor values
        var avgValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Average();

        // Common armor values: 0-100 (percentage), 100-1000 (rating systems)
        if ((avgValue >= 0 && avgValue <= 100) || (avgValue >= 50 && avgValue <= 1000))
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

    private static bool IsInArmorRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Armor typically in range 0-10000
            var val = doubleValue.Value;
            return val >= 0 && val <= 10000;
        }
        catch
        {
            return false;
        }
    }
}