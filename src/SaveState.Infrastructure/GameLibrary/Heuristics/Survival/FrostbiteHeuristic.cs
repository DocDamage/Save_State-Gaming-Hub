using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting frostbite/cold injury values in survival games.
/// Frostbite values typically:
/// - Are floats or integers (0.0-100.0 severity or stages 0-4)
/// - Accumulate in extreme cold without protection
/// - Affect extremities (fingers, toes, ears, nose)
/// - Can cause permanent damage if severe
/// </summary>
public sealed class FrostbiteHeuristic : IValueHeuristic
{
    public string Name => "Frostbite Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int coldExposureEvents = 0;
        int warmingEvents = 0;
        bool gradualAccumulationPattern = false;

        // Check value range (frostbite typically 0-100 or 0-4 stages)
        if (IsInFrostbiteRange(value.CurrentValue))
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

            // Check for frostbite accumulation in cold (idle in cold)
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = currVal.Value - prevVal.Value;
                // Frostbite accumulates slowly in extreme cold
                if (delta > 0 && delta < 3)
                {
                    coldExposureEvents++;
                    gradualAccumulationPattern = true;
                    score += 0.12;
                }
            }

            // Check for rapid frostbite from extreme exposure
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Sprinted)
            {
                var delta = currVal.Value - prevVal.Value;
                // Exposed skin freezes faster when moving
                if (delta > 5 && delta < 30)
                {
                    coldExposureEvents++;
                    score += 0.15;
                }
            }

            // Check for warming/recovery
            if (currVal < prevVal && (curr.RelatedAction == PlayerAction.Healed || 
                                       curr.RelatedAction == PlayerAction.Idle))
            {
                var delta = prevVal.Value - currVal.Value;
                // Warming reduces frostbite
                if (delta > 5)
                {
                    warmingEvents++;
                    score += 0.18;
                }
            }

            // Check for stage-based system (0-4)
            if (HeuristicUtilities.IsIntegerValue(currVal.Value) && 
                currVal >= 0 && currVal <= 4)
            {
                score += 0.15;
            }

            // Frostbite should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Reasonable max values
            if (currVal > 100 && currVal != 4)
            {
                score -= 0.2;
            }
        }

        // Bonus for cold exposure events
        if (coldExposureEvents >= 2)
            score += 0.15;

        // Strong bonus for gradual accumulation (distinctive)
        if (gradualAccumulationPattern)
            score += 0.2;

        // Bonus for warming events
        if (warmingEvents >= 1)
            score += 0.12;

        // Check for max value (100 or 4 stages)
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        if (Math.Abs(maxValue - 100) < 5 || Math.Abs(maxValue - 4) < 0.5)
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

    private static bool IsInFrostbiteRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Frostbite: 0-100 severity or 0-4 stages
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}