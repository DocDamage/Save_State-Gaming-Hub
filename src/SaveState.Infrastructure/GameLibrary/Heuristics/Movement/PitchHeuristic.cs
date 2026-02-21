using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting pitch (up/down look) angle values in game memory.
/// Pitch values typically:
/// - Are floats in range -90.0 to +90.0 degrees
/// - Change continuously with mouse/controller input
/// - Clamp at -90/+90 (looking straight up/down)
/// - Different from yaw which is 0-360
/// </summary>
public sealed class PitchHeuristic : IValueHeuristic
{
    public string Name => "Pitch Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasFloatPrecision = false;
        bool clampDetected = false;
        int positiveCount = 0;
        int negativeCount = 0;

        // Check value range - pitch is typically -90 to +90
        if (IsInPitchRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Analyze observation history
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

            // Track positive/negative (looking up/down)
            if (val > 5.0) positiveCount++;
            if (val < -5.0) negativeCount++;

            // Check for clamping at boundaries
            if (Math.Abs(val) > 85.0 && Math.Abs(val) <= 90.0)
            {
                clampDetected = true;
            }
        }

        // Bonus for float precision (angular values)
        if (hasFloatPrecision)
        {
            score += 0.15;
        }

        // Bonus for clamp detection (distinctive of pitch)
        if (clampDetected)
        {
            score += 0.2;
        }

        // Pitch can be both positive and negative
        if (positiveCount > 0 && negativeCount > 0)
        {
            score += 0.15;
        }

        // Correlation with camera/aim actions
        int aimEvents = history.Count(h => h.RelatedAction == PlayerAction.AimChanged);
        if (aimEvents >= 2)
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

    private static bool IsInPitchRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -90.0 && val <= 90.0;
        }
        catch
        {
            return false;
        }
    }
}