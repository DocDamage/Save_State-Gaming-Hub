using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting roll (bank/tilt) angle values in game memory.
/// Roll values typically:
/// - Are floats in range -180.0 to +180.0 degrees
/// - Used in aircraft, spaceship, or advanced movement games
/// - Changes during turns or when leaning
/// - Less common than pitch/yaw
/// </summary>
public sealed class RollHeuristic : IValueHeuristic
{
    public string Name => "Roll Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasFloatPrecision = false;
        int positiveCount = 0;
        int negativeCount = 0;
        bool nearZeroCommon = false;

        // Check value range - roll is typically -180 to +180
        if (IsInRollRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Analyze observation history
        if (history.Count >= 3)
        {
            var values = history
                .Where(h => h.Value != null)
                .Select(h => HeuristicUtilities.ConvertToDouble(h.Value))
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            if (values.Count >= 3)
            {
                // Roll often returns to near-zero when not turning
                var nearZeroCount = values.Count(v => Math.Abs(v) < 5.0);
                if (nearZeroCount > values.Count * 0.4)
                {
                    nearZeroCommon = true;
                }
            }
        }

        for (int i = 1; i < history.Count; i++)
        {
            var curr = history[i];

            if (curr.Value == null)
                continue;

            double? currVal = HeuristicUtilities.ConvertToDouble(curr.Value);
            if (!currVal.HasValue)
                continue;

            var val = currVal.Value;

            // Check for float precision
            if (val != Math.Floor(val))
            {
                hasFloatPrecision = true;
            }

            // Track positive/negative (banking left/right)
            if (val > 10.0) positiveCount++;
            if (val < -10.0) negativeCount++;
        }

        // Bonus for float precision (angular values)
        if (hasFloatPrecision)
        {
            score += 0.15;
        }

        // Bonus for near-zero being common
        if (nearZeroCommon)
        {
            score += 0.2;
        }

        // Roll can be both positive and negative (banking either direction)
        if (positiveCount > 0 && negativeCount > 0)
        {
            score += 0.2;
        }

        // Correlation with position changes (turning)
        int movementEvents = history.Count(h => h.RelatedAction == PlayerAction.PositionChanged);
        if (movementEvents >= 2)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInRollRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -180.0 && val <= 180.0;
        }
        catch
        {
            return false;
        }
    }
}