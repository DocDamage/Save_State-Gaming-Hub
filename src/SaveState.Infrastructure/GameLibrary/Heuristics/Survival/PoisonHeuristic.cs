using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting poison/disease status in survival games.
/// Poison values typically:
/// - Are floats (0.0-100.0) representing severity
/// - Increase when poisoned
/// - Decrease over time or with antidote
/// </summary>
public sealed class PoisonHeuristic : IValueHeuristic
{
    public string Name => "Poison/Disease Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increaseEvents = 0;
        int decreaseEvents = 0;

        // Check value range (poison typically 0-100)
        if (IsInPoisonRange(value.CurrentValue))
        {
            score += 0.35;
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

            // Check for increase (getting poisoned)
            if (currVal > prevVal)
            {
                increaseEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Poison increases rapidly when applied
                if (delta > 10 && delta <= 50)
                {
                    score += 0.15;
                }
            }

            // Check for decrease (curing)
            if (currVal < prevVal)
            {
                decreaseEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Cure reduces significantly
                if (delta > 10)
                {
                    score += 0.15;
                }
                // Natural decay is slower
                else if (delta > 0)
                {
                    score += 0.08;
                }
            }

            // Should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Typically caps at 100
            if (currVal > 200)
            {
                score -= 0.3;
            }
        }

        // Bonus for patterns
        if (increaseEvents >= 1)
            score += 0.1;
        if (decreaseEvents >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double";
    }

    private static bool IsInPoisonRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 200;
        }
        catch
        {
            return false;
        }
    }
}