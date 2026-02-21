using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting straight-line distance to destination.
/// Destination Distance values typically:
/// - Are floats representing meters to objective
/// - Decrease when approaching, increase when moving away
/// - Often shown in quest/navigation UI
/// </summary>
public sealed class DestinationDistanceHeuristic : IValueHeuristic
{
    public string Name => "Destination Distance Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasDistanceChanges = false;
        int approachEvents = 0;

        // Check value range (distance typically 0-50000)
        if (IsInDestinationDistanceRange(value.CurrentValue))
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

            // Check for distance changes
            var delta = currVal.Value - prevVal.Value;
            if (Math.Abs(delta) > 0.1)
            {
                hasDistanceChanges = true;
                // Approaching destination
                if (delta < 0)
                {
                    approachEvents++;
                    score += 0.12;
                }
                else
                {
                    score += 0.06;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Zero means at destination
            if (currVal == 0)
            {
                score += 0.15;
            }

            // Extreme values suspicious
            if (currVal > 100000)
            {
                score -= 0.3;
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

    private static bool IsInDestinationDistanceRange(object? value)
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