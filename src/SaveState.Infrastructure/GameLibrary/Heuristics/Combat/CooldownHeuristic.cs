using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting ability/skill cooldown values in game memory.
/// Cooldown values typically:
/// - Are floats in range 0.0-300.0 seconds
/// - Count down from max to 0
/// - Jump back up when ability is used
/// </summary>
public sealed class CooldownHeuristic : IValueHeuristic
{
    public string Name => "Cooldown Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int countdownEvents = 0;
        int resetEvents = 0;
        double? maxValue = null;

        // Check value range
        if (IsInCooldownRange(value.CurrentValue))
        {
            score += 0.25;
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

            var delta = currVal.Value - prevVal.Value;

            // Check for countdown pattern (decreasing)
            if (delta < 0 && delta > -5 && currVal.Value >= 0)
            {
                countdownEvents++;
                // Track max value
                if (!maxValue.HasValue || prevVal.Value > maxValue.Value)
                    maxValue = prevVal.Value;
            }

            // Check for reset pattern (jump back up)
            if (delta > 1)
            {
                resetEvents++;
            }

            // Cooldown should never be negative
            if (currVal.Value < 0)
            {
                score -= 0.3;
            }
        }

        // Bonus for countdown pattern
        if (countdownEvents >= 3)
        {
            score += 0.3;
        }

        // Bonus for reset events
        if (resetEvents >= 1)
        {
            score += 0.2;
        }

        // Bonus for both countdown and reset
        if (countdownEvents >= 2 && resetEvents >= 1)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInCooldownRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 300.0;
        }
        catch
        {
            return false;
        }
    }
}