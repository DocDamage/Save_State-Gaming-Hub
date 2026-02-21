using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting elemental damage values in game memory.
/// Elemental damage values typically:
/// - Are integers in range 1-10000
/// - Static or change with equipment/enchantments
/// - Often separate from base damage
/// - May have types: fire, ice, lightning, poison, etc.
/// </summary>
public sealed class ElementalDamageHeuristic : IValueHeuristic
{
    public string Name => "Elemental Damage Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int changes = 0;
        int enchantmentCorrelations = 0;

        // Check value range
        if (IsInElementalDamageRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Elemental damage is typically an integer
        if (IsIntegerValue(value.CurrentValue))
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

            var delta = currVal.Value - prevVal.Value;

            // Track changes
            if (Math.Abs(delta) > 0.001)
            {
                changes++;
            }

            // Elemental damage often changes with equipment
            if (Math.Abs(delta) > 5 && Math.Abs(delta) < 500)
            {
                score += 0.08;
            }

            // Check for buff/debuff correlations (via used ability)
            if (curr.RelatedAction == PlayerAction.UsedAbility && delta != 0)
            {
                enchantmentCorrelations++;
            }
        }

        // Elemental damage should be relatively static
        if (history.Count > 1)
        {
            var changeRatio = (double)changes / (history.Count - 1);
            if (changeRatio < 0.15)
            {
                score += 0.2;
            }
        }

        // Bonus for enchantment/buff correlations
        if (enchantmentCorrelations >= 1)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "int16" or "short";
    }

    private static bool IsInElementalDamageRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 10000;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsIntegerValue(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return true;

            return Math.Abs(doubleValue.Value % 1) < 0.0001;
        }
        catch
        {
            return false;
        }
    }
}