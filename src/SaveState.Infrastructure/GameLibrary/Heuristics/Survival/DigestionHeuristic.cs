using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting digestion/stomach content values in survival games.
/// Digestion values typically:
/// - Are floats or integers (0.0-100.0 fullness)
/// - Increase immediately after eating
/// - Decrease gradually as food is processed
/// - Affects energy gain rate and hunger
/// </summary>
public sealed class DigestionHeuristic : IValueHeuristic
{
    public string Name => "Digestion Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int eatingEvents = 0;
        int digestionEvents = 0;
        bool gradualDigestionPattern = false;

        // Check value range (digestion typically 0-100)
        if (IsInDigestionRange(value.CurrentValue))
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

            // Check for eating (sudden increase)
            if (currVal > prevVal && (curr.RelatedAction == PlayerAction.UsedItem || 
                                       curr.RelatedAction == PlayerAction.Healed))
            {
                var delta = currVal.Value - prevVal.Value;
                // Eating fills stomach immediately
                if (delta > 10 && delta < 70)
                {
                    eatingEvents++;
                    score += 0.2;
                }
            }

            // Check for digestion (gradual decrease)
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = prevVal.Value - currVal.Value;
                // Digestion is slow and steady
                if (delta > 0 && delta < 3)
                {
                    digestionEvents++;
                    gradualDigestionPattern = true;
                    score += 0.1;
                }
            }

            // Check for digestion continuing during activity
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Moved)
            {
                var delta = prevVal.Value - currVal.Value;
                // Activity may speed up digestion slightly
                if (delta > 0 && delta < 5)
                {
                    digestionEvents++;
                    score += 0.08;
                }
            }

            // Digestion should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Digestion typically caps at 100
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Strong bonus for eating events
        if (eatingEvents >= 1)
            score += 0.2;

        // Strong bonus for gradual digestion pattern (distinctive)
        if (gradualDigestionPattern && digestionEvents >= 3)
            score += 0.25;

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

    private static bool IsInDigestionRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Digestion typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}