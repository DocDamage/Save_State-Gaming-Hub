using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting mental health/psychological state values in survival games.
/// Mental health values typically:
/// - Are floats or integers (0.0-100.0)
/// - Decrease during isolation, stress, or traumatic events
/// - Recover through rest, social interaction, and positive activities
/// - Affects decision-making and sanity
/// </summary>
public sealed class MentalHealthHeuristic : IValueHeuristic
{
    public string Name => "Mental Health Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int stressEvents = 0;
        int recoveryEvents = 0;
        bool gradualDecayPattern = false;

        // Check value range (mental health typically 0-100)
        if (IsInMentalHealthRange(value.CurrentValue))
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

            // Check for stress/trauma (combat, danger)
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Attacked)
            {
                var delta = prevVal.Value - currVal.Value;
                if (delta > 5 && delta < 30)
                {
                    stressEvents++;
                    score += 0.18;
                }
            }

            // Check for gradual decay during isolation
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = prevVal.Value - currVal.Value;
                // Loneliness affects mental health slowly
                if (delta > 0 && delta < 2)
                {
                    gradualDecayPattern = true;
                    score += 0.1;
                }
            }

            // Check for recovery through positive activities
            if (currVal > prevVal && (curr.RelatedAction == PlayerAction.Healed || 
                                       curr.RelatedAction == PlayerAction.UsedItem))
            {
                var delta = currVal.Value - prevVal.Value;
                if (delta > 3 && delta < 25)
                {
                    recoveryEvents++;
                    score += 0.15;
                }
            }

            // Mental health should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Mental health typically caps at 100
            if (currVal > 100)
            {
                score -= 0.3;
            }

            // Check for typical mental health ranges (rarely at extremes)
            if (currVal >= 20 && currVal <= 90)
            {
                score += 0.05;
            }
        }

        // Bonus for stress events
        if (stressEvents >= 1)
            score += 0.15;

        // Strong bonus for gradual decay (distinctive)
        if (gradualDecayPattern)
            score += 0.2;

        // Bonus for recovery events
        if (recoveryEvents >= 1)
            score += 0.12;

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

    private static bool IsInMentalHealthRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Mental health typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}