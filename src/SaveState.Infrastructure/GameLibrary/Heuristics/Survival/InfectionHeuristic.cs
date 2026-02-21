using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting infection/disease values in survival games.
/// Infection values typically:
/// - Are floats or integers (0.0-100.0)
/// - Increase when exposed to pathogens or contaminated sources
/// - Decrease with medication, treatment, or over time with immunity
/// - High infection causes negative status effects and health loss
/// </summary>
public sealed class InfectionHeuristic : IValueHeuristic
{
    public string Name => "Infection Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int exposureEvents = 0;
        int treatmentEvents = 0;
        bool gradualProgressionPattern = false;

        // Check value range (infection typically 0-100)
        if (IsInInfectionRange(value.CurrentValue))
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

            // Check for infection increase (exposure)
            if (currVal > prevVal)
            {
                var delta = currVal.Value - prevVal.Value;
                // Infection typically increases gradually over time
                if (delta > 0 && delta < 5)
                {
                    exposureEvents++;
                    score += 0.1;
                }
                // Sudden jump might be initial exposure
                else if (delta >= 5 && delta < 30)
                {
                    exposureEvents++;
                    score += 0.08;
                }
            }

            // Check for treatment (medication/healing)
            if (currVal < prevVal && (curr.RelatedAction == PlayerAction.Healed || 
                                       curr.RelatedAction == PlayerAction.UsedItem))
            {
                var delta = prevVal.Value - currVal.Value;
                // Treatment reduces infection significantly
                if (delta > 5)
                {
                    treatmentEvents++;
                    score += 0.18;
                }
            }

            // Check for gradual progression while idle (disease worsening)
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = currVal.Value - prevVal.Value;
                if (delta > 0 && delta < 2)
                {
                    gradualProgressionPattern = true;
                }
            }

            // Infection should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Infection typically caps at 100
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Bonus for exposure pattern
        if (exposureEvents >= 2)
            score += 0.15;

        // Strong bonus for treatment events
        if (treatmentEvents >= 1)
            score += 0.2;

        // Bonus for gradual progression (distinctive disease pattern)
        if (gradualProgressionPattern)
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

    private static bool IsInInfectionRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Infection typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}