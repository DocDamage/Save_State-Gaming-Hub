using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting current map layer/floor level.
/// Map Layer values typically:
/// - Are integers (0-10 or -5 to 5)
/// - Change when switching floors in multi-level areas
/// - Often used in dungeons, buildings, or cave systems
/// </summary>
public sealed class MapLayerHeuristic : IValueHeuristic
{
    public string Name => "Map Layer/Floor Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int layerChanges = 0;

        // Check value range (layers typically -10 to 20)
        if (IsInLayerRange(value.CurrentValue))
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

            // Layer changes should be +/- 1
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta == 1)
            {
                layerChanges++;
                score += 0.2;
            }
            else if (delta > 1 && delta <= 3)
            {
                // Possible stairs/ramps
                score += 0.08;
            }
            else if (delta > 10)
            {
                // Extreme jumps suspicious
                score -= 0.15;
            }

            // Extreme values suspicious
            if (Math.Abs(currVal.Value) > 100)
            {
                score -= 0.3;
            }
        }

        // Layer changes should be relatively infrequent
        if (layerChanges >= 1 && layerChanges <= history.Count / 4)
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

    private static bool IsInLayerRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -100 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}