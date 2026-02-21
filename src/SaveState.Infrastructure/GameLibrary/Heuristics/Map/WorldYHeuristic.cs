using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting world Y coordinate (vertical) in 3D games.
/// World Y values typically:
/// - Are floats representing vertical position
/// - Change with jumping/falling/climbing
/// - Often constrained by terrain/building limits
/// </summary>
public sealed class WorldYHeuristic : IValueHeuristic
{
    public string Name => "World Y Coordinate Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasVerticalMovement = false;

        // Check value range (Y typically -500 to 10000 depending on game)
        if (IsInWorldYRange(value.CurrentValue))
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

            // Check for vertical movement
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta > 0.1 && delta < 500)
            {
                hasVerticalMovement = true;
                score += 0.08;
            }

            // Extreme changes might be teleports (less common)
            if (delta > 2000)
            {
                score -= 0.15;
            }

            // Negative Y often means underground/below sea level
            if (currVal < -1000)
            {
                score -= 0.1;
            }
        }

        // Bonus for vertical movement patterns
        if (hasVerticalMovement && history.Count > 2)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInWorldYRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -1000 && val <= 20000;
        }
        catch
        {
            return false;
        }
    }
}