using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting visibility/fog values in survival games.
/// Visibility values typically:
/// - Are floats or integers (0.0-100.0 representing percentage or meters)
/// - Decrease during weather events (fog, rain, snow)
/// - Decrease at night or in dark areas
/// - Affect detection range and stealth capabilities
/// </summary>
public sealed class VisibilityHeuristic : IValueHeuristic
{
    public string Name => "Visibility Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int weatherEvents = 0;
        int nightCycleEvents = 0;
        bool inverseToTimePattern = false;

        // Check value range (visibility typically 0-100 or 0-1000 meters)
        if (IsInVisibilityRange(value.CurrentValue))
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

            // Check for visibility decrease (weather entering)
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = prevVal.Value - currVal.Value;
                // Weather can cause sudden visibility drops
                if (delta > 10 && delta < 80)
                {
                    weatherEvents++;
                    score += 0.12;
                }
            }

            // Check for visibility recovery (weather clearing)
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = currVal.Value - prevVal.Value;
                if (delta > 10)
                {
                    score += 0.08;
                }
            }

            // Check for day/night pattern (visibility dropping at "night")
            if (currVal < 30 && prevVal > 50)
            {
                nightCycleEvents++;
                score += 0.1;
            }

            // Visibility should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Reasonable max visibility
            if (currVal > 1000)
            {
                score -= 0.2;
            }
        }

        // Bonus for weather events
        if (weatherEvents >= 1)
            score += 0.15;

        // Bonus for night cycle pattern
        if (nightCycleEvents >= 1)
            score += 0.15;

        // Check for typical visibility ranges
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        // Common visibility caps: 100 (percentage) or 1000 (meters)
        if (Math.Abs(maxValue - 100) < 5 || Math.Abs(maxValue - 1000) < 50)
        {
            score += 0.2;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double" or "int16" or "short";
    }

    private static bool IsInVisibilityRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Visibility typically in range 0-1000 (meters or percentage)
            var val = doubleValue.Value;
            return val >= 0 && val <= 1000;
        }
        catch
        {
            return false;
        }
    }
}