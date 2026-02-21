using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting map completion percentage.
/// Map Completion values typically:
/// - Are floats (0.0-100.0) representing percentage
/// - Only increase as map is explored
/// - Never decrease
/// </summary>
public sealed class MapCompletionPercentHeuristic : IValueHeuristic
{
    public string Name => "Map Completion Percentage Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool onlyIncreases = true;

        // Check value range (percentage 0-100)
        if (IsInPercentageRange(value.CurrentValue))
        {
            score += 0.45;
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

            // Should only increase or stay same
            if (currVal >= prevVal)
            {
                var delta = currVal.Value - prevVal.Value;
                // Small gradual increases
                if (delta > 0 && delta < 10)
                {
                    score += 0.1;
                }
                // Large jumps suspicious unless near completion
                if (delta > 20 && currVal < 100)
                {
                    score -= 0.2;
                }
            }
            else
                       {
                onlyIncreases = false;
                score -= 0.4;
            }

            // Should not exceed 100
            if (currVal > 100)
            {
                score -= 0.5;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Exactly 100 is common (completion)
            if (currVal == 100)
            {
                score += 0.1;
            }
        }

        // Bonus for only increasing
        if (onlyIncreases && history.Count > 2)
            score += 0.25;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInPercentageRange(object? value)
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