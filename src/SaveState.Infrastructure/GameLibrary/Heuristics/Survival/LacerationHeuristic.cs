using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting laceration/cut wound values in survival games.
/// Laceration values typically:
/// - Are floats or integers (0.0-100.0 severity)
/// - Occur from sharp objects, combat, or environmental hazards
/// - Cause bleeding over time
/// - Heal with bandages or stitches
/// </summary>
public sealed class LacerationHeuristic : IValueHeuristic
{
    public string Name => "Laceration Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int woundEvents = 0;
        int treatmentEvents = 0;
        bool bleedingPattern = false;

        // Check value range (laceration typically 0-100)
        if (IsInLacerationRange(value.CurrentValue))
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

            // Check for wound from combat/environment
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Attacked)
            {
                var delta = currVal.Value - prevVal.Value;
                // Lacerations from combat
                if (delta > 10 && delta < 60)
                {
                    woundEvents++;
                    score += 0.18;
                }
            }

            // Check for treatment (bandaging)
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Healed)
            {
                var delta = prevVal.Value - currVal.Value;
                // Bandaging reduces severity
                if (delta > 10)
                {
                    treatmentEvents++;
                    score += 0.2;
                }
            }

            // Check for bleeding pattern (worsening without treatment)
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = currVal.Value - prevVal.Value;
                // Untreated lacerations worsen
                if (delta > 0 && delta < 2)
                {
                    bleedingPattern = true;
                    score += 0.1;
                }
            }

            // Check for gradual healing
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = prevVal.Value - currVal.Value;
                // Slow natural healing
                if (delta > 0 && delta < 2)
                {
                    score += 0.08;
                }
            }

            // Laceration should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Laceration typically caps at 100
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Strong bonus for wound events
        if (woundEvents >= 1)
            score += 0.2;

        // Strong bonus for treatment events
        if (treatmentEvents >= 1)
            score += 0.18;

        // Bonus for bleeding pattern
        if (bleedingPattern)
            score += 0.12;

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
            score += 0.12;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double" or "int16" or "short";
    }

    private static bool IsInLacerationRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Laceration typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}