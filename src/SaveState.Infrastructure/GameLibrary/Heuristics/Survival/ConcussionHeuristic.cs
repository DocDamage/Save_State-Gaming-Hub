using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting concussion/head trauma values in survival games.
/// Concussion values typically:
/// - Are floats or integers (0.0-100.0 severity or 0-3 stages)
/// - Occur from head injuries or explosions
/// - Cause vision blur, dizziness, and reduced coordination
/// - Heal slowly with rest, worsen with activity
/// </summary>
public sealed class ConcussionHeuristic : IValueHeuristic
{
    public string Name => "Concussion Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int traumaEvents = 0;
        int recoveryEvents = 0;
        bool activityWorsening = false;

        // Check value range (concussion typically 0-100 or 0-3 stages)
        if (IsInConcussionRange(value.CurrentValue))
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

            // Check for trauma event (sudden increase)
            if (currVal > prevVal && (curr.RelatedAction == PlayerAction.Attacked || 
                                       curr.RelatedAction == PlayerAction.Sprinted))
            {
                var delta = currVal.Value - prevVal.Value;
                // Concussions occur from impacts
                if (delta > 20 || (prevVal == 0 && currVal > 0))
                {
                    traumaEvents++;
                    score += 0.2;
                }
            }

            // Check for recovery during rest
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = prevVal.Value - currVal.Value;
                // Recovery is slow with rest
                if (delta > 0 && delta < 3)
                {
                    recoveryEvents++;
                    score += 0.12;
                }
            }

            // Check for worsening with activity
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Sprinted)
            {
                var delta = currVal.Value - prevVal.Value;
                // Activity worsens concussion
                if (delta > 0 && delta < 5)
                {
                    activityWorsening = true;
                    score += 0.15;
                }
            }

            // Concussion should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Check for typical max values
            if (currVal > 100 && currVal != 3)
            {
                score -= 0.2;
            }
        }

        // Strong bonus for trauma events
        if (traumaEvents >= 1)
            score += 0.2;

        // Bonus for recovery during rest
        if (recoveryEvents >= 2)
            score += 0.15;

        // Bonus for activity worsening (distinctive)
        if (activityWorsening)
            score += 0.15;

        // Check for max value (100 or 3 stages)
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        if (Math.Abs(maxValue - 100) < 5 || Math.Abs(maxValue - 3) < 0.5)
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

    private static bool IsInConcussionRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Concussion typically in range 0-100 or 0-3
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}