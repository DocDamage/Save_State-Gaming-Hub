using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting multi-kill streaks in shooter games.
/// Multi-kill values typically:
/// - Are positive integers (2, 3, 4, 5+)
/// - Trigger on rapid consecutive kills
/// - Reset quickly if no kills continue
/// </summary>
public sealed class MultiKillHeuristic : IValueHeuristic
{
    public string Name => "Multi-Kill Streak Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int streakEvents = 0;
        int resetEvents = 0;

        // Check value range (multi-kills typically 0-10)
        if (IsInMultiKillRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Must be integer type
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.3;
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

            // Check for streak increase (during combat)
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Attacked)
            {
                streakEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Multi-kill increments by 1
                if (delta == 1)
                {
                    score += 0.2;
                }
            }

            // Check for rapid reset (multi-kills reset quickly)
            if (currVal == 0 && prevVal > 1)
            {
                resetEvents++;
                score += 0.15;
            }

            // Values should be small (2-5 typical for multi-kill)
            if (currVal > 10)
            {
                score -= 0.3;
            }

            // Should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for streak events
        if (streakEvents >= 1)
            score += 0.15;

        // Bonus for reset pattern
        if (resetEvents >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInMultiKillRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 20;
        }
        catch
        {
            return false;
        }
    }
}