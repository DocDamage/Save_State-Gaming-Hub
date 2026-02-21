using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting toxicity/poison buildup values in survival games.
/// Toxicity values typically:
/// - Are floats or integers (0.0-100.0 or 0-1000)
/// - Accumulate from venom, toxins, or poisonous food
/// - Cause damage over time when high
/// - Reduced with antidotes or through natural filtration
/// </summary>
public sealed class ToxicityHeuristic : IValueHeuristic
{
    public string Name => "Toxicity Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int exposureEvents = 0;
        int detoxEvents = 0;
        bool gradualAccumulationPattern = false;

        // Check value range (toxicity: 0-100 or 0-1000)
        if (IsInToxicityRange(value.CurrentValue))
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

            // Check for sudden toxicity (bites, poisoned items)
            if (currVal > prevVal && (curr.RelatedAction == PlayerAction.Attacked || 
                                       curr.RelatedAction == PlayerAction.UsedItem))
            {
                var delta = currVal.Value - prevVal.Value;
                // Poison exposure is sudden
                if (delta > 10 && delta < 60)
                {
                    exposureEvents++;
                    score += 0.2;
                }
            }

            // Check for gradual buildup
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = currVal.Value - prevVal.Value;
                // Toxins spread in body gradually
                if (delta > 0 && delta < 5)
                {
                    gradualAccumulationPattern = true;
                    score += 0.1;
                }
            }

            // Check for antidote use
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.UsedItem)
            {
                var delta = prevVal.Value - currVal.Value;
                // Antidotes reduce toxicity rapidly
                if (delta > 20 && delta < 90)
                {
                    detoxEvents++;
                    score += 0.22;
                }
            }

            // Check for natural filtration
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = prevVal.Value - currVal.Value;
                // Body processes toxins slowly
                if (delta > 0 && delta < 3)
                {
                    detoxEvents++;
                    score += 0.08;
                }
            }

            // Toxicity should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Toxicity typically caps at 100 or 1000
            if (currVal > 1000)
            {
                score -= 0.3;
            }
        }

        // Strong bonus for exposure events
        if (exposureEvents >= 1)
            score += 0.2;

        // Bonus for gradual accumulation
        if (gradualAccumulationPattern)
            score += 0.15;

        // Strong bonus for detox events
        if (detoxEvents >= 1)
            score += 0.18;

        // Check for max value
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        // Common caps: 100 (%) or 1000 (toxicity units)
        if (Math.Abs(maxValue - 100) < 5 || Math.Abs(maxValue - 1000) < 50)
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

    private static bool IsInToxicityRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Toxicity: 0-100 (%) or 0-1000 (units)
            var val = doubleValue.Value;
            return val >= 0 && val <= 1000;
        }
        catch
        {
            return false;
        }
    }
}