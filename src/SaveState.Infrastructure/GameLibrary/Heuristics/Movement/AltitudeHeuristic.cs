using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting altitude/height in flight/sandbox games.
/// Altitude values typically:
/// - Are floats with decimal precision (meters/feet)
/// - Change continuously during flight/climbing
/// - Can be positive (above ground) or negative (below sea level)
/// - Often correlate with position changes
/// </summary>
public sealed class AltitudeHeuristic : IValueHeuristic
{
    public string Name => "Altitude/Height Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasFloatPrecision = false;
        int changeCount = 0;
        bool canBeNegative = false;

        // Check for float type (altitude usually has decimals)
        if (IsFloatType(value.ValueType))
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

            // Check for float precision (altitude is rarely whole number)
            if (currVal.Value != Math.Floor(currVal.Value))
            {
                hasFloatPrecision = true;
            }

            // Check for changes
            if (Math.Abs(currVal.Value - prevVal.Value) > 0.01)
            {
                changeCount++;
            }

            // Check if value can be negative (below sea level/takeoff point)
            if (currVal < 0)
            {
                canBeNegative = true;
            }

            // Check for reasonable altitude range
            if (currVal > 100000 || currVal < -10000)
            {
                score -= 0.3;
            }
        }

        // Bonus for float precision
        if (hasFloatPrecision)
            score += 0.2;

        // Bonus for continuous changes
        if (changeCount >= 3)
            score += 0.15;

        // Bonus for negative capability (distinctive of altitude)
        if (canBeNegative)
            score += 0.15;

        // Check if correlated with PositionChanged
        int positionCorrelations = history.Count(o => o.RelatedAction == PlayerAction.PositionChanged);
        if (positionCorrelations > 0)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsFloatType(string valueType)
    {
        var normalized = valueType.ToLowerInvariant();
        return normalized is "float" or "single" or "double";
    }
}