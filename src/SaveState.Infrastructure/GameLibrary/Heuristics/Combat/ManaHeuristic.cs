using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting mana/magic/energy resource values in game memory.
/// Mana values typically:
/// - Are floats or integers in range 0-1000
/// - Fluctuate (use spell -> decreases, regen -> increases)
/// - Similar pattern to health but for magic
/// </summary>
public sealed class ManaHeuristic : IValueHeuristic
{
    public string Name => "Mana Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int decreases = 0;
        int increases = 0;
        int spellUseEvents = 0;

        // Check value range
        if (IsInManaRange(value.CurrentValue))
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

            var delta = currVal.Value - prevVal.Value;

            // Track decreases (spell usage)
            if (delta < 0)
            {
                decreases++;
                spellUseEvents++;
            }

            // Track increases (regeneration)
            if (delta > 0)
            {
                increases++;
            }

            // Mana should never be negative
            if (currVal.Value < 0)
            {
                score -= 0.3;
            }
        }

        // Mana should have both decreases (usage) and increases (regen)
        if (decreases >= 1 && increases >= 1)
        {
            score += 0.25;
        }

        // Fluctuating pattern is key for mana
        if (decreases + increases >= 3)
        {
            score += 0.2;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "float" or "single" or "int16" or "short";
    }

    private static bool IsInManaRange(object? value)
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
}