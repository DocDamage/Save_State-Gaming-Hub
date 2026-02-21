using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting rotation/yaw angle in 3D games.
/// Rotation values typically:
/// - Are floats (0-360 degrees or -180 to +180)
/// - Change continuously with mouse/controller input
/// - Wrap around at 0/360 boundary
/// </summary>
public sealed class RotationHeuristic : IValueHeuristic
{
    public string Name => "Rotation/Yaw Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasFloatPrecision = false;
        bool wrapAroundDetected = false;

        // Check for float type
        if (IsFloatType(value.ValueType))
        {
            score += 0.2;
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

            // Check for reasonable range
            if ((currVal >= 0 && currVal <= 360) || (currVal >= -180 && currVal <= 180))
            {
                score += 0.1;
            }
            else if (currVal < -360 || currVal > 720)
            {
                score -= 0.3;
            }
        }

        // Bonus for float precision
        if (hasFloatPrecision)
            score += 0.15;

        // Bonus for wrap-around detection (distinctive of rotation)
        if (wrapAroundDetected)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsFloatType(string valueType)
    {
        var normalized = valueType.ToLowerInvariant();
        return normalized is "float" or "single" or "double";
    }
}