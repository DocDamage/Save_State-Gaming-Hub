using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting active waypoint X coordinate.
/// Active Waypoint X values typically:
/// - Are floats representing target X position
/// - Change when setting new waypoints
/// - Often constant between waypoint changes
/// </summary>
public sealed class ActiveWaypointXHeuristic : IValueHeuristic
{
    public string Name => "Active Waypoint X Coordinate Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int waypointChanges = 0;

        // Check value range (waypoint coords typically -100000 to 100000)
        if (IsInWaypointCoordinateRange(value.CurrentValue))
        {
            score += 0.35;
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

            // Waypoint changes should be infrequent and significant
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta > 100)
            {
                waypointChanges++;
                score += 0.15;
            }
            else if (delta < 0.01)
            {
                // Values often constant
                score += 0.05;
            }

            // Extreme values suspicious
            if (Math.Abs(currVal.Value) > 1000000)
            {
                score -= 0.4;
            }
        }

        // Waypoint changes should be relatively infrequent
        if (waypointChanges >= 1 && waypointChanges <= history.Count / 3)
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

    private static bool IsInWaypointCoordinateRange(object? value)
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