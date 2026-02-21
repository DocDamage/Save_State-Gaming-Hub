using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting bearing (direction to target) values in game memory.
/// Bearing values typically:
/// - Are floats in range 0.0 to 360.0 degrees
/// - Represent direction to objective, waypoint, or target
/// - Change as player moves relative to target
/// - Similar to heading but toward a point, not facing direction
/// </summary>
public sealed class BearingHeuristic : IValueHeuristic
{
    public string Name => "Bearing Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasFloatPrecision = false;
        bool continuousChanges = false;
        bool allPositive = true;

        // Check value range - bearing is typically 0-360
        if (IsInBearingRange(value.CurrentValue))
        {
            score += 0.4;
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

            // Check for float precision
            if (currVal.Value != Math.Floor(currVal.Value))
            {
                hasFloatPrecision = true;
            }

            // Bearing should always be positive (0-360)
            if (currVal.Value < 0)
            {
                allPositive = false;
            }

            // Bearing changes as player moves (not only on input)
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta > 0.1 && delta < 180.0)
            {
                continuousChanges = true;
            }
        }

        // Bonus for float precision
        if (hasFloatPrecision)
        {
            score += 0.15;
        }

        // Bearing should always be positive (0-360 range)
        if (allPositive)
        {
            score += 0.15;
        }

        // Bonus for continuous changes (not just on input)
        if (continuousChanges)
        {
            score += 0.2;
        }

        // Correlation with position changes
        int positionEvents = history.Count(h => h.RelatedAction == PlayerAction.PositionChanged);
        if (positionEvents >= 2)
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

    private static bool IsInBearingRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 360.0;
        }
        catch
        {
            return false;
        }
    }
}