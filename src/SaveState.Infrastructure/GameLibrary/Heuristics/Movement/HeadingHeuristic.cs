using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting heading direction values in game memory.
/// Heading values typically:
/// - Are floats in range 0.0 to 360.0 degrees (compass direction)
/// - Represent absolute facing direction (0 = North, 90 = East, etc.)
/// - Change smoothly during rotation
/// - Wrap around at 0/360 boundary
/// </summary>
public sealed class HeadingHeuristic : IValueHeuristic
{
    public string Name => "Heading Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasFloatPrecision = false;
        bool wrapAroundDetected = false;
        bool allPositive = true;

        // Check value range - heading is typically 0-360
        if (IsInHeadingRange(value.CurrentValue))
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

            // Heading should always be positive (0-360)
            if (currVal.Value < 0)
            {
                allPositive = false;
            }

            // Check for wrap-around at 0/360
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta > 300 && delta < 400)
            {
                wrapAroundDetected = true;
            }
        }

        // Bonus for float precision
        if (hasFloatPrecision)
        {
            score += 0.15;
        }

        // Heading should always be positive (0-360 range)
        if (allPositive)
        {
            score += 0.15;
        }

        // Bonus for wrap-around detection
        if (wrapAroundDetected)
        {
            score += 0.2;
        }

        // Correlation with rotation/movement
        int rotationEvents = history.Count(h => h.RelatedAction == PlayerAction.Rotated);
        if (rotationEvents >= 2)
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

    private static bool IsInHeadingRange(object? value)
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