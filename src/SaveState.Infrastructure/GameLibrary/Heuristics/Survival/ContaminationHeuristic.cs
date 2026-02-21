using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting contamination/poison buildup values in survival games.
/// Contamination values typically:
/// - Are floats or integers (0.0-100.0)
/// - Increase when consuming contaminated food/water or exposure to toxins
/// - Decrease slowly through natural body filtration or with antidotes
/// - Can cause sickness, damage, or death at high levels
/// </summary>
public sealed class ContaminationHeuristic : IValueHeuristic
{
    public string Name => "Contamination Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int contaminationEvents = 0;
        int detoxEvents = 0;
        bool gradualFiltrationPattern = false;

        // Check value range (contamination typically 0-100)
        if (IsInContaminationRange(value.CurrentValue))
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

            // Check for contamination increase (consuming bad items)
            if (currVal > prevVal && (curr.RelatedAction == PlayerAction.UsedItem || 
                                       curr.RelatedAction == PlayerAction.Healed))
            {
                var delta = currVal.Value - prevVal.Value;
                // Contamination usually jumps when consuming contaminated items
                if (delta > 5 && delta < 50)
                {
                    contaminationEvents++;
                    score += 0.15;
                }
            }

            // Check for natural filtration (slow decrease while idle)
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = prevVal.Value - currVal.Value;
                // Natural filtration is very slow
                if (delta > 0 && delta < 1)
                {
                    gradualFiltrationPattern = true;
                    score += 0.08;
                }
            }

            // Check for detox (rapid decrease with antidote)
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.UsedItem)
            {
                var delta = prevVal.Value - currVal.Value;
                // Detox is rapid and significant
                if (delta > 15)
                {
                    detoxEvents++;
                    score += 0.18;
                }
            }

            // Contamination should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Contamination typically caps at 100
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Bonus for contamination events
        if (contaminationEvents >= 1)
            score += 0.15;

        // Strong bonus for gradual filtration (distinctive)
        if (gradualFiltrationPattern)
            score += 0.2;

        // Bonus for detox events
        if (detoxEvents >= 1)
            score += 0.15;

        // Check for max value near 100
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

    private static bool IsInContaminationRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Contamination typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}