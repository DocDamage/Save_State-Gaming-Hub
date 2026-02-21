using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting swim speed values in game memory.
/// Swim speed values typically:
/// - Are floats in range 0.0-30.0
/// - Non-zero only when in water
/// - Usually slower than walk/run speed
/// - Often has different values for surface vs underwater swimming
/// </summary>
public sealed class SwimSpeedHeuristic : IValueHeuristic
{
    public string Name => "Swim Speed Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int zeroWhenNotSwimming = 0;
        int nonZeroCount = 0;
        double prevVal = 0;

        // Check value range
        if (IsInSwimSpeedRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Analyze observation history
        for (int i = 0; i < history.Count; i++)
        {
            if (history[i].Value == null)
                continue;

            double? currVal = HeuristicUtilities.ConvertToDouble(history[i].Value);
            if (!currVal.HasValue)
                continue;

            var val = currVal.Value;

            // Track non-zero values
            if (val > 0.01)
                nonZeroCount++;

            // Swim speed is 0 when not swimming
            if (i > 0 && history[i].RelatedAction == null && val < 0.01)
            {
                zeroWhenNotSwimming++;
            }

            prevVal = val;

            // Swim speed should never be negative
            if (val < 0)
            {
                score -= 0.3;
            }
        }

        // Bonus for being zero when not swimming (intermittent pattern)
        if (zeroWhenNotSwimming >= 2)
        {
            score += 0.25;
        }

        // Bonus for having some swimming activity but not continuous
        if (nonZeroCount >= 1 && nonZeroCount < history.Count * 0.4)
        {
            score += 0.2;
        }

        // Correlation with position changes
        int movementEvents = history.Count(h => h.RelatedAction == PlayerAction.PositionChanged);
        if (movementEvents >= 2)
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

    private static bool IsInSwimSpeedRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 30.0;
        }
        catch
        {
            return false;
        }
    }
}