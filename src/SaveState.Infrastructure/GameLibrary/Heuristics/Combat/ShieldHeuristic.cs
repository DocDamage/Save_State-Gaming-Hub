using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting shield/barrier values in game memory.
/// Shield values typically:
/// - Are integers or floats (0 to max shield capacity)
/// - Decrease when taking damage (absorbing hits)
/// - Often regenerate slowly or require items/abilities to restore
/// - Usually cap at a specific maximum value per character/class
/// </summary>
public sealed class ShieldHeuristic : IValueHeuristic
{
    public string Name => "Shield/Barrier Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int damageAbsorptionEvents = 0;
        int breakEvents = 0;
        double? maxObservedValue = null;

        // Check value range (shield typically 0-500 or 0-1000)
        if (IsInShieldRange(value.CurrentValue))
        {
            score += 0.25;
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

            // Track max observed value for cap detection
            maxObservedValue = Math.Max(maxObservedValue ?? currVal.Value, currVal.Value);

            // Check for damage absorption (decrease after taking damage)
            if (curr.RelatedAction == PlayerAction.TookDamage && currVal < prevVal)
            {
                damageAbsorptionEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Shield absorbs damage without affecting health
                if (delta > 0 && delta < 200)
                {
                    score += 0.1;
                }
            }

            // Check for shield break (goes to 0)
            if (currVal == 0 && prevVal > 0)
            {
                breakEvents++;
                score += 0.15;
            }

            // Shield values should not go negative
            if (currVal < 0)
            {
                score -= 0.4;
            }

            // Shield values should stay within reasonable bounds
            if (currVal > 5000)
            {
                score -= 0.2;
            }
        }

        // Bonus for damage absorption pattern
        if (damageAbsorptionEvents >= 2)
            score += 0.2;

        // Bonus for shield break events (distinctive pattern)
        if (breakEvents >= 1)
            score += 0.15;

        // Check for consistent max value (shields often have fixed caps)
        if (maxObservedValue.HasValue && maxObservedValue.Value > 0)
        {
            // Common shield caps: 50, 100, 150, 200, 250, 500, 1000
            var commonCaps = new[] { 50, 100, 150, 200, 250, 500, 1000 };
            foreach (var cap in commonCaps)
            {
                if (Math.Abs(maxObservedValue.Value - cap) < 5)
                {
                    score += 0.1;
                    break;
                }
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "float" or "single" or "int16" or "short" or "double";
    }

    private static bool IsInShieldRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Shield typically in range 0-5000
            var val = doubleValue.Value;
            return val >= 0 && val <= 5000;
        }
        catch
        {
            return false;
        }
    }
}