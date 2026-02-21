using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting weapon spread values in game memory.
/// Spread values typically:
/// - Are floats in range 0.0-90.0 (degrees)
/// - Dynamic - increase while firing, decrease when idle
/// - Higher for hipfire, lower for aiming down sights
/// - Affected by movement and stance
/// </summary>
public sealed class SpreadHeuristic : IValueHeuristic
{
    public string Name => "Weapon Spread Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int firingIncreases = 0;
        int idleDecreases = 0;

        // Check value range
        if (IsInSpreadRange(value.CurrentValue))
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

            var delta = currVal.Value - prevVal.Value;

            // Spread increases while firing
            if (curr.RelatedAction == PlayerAction.UsedAmmo && delta > 0)
            {
                firingIncreases++;
                score += 0.15;
            }

            // Spread decreases when not firing
            if (delta < 0 && Math.Abs(delta) < 5)
            {
                idleDecreases++;
                score += 0.05;
            }

            // Spread should stay within reasonable bounds
            if (currVal < 0 || currVal > 90)
            {
                score -= 0.3;
            }
        }

        // Strong bonus for firing pattern correlation
        if (firingIncreases >= 2)
        {
            score += 0.25;
        }

        // Bonus for recovery pattern
        if (idleDecreases >= 2)
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

    private static bool IsInSpreadRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Spread typically 0-90 degrees
            return val >= 0.0 && val <= 90.0;
        }
        catch
        {
            return false;
        }
    }
}