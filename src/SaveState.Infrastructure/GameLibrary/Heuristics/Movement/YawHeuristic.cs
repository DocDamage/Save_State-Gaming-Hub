using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting yaw (horizontal rotation) angle values in game memory.
/// Yaw values typically:
/// - Are floats in range 0.0 to 360.0 or -180.0 to +180.0 degrees
/// - Change continuously with mouse/controller input
/// - Wrap around at 0/360 boundary
/// - Same as horizontal look/rotation
/// </summary>
public sealed class YawHeuristic : IValueHeuristic
{
    public string Name => "Yaw Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasFloatPrecision = false;
        bool wrapAroundDetected = false;

        // Check value range
        if (IsInYawRange(value.CurrentValue))
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

            // Check for wrap-around (0 to 360 or -180 to 180)
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta > 300 && delta < 400)
            {
                wrapAroundDetected = true;
                score += 0.25;
            }
        }

        // Bonus for float precision (angular values)
        if (hasFloatPrecision)
        {
            score += 0.15;
        }

        // Bonus for wrap-around detection (distinctive of rotation)
        if (wrapAroundDetected)
        {
            score += 0.2;
        }

        // Correlation with aim/camera actions
        int aimEvents = history.Count(h => h.RelatedAction == PlayerAction.AimChanged);
        if (aimEvents >= 2)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInYawRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Yaw can be 0-360 or -180 to 180
            return (val >= 0.0 && val <= 360.0) || (val >= -180.0 && val <= 180.0);
        }
        catch
        {
            return false;
        }
    }
}