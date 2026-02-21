using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting radiation/exposure levels in post-apocalyptic survival games.
/// Radiation values typically:
/// - Are floats (0.0-100.0) or integers (0-1000)
/// - Increase when in contaminated areas
/// - Decrease with medication or over time
/// - Cause damage at high levels
/// </summary>
public sealed class RadiationHeuristic : IValueHeuristic
{
    public string Name => "Radiation/Exposure Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int exposureEvents = 0;
        int reductionEvents = 0;

        // Check value range (radiation typically 0-1000)
        if (IsInRadiationRange(value.CurrentValue))
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

            // Check for radiation increase (exposure)
            if (currVal > prevVal)
            {
                exposureEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Exposure increases gradually
                if (delta > 0 && delta < 50)
                {
                    score += 0.1;
                }
            }

            // Check for radiation decrease (medication/time)
            if (currVal < prevVal)
            {
                reductionEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Reduction from medication is faster
                if (delta > 10 && delta < 200)
                {
                    score += 0.12;
                }
                // Natural decay is slower
                else if (delta > 0 && delta <= 10)
                {
                    score += 0.08;
                }
            }

            // Radiation should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Very high radiation is dangerous but possible
            if (currVal > 10000)
            {
                score -= 0.3;
            }
        }

        // Bonus for exposure pattern
        if (exposureEvents >= 2)
            score += 0.15;

        // Bonus for reduction pattern
        if (reductionEvents >= 1)
            score += 0.1;

        // Check for common max values (100 for percentage, 1000 for rads)
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        if (Math.Abs(maxValue - 100) < 5 || Math.Abs(maxValue - 1000) < 50)
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

    private static bool IsInRadiationRange(object? value)
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