using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting fatigue/exhaustion in survival games.
/// Fatigue values typically:
/// - Are floats (0.0-100.0) or integers (0-100)
/// - Increase with activity and lack of sleep
/// - Decrease with rest/sleep
/// - Affect performance when high
/// </summary>
public sealed class FatigueHeuristic : IValueHeuristic
{
    public string Name => "Fatigue/Exhaustion Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increaseEvents = 0;
        int decreaseEvents = 0;

        // Check value range (fatigue typically 0-100)
        if (IsInFatigueRange(value.CurrentValue))
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

            // Check for increase (activity)
            if (currVal > prevVal)
            {
                increaseEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Fatigue builds gradually
                if (delta > 0 && delta < 5)
                {
                    score += 0.1;
                }
            }

            // Check for decrease (rest/sleep)
            if (currVal < prevVal)
            {
                decreaseEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Sleep reduces fatigue significantly
                if (delta > 10)
                {
                    score += 0.15;
                }
                // Rest reduces gradually
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
        if (increaseEvents >= 2)
            score += 0.15;
        if (decreaseEvents >= 1)
            score += 0.1;

        // Check for max of 100
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
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
        return normalizedType is "float" or "single" or "int32" or "int" or "double";
    }

    private static bool IsInFatigueRange(object? value)
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