using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting world Z coordinate (depth) in 3D games.
/// World Z values typically:
/// - Are floats representing depth/forward-backward position
/// - Change gradually as player moves
/// - Complement X coordinate for 3D positioning
/// </summary>
public sealed class WorldZHeuristic : IValueHeuristic
{
    public string Name => "World Z Coordinate Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasGradualChanges = false;

        // Check value range (world coords typically -100000 to 100000)
        if (IsInWorldCoordinateRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Float type preferred for coordinates
        if (value.ValueType.ToLowerInvariant() is "float" or "single" or "double")
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

            // Check for gradual movement
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta > 0.01 && delta < 100)
            {
                hasGradualChanges = true;
                score += 0.08;
            }

            // Large jumps are suspicious
            if (delta > 1000)
            {
                score -= 0.2;
            }

            // Extreme values unlikely
            if (Math.Abs(currVal.Value) > 1000000)
            {
                score -= 0.3;
            }
        }

        // Bonus for gradual changes
        if (hasGradualChanges && history.Count > 2)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInWorldCoordinateRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -100000 && val <= 100000;
        }
        catch
        {
            return false;
        }
    }
}