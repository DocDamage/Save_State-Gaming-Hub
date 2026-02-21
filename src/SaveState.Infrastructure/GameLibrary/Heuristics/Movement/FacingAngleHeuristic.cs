using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting facing angle values in game memory.
/// Facing angle values typically:
/// - Are floats in range 0.0 to 360.0 or -180.0 to +180.0 degrees
/// - Represent the direction character is facing
/// - Used in 2D games or top-down view games
/// - Change with rotation input
/// </summary>
public sealed class FacingAngleHeuristic : IValueHeuristic
{
    public string Name => "Facing Angle Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasFloatPrecision = false;
        bool wrapAroundDetected = false;
        int discreteDirections = 0;

        // Check value range
        if (IsInFacingAngleRange(value.CurrentValue))
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
                .Select(v => Math.Round(v!.Value / 45.0) * 45.0) // Round to 45-degree increments
                .Distinct()
                .ToList();

            discreteDirections = values.Count;
        }

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

            // Check for wrap-around
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

        // Bonus for wrap-around detection
        if (wrapAroundDetected)
        {
            score += 0.2;
        }

        // In 2D games, facing angle often snaps to 45-degree increments
        if (discreteDirections >= 4 && discreteDirections <= 8)
        {
            score += 0.15;
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

    private static bool IsInFacingAngleRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Can be 0-360 or -180 to 180
            return (val >= 0.0 && val <= 360.0) || (val >= -180.0 && val <= 180.0);
        }
        catch
        {
            return false;
        }
    }
}