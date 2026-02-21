using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting breath hold/apnea values in survival games.
/// Breath hold values typically:
/// - Are floats or integers (0.0-100.0 or 0-60 seconds)
/// - Decrease while underwater or in smoke
/// - Recover rapidly when breathing resumes
/// - Used for diving and escaping hazards
/// </summary>
public sealed class BreathHoldHeuristic : IValueHeuristic
{
    public string Name => "Breath Hold Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int underwaterEvents = 0;
        int recoveryEvents = 0;
        bool rapidRecoveryPattern = false;

        // Check value range (breath hold: 0-100 or 0-60 seconds)
        if (IsInBreathHoldRange(value.CurrentValue))
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

            // Check for breath depletion during activity
            if (currVal < prevVal && (curr.RelatedAction == PlayerAction.Sprinted || 
                                       curr.RelatedAction == PlayerAction.Moved))
            {
                var delta = prevVal.Value - currVal.Value;
                // Breath decreases while submerged
                if (delta > 1 && delta < 20)
                {
                    underwaterEvents++;
                    score += 0.15;
                }
            }

            // Check for rapid recovery when surfacing
            if (currVal > prevVal)
            {
                var delta = currVal.Value - prevVal.Value;
                // Breath recovers very quickly
                if (delta > 20 && delta < 100)
                {
                    recoveryEvents++;
                    rapidRecoveryPattern = true;
                    score += 0.2;
                }
            }

            // Check for critical low breath (emergency state)
            if (currVal < 20 && currVal >= 0)
            {
                score += 0.05;
            }

            // Breath hold should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Breath hold typically caps at 100 or 60
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Bonus for underwater events
        if (underwaterEvents >= 2)
            score += 0.15;

        // Strong bonus for rapid recovery (distinctive)
        if (rapidRecoveryPattern)
            score += 0.2;

        // Bonus for recovery events
        if (recoveryEvents >= 1)
            score += 0.1;

        // Check for max value
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        // Common caps: 100 (%) or 60 (seconds)
        if (Math.Abs(maxValue - 100) < 5 || Math.Abs(maxValue - 60) < 5)
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

    private static bool IsInBreathHoldRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Breath hold: 0-100 (%) or 0-60 (seconds)
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}