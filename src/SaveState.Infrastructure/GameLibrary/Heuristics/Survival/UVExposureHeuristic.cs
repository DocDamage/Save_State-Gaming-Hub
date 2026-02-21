using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting UV exposure/sunburn values in survival games.
/// UV exposure values typically:
/// - Are floats or integers (0.0-100.0 or 0-11 UV index)
/// - Increase when exposed to direct sunlight
/// - Decrease in shade, indoors, or at night
/// - Cause sunburn, heat exhaustion, or skin damage at high levels
/// </summary>
public sealed class UVExposureHeuristic : IValueHeuristic
{
    public string Name => "UV Exposure Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int sunExposureEvents = 0;
        int shadeRecoveryEvents = 0;
        bool dayNightCyclePattern = false;

        // Check value range (UV typically 0-100 or 0-11)
        if (IsInUVRange(value.CurrentValue))
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

            // Check for UV increase when exposed (moving outside)
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Moved)
            {
                var delta = currVal.Value - prevVal.Value;
                if (delta > 5)
                {
                    sunExposureEvents++;
                    score += 0.15;
                }
            }

            // Check for UV decrease in shade/indoors (idle)
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = prevVal.Value - currVal.Value;
                // Gradual decrease when protected
                if (delta > 0 && delta < 10)
                {
                    shadeRecoveryEvents++;
                    score += 0.08;
                }
            }

            // Check for day/night cycle (values dropping to 0)
            if (currVal == 0 && prevVal > 0)
            {
                dayNightCyclePattern = true;
                score += 0.1;
            }

            // UV should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // UV index typically caps at 11 or 100
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Bonus for sun exposure events
        if (sunExposureEvents >= 1)
            score += 0.15;

        // Bonus for shade recovery
        if (shadeRecoveryEvents >= 2)
            score += 0.12;

        // Strong bonus for day/night cycle pattern
        if (dayNightCyclePattern)
            score += 0.2;

        // Check for common max values
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        // Common UV scales: 0-11 (index) or 0-100 (percentage)
        if (Math.Abs(maxValue - 11) < 1 || Math.Abs(maxValue - 100) < 5)
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

    private static bool IsInUVRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // UV typically in range 0-100 or 0-11
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}