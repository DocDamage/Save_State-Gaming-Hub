using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting hygiene/cleanliness values in survival games.
/// Hygiene values typically:
/// - Are floats or integers (0.0-100.0)
/// - Decrease gradually over time and activities
/// - Increase when washing or cleaning
/// - Affects disease risk and social interactions
/// </summary>
public sealed class HygieneHeuristic : IValueHeuristic
{
    public string Name => "Hygiene Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int cleaningEvents = 0;
        int activityDecay = 0;
        bool gradualDecayPattern = false;

        // Check value range (hygiene typically 0-100)
        if (IsInHygieneRange(value.CurrentValue))
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

            // Check for hygiene improvement from cleaning
            if (currVal > prevVal && (curr.RelatedAction == PlayerAction.UsedItem || 
                                       curr.RelatedAction == PlayerAction.Healed))
            {
                var delta = currVal.Value - prevVal.Value;
                // Cleaning provides significant hygiene boost
                if (delta > 15 && delta < 80)
                {
                    cleaningEvents++;
                    score += 0.18;
                }
            }

            // Check for hygiene decay during activities
            if (currVal < prevVal)
            {
                var delta = prevVal.Value - currVal.Value;
                // Gradual decay from normal activities
                if (delta > 0 && delta < 3)
                {
                    activityDecay++;
                    gradualDecayPattern = true;
                    score += 0.08;
                }
                // Larger decay from strenuous activities
                else if (delta >= 3 && delta < 15 && 
                        (curr.RelatedAction == PlayerAction.Sprinted || 
                         curr.RelatedAction == PlayerAction.Attacked))
                {
                    activityDecay++;
                    score += 0.1;
                }
            }

            // Hygiene should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Hygiene typically caps at 100
            if (currVal > 100)
            {
                score -= 0.3;
            }

            // Check for typical hygiene ranges
            if (currVal >= 10 && currVal <= 100)
            {
                score += 0.05;
            }
        }

        // Strong bonus for cleaning events
        if (cleaningEvents >= 1)
            score += 0.2;

        // Strong bonus for gradual decay pattern (distinctive)
        if (gradualDecayPattern && activityDecay >= 3)
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

    private static bool IsInHygieneRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Hygiene typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}