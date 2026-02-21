using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting strafe (sideways movement) speed values in game memory.
/// Strafe speed values typically:
/// - Are floats in range -30.0 to +30.0
/// - Can be negative (left) or positive (right)
/// - Zero when not strafing
/// - Usually slower than forward movement
/// </summary>
public sealed class StrafeSpeedHeuristic : IValueHeuristic
{
    public string Name => "Strafe Speed Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasNegative = false;
        bool hasPositive = false;
        int zeroWhenNotStrafing = 0;

        // Check value range
        if (IsInStrafeSpeedRange(value.CurrentValue))
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

            // Track positive/negative (right/left)
            if (val > 0.01) hasPositive = true;
            if (val < -0.01) hasNegative = true;

            // Strafe speed is 0 when not strafing
            if (i > 0 && history[i].RelatedAction == null && Math.Abs(val) < 0.01)
            {
                zeroWhenNotStrafing++;
            }
        }

        // Bonus for having both directions
        if (hasNegative && hasPositive)
        {
            score += 0.3;
        }

        // Bonus for being zero when not strafing
        if (zeroWhenNotStrafing >= 2)
        {
            score += 0.2;
        }

        // Correlation with position changes
        int positionEvents = history.Count(h => h.RelatedAction == PlayerAction.PositionChanged);
        if (positionEvents >= 2)
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

    private static bool IsInStrafeSpeedRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -30.0 && val <= 30.0;
        }
        catch
        {
            return false;
        }
    }
}