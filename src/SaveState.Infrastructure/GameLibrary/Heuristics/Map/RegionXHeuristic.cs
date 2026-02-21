using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting region/map section X coordinate.
/// Region X values typically:
/// - Are integers representing large map region index
/// - Change infrequently when entering new zones
/// - Used in games with region-based world subdivision
/// </summary>
public sealed class RegionXHeuristic : IValueHeuristic
{
    public string Name => "Region X Coordinate Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int regionChanges = 0;

        // Check value range (regions typically 0-100 or -50 to 50)
        if (IsInRegionRange(value.CurrentValue))
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

            // Region changes should be infrequent and small steps
            var delta = currVal.Value - prevVal.Value;
            if (Math.Abs(delta) == 1)
            {
                regionChanges++;
                score += 0.2;
            }

            // Multiple region jumps possible (fast travel)
            if (Math.Abs(delta) > 1 && Math.Abs(delta) < 10)
            {
                score += 0.05;
            }

            // Values should stay in reasonable range
            if (Math.Abs(currVal.Value) > 1000)
            {
                score -= 0.4;
            }
        }

        // Region changes should be relatively rare
        if (regionChanges >= 1 && regionChanges <= history.Count / 5)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInRegionRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -500 && val <= 500;
        }
        catch
        {
            return false;
        }
    }
}