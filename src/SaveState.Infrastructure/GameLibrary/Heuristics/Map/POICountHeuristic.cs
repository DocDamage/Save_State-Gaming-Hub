using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting total points of interest count.
/// POI Count values typically:
/// - Are integers representing total POIs in game world
/// - Usually constant, may increase with DLC
/// - Higher than discovered count
/// </summary>
public sealed class POICountHeuristic : IValueHeuristic
{
    public string Name => "Total Points of Interest Count Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool mostlyConstant = true;
        double? firstValue = null;

        // Check value range (total POIs typically 10-1000)
        if (IsInTotalPOIRange(value.CurrentValue))
        {
            score += 0.45;
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
        for (int i = 0; i < history.Count; i++)
        {
            var curr = history[i];

            if (curr.Value == null)
                continue;

            double? currVal = HeuristicUtilities.ConvertToDouble(curr.Value);
            if (!currVal.HasValue)
                continue;

            // Track first value for constancy check
            if (firstValue == null)
            {
                firstValue = currVal;
            }

            // Should not change much (maybe DLC adds more)
            if (Math.Abs(currVal.Value - firstValue.Value) > 10)
            {
                mostlyConstant = false;
            }

            // Should be positive
            if (currVal <= 0)
            {
                score -= 0.5;
            }

            // Reasonable maximum
            if (currVal > 5000)
            {
                score -= 0.3;
            }
        }

        // Bonus for being relatively constant
        if (mostlyConstant && history.Count > 2)
            score += 0.25;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInTotalPOIRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 1 && val <= 2000;
        }
        catch
        {
            return false;
        }
    }
}