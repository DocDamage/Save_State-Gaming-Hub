using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting burn severity values in survival games.
/// Burn values typically:
/// - Are floats or integers (0.0-100.0 severity or 1st/2nd/3rd degree)
/// - Occur from fire, heat, electricity, or chemicals
/// - Cause ongoing damage and pain
/// - Require specific burn treatments to heal
/// </summary>
public sealed class BurnSeverityHeuristic : IValueHeuristic
{
    public string Name => "Burn Severity Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int burnEvents = 0;
        int treatmentEvents = 0;
        bool ongoingDamagePattern = false;

        // Check value range (burn typically 0-100 or 1-3 degrees)
        if (IsInBurnRange(value.CurrentValue))
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

            // Check for burn from fire/heat (sudden increase)
            if (currVal > prevVal && (curr.RelatedAction == PlayerAction.Attacked || 
                                       curr.RelatedAction == PlayerAction.Moved))
            {
                var delta = currVal.Value - prevVal.Value;
                // Burns occur suddenly from exposure
                if (delta > 15 && delta < 70)
                {
                    burnEvents++;
                    score += 0.2;
                }
            }

            // Check for specific burn treatment
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Healed)
            {
                var delta = prevVal.Value - currVal.Value;
                // Burn treatment is specialized
                if (delta > 10)
                {
                    treatmentEvents++;
                    score += 0.18;
                }
            }

            // Check for ongoing damage (burns worsen without treatment)
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = currVal.Value - prevVal.Value;
                // Burns can spread or deepen
                if (delta > 0 && delta < 3)
                {
                    ongoingDamagePattern = true;
                    score += 0.12;
                }
            }

            // Check for degree-based system (1, 2, 3)
            if (HeuristicUtilities.IsIntegerValue(currVal.Value) && 
                (currVal == 1 || currVal == 2 || currVal == 3))
            {
                score += 0.15;
            }

            // Burn should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Reasonable max values
            if (currVal > 100 && currVal != 3)
            {
                score -= 0.2;
            }
        }

        // Strong bonus for burn events
        if (burnEvents >= 1)
            score += 0.2;

        // Bonus for treatment events
        if (treatmentEvents >= 1)
            score += 0.15;

        // Bonus for ongoing damage pattern
        if (ongoingDamagePattern)
            score += 0.15;

        // Check for max value (100 or 3 degrees)
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        if (Math.Abs(maxValue - 100) < 5 || Math.Abs(maxValue - 3) < 0.5)
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

    private static bool IsInBurnRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Burn severity: 0-100 or degrees 1-3
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}