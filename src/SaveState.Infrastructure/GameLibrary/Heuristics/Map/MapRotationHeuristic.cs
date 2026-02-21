using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting map rotation in navigation systems.
/// Map rotation values typically:
/// - Are floats (0-360 degrees)
/// - Change as player rotates map
/// - Often snap to north when reset
/// </summary>
public sealed class MapRotationHeuristic : IValueHeuristic
{
    public string Name => "Map Rotation Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool wrapAroundDetected = false;

        // Check value range (rotation 0-360)
        if (IsInRotationRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Float type preferred
        if (value.ValueType.ToLowerInvariant() is "float" or "single" or "double")
        {
            score += 0.1;
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

            // Check for wrap-around (0 to 360 or vice versa)
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta > 300 && delta < 400)
            {
                wrapAroundDetected = true;
                score += 0.25;
            }

            // Check for north snap (0 or 360)
            if (Math.Abs(currVal.Value) < 5 || Math.Abs(currVal.Value - 360) < 5)
            {
                score += 0.1;
            }

            // Should be in 0-360 range
            if (currVal < 0 || currVal > 360)
            {
                score -= 0.4;
            }
        }

        // Bonus for wrap-around (distinctive of rotation)
        if (wrapAroundDetected)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInRotationRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 360;
        }
        catch
        {
            return false;
        }
    }
}