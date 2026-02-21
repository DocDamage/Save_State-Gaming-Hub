using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting distance to active waypoint.
/// Waypoint Distance values typically:
/// - Are floats representing meters/units to target
/// - Decrease as player approaches waypoint
/// - Increase if player moves away
/// </summary>
public sealed class WaypointDistanceHeuristic : IValueHeuristic
{
    public string Name => "Waypoint Distance Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasDistanceChanges = false;
        int approachEvents = 0;

        // Check value range (distance typically 0-50000)
        if (IsInDistanceRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Float type preferred for distance
        if (value.ValueType.ToLowerInvariant() is "float" or "single" or "double")
        {
            score += 0.15;
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

            // Check for distance changes
            var delta = currVal.Value - prevVal.Value;
            if (Math.Abs(delta) > 0.1)
            {
                hasDistanceChanges = true;
                // Approaching waypoint (decreasing)
                if (delta < 0)
                {
                    approachEvents++;
                    score += 0.1;
                }
                else
                {
                    score += 0.05;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Extreme distances suspicious
            if (currVal > 1000000)
            {
                score -= 0.4;
            }

            // Zero means reached waypoint
            if (currVal == 0)
            {
                score += 0.1;
            }
        }

        // Bonus for distance changes
        if (hasDistanceChanges)
            score += 0.15;

        // Bonus for approach patterns
        if (approachEvents >= 2)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInDistanceRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 100000;
        }
        catch
        {
            return false;
        }
    }
}