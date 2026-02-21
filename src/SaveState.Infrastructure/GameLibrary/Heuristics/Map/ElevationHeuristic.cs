using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting player elevation/altitude.
/// Elevation values typically:
/// - Are floats representing height above sea level
/// - Change with terrain and vertical movement
/// - Can be positive (mountains) or negative (underwater/caves)
/// </summary>
public sealed class ElevationHeuristic : IValueHeuristic
{
    public string Name => "Elevation/Altitude Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasElevationChanges = false;

        // Check value range (elevation typically -1000 to 10000)
        if (IsInElevationRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Float type preferred for elevation
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

            // Check for elevation changes
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta > 0.1 && delta < 500)
            {
                hasElevationChanges = true;
                score += 0.08;
            }

            // Extreme changes might be teleports
            if (delta > 5000)
            {
                score -= 0.2;
            }

            // Negative elevation valid (underground/underwater)
            if (currVal < -1000)
            {
                score -= 0.1;
            }

            // Extreme high elevation
            if (currVal > 20000)
            {
                score -= 0.3;
            }

            // Sea level is common
            if (Math.Abs(currVal.Value) < 10)
            {
                score += 0.05;
            }
        }

        // Bonus for elevation changes
        if (hasElevationChanges && history.Count > 2)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInElevationRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -5000 && val <= 30000;
        }
        catch
        {
            return false;
        }
    }
}