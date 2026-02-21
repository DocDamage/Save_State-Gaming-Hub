using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting map zoom level in open world games.
/// Map zoom values typically:
/// - Are floats (0.5-10.0) representing zoom multiplier
/// - Change with scroll wheel or buttons
/// - Affect visible map area
/// </summary>
public sealed class MapZoomHeuristic : IValueHeuristic
{
    public string Name => "Map Zoom Level Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasChanges = false;

        // Check value range (zoom typically 0.1-50)
        if (IsInZoomRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Float type preferred
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

            // Check for zoom changes
            if (currVal != prevVal)
            {
                hasChanges = true;
                var delta = Math.Abs(currVal.Value - prevVal.Value);
                // Zoom usually changes in steps
                if (delta >= 0.1 && delta <= 2.0)
                {
                    score += 0.1;
                }
            }

            // Common zoom values
            var commonZooms = new[] { 0.5, 1.0, 1.5, 2.0, 3.0, 5.0, 10.0 };
            foreach (var zoom in commonZooms)
            {
                if (Math.Abs(currVal.Value - zoom) < 0.1)
                {
                    score += 0.1;
                    break;
                }
            }

            // Should be positive
            if (currVal <= 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for changes
        if (hasChanges)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInZoomRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.1 && val <= 100.0;
        }
        catch
        {
            return false;
        }
    }
}