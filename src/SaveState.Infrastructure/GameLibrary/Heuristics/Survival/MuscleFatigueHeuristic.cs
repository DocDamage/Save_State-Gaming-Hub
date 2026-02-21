using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting muscle fatigue/strain values in survival games.
/// Muscle fatigue values typically:
/// - Are floats or integers (0.0-100.0)
/// - Build up during repetitive or strenuous activities
/// - Recover with rest and stretching
/// - Affect strength and movement efficiency
/// </summary>
public sealed class MuscleFatigueHeuristic : IValueHeuristic
{
    public string Name => "Muscle Fatigue Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int exertionEvents = 0;
        int recoveryEvents = 0;
        bool activityCorrelation = false;

        // Check value range (muscle fatigue typically 0-100)
        if (IsInMuscleFatigueRange(value.CurrentValue))
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

            // Check for fatigue buildup during strenuous activity
            if (currVal > prevVal && (curr.RelatedAction == PlayerAction.Sprinted || 
                                       curr.RelatedAction == PlayerAction.Attacked))
            {
                var delta = currVal.Value - prevVal.Value;
                if (delta > 2 && delta < 25)
                {
                    exertionEvents++;
                    score += 0.15;
                }
            }

            // Check for gradual buildup during normal activity
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Moved)
            {
                var delta = currVal.Value - prevVal.Value;
                if (delta > 0 && delta < 10)
                {
                    exertionEvents++;
                    score += 0.08;
                }
            }

            // Check for recovery during rest
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = prevVal.Value - currVal.Value;
                // Muscle recovery is gradual
                if (delta > 1 && delta < 15)
                {
                    recoveryEvents++;
                    score += 0.12;
                }
            }

            // Muscle fatigue should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Muscle fatigue typically caps at 100
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Bonus for exertion events
        if (exertionEvents >= 2)
        {
            score += 0.18;
            activityCorrelation = true;
        }

        // Bonus for recovery events
        if (recoveryEvents >= 2)
            score += 0.15;

        // Bonus for activity correlation
        if (activityCorrelation)
            score += 0.1;

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

    private static bool IsInMuscleFatigueRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Muscle fatigue typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}