using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting hunger values in survival games.
/// Hunger values typically:
/// - Are floats (0.0-100.0) or integers (0-100)
/// - Decrease slowly over time (simulating metabolism)
/// - Can be restored by eating food items
/// - Low values may trigger negative effects
/// </summary>
public sealed class HungerHeuristic : IValueHeuristic
{
    public string Name => "Hunger Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int eatingEvents = 0;
        int gradualDecreaseCount = 0;
        bool hasDecreaseOverTimePattern = false;

        // Check value range (hunger typically 0-100 or 0-1000)
        if (IsInHungerRange(value.CurrentValue))
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

            // Check for eating (significant increase)
            if (currVal > prevVal && (curr.RelatedAction == PlayerAction.UsedItem || 
                                       curr.RelatedAction == PlayerAction.Healed))
            {
                var delta = currVal.Value - prevVal.Value;
                // Eating typically restores significant amount
                if (delta > 5 && delta < 80)
                {
                    eatingEvents++;
                    score += 0.15;
                }
            }

            // Check for gradual decrease (metabolism)
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = prevVal.Value - currVal.Value;
                // Gradual decrease over time
                if (delta > 0 && delta < 2)
                {
                    gradualDecreaseCount++;
                    hasDecreaseOverTimePattern = true;
                }
            }

            // Hunger values should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Hunger values typically cap at 100 or 1000
            if (currVal > 2000)
            {
                score -= 0.3;
            }
        }

        // Bonus for eating pattern
        if (eatingEvents >= 1)
            score += 0.2;

        // Bonus for gradual decrease pattern (distinctive survival mechanic)
        if (hasDecreaseOverTimePattern && gradualDecreaseCount >= 3)
            score += 0.25;

        // Check for common max value (100 or 1000)
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

    private static bool IsInHungerRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Hunger typically in range 0-1000
            var val = doubleValue.Value;
            return val >= 0 && val <= 1000;
        }
        catch
        {
            return false;
        }
    }
}