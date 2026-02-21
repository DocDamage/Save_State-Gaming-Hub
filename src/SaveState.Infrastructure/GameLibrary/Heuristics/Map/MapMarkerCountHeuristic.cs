using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting number of map markers/waypoints.
/// Marker count values typically:
/// - Are integers (0-50)
/// - Increase when placing markers
/// - Decrease when removing/completing
/// </summary>
public sealed class MapMarkerCountHeuristic : IValueHeuristic
{
    public string Name => "Map Marker/Waypoint Count Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int addEvents = 0;
        int removeEvents = 0;

        // Check value range (markers typically 0-50)
        if (IsInMarkerRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Must be integer
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.3;
        }
        else
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

            // Check for adding marker
            if (currVal == prevVal + 1)
            {
                addEvents++;
                score += 0.15;
            }

            // Check for removing marker
            if (currVal == prevVal - 1)
            {
                removeEvents++;
                score += 0.12;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Reasonable max
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Bonus for add/remove patterns
        if (addEvents >= 1)
            score += 0.1;
        if (removeEvents >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInMarkerRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}