using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting sun protection/SPF values in survival games.
/// Sun protection values typically:
/// - Are floats or integers (0.0-100.0 protection level)
/// - Decrease over time when exposed to sun
/// - Replenished by applying sunscreen or wearing protective gear
/// - Prevent sunburn and heatstroke
/// </summary>
public sealed class SunProtectionHeuristic : IValueHeuristic
{
    public string Name => "Sun Protection Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int applicationEvents = 0;
        int depletionEvents = 0;
        bool gradualDepletionPattern = false;

        // Check value range (sun protection typically 0-100)
        if (IsInSunProtectionRange(value.CurrentValue))
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

            // Check for protection application (sudden increase)
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.UsedItem)
            {
                var delta = currVal.Value - prevVal.Value;
                // Applying protection gives large boost
                if (delta > 20 && delta < 100)
                {
                    applicationEvents++;
                    score += 0.2;
                }
            }

            // Check for gradual depletion in sun
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = prevVal.Value - currVal.Value;
                // Protection wears off slowly
                if (delta > 0 && delta < 3)
                {
                    depletionEvents++;
                    gradualDepletionPattern = true;
                    score += 0.1;
                }
            }

            // Check for faster depletion when moving
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Sprinted)
            {
                var delta = prevVal.Value - currVal.Value;
                // Sweating reduces protection faster
                if (delta > 0 && delta < 5)
                {
                    depletionEvents++;
                    score += 0.08;
                }
            }

            // Sun protection should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Sun protection typically caps at 100
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Strong bonus for application events
        if (applicationEvents >= 1)
            score += 0.2;

        // Strong bonus for gradual depletion pattern
        if (gradualDepletionPattern && depletionEvents >= 3)
            score += 0.2;

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
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double" or "int16" or "short";
    }

    private static bool IsInSunProtectionRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Sun protection typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}