using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting position coordinates in game memory.
/// Position values typically:
/// - Are floats (X, Y, Z coordinates)
/// - Change smoothly (not jumping instantly)
/// - Are consecutive in memory (X at addr, Y at addr+4, Z at addr+8)
/// - Change on "PositionChanged" action
/// - Values typically in range -10000 to +10000
/// </summary>
public sealed class PositionHeuristic : IValueHeuristic
{
    public string Name => "Position Detection";
    public string Category => "Position";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int smoothChanges = 0;
        int positionChanges = 0;
        double totalDelta = 0;

        // Check value range for position
        if (IsInPositionRange(value.CurrentValue))
        {
            score += 0.25;
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

            var delta = Math.Abs(currVal.Value - prevVal.Value);
            totalDelta += delta;

            // Position changes should be smooth, not instant jumps
            if (delta > 0 && delta < 1000)
            {
                smoothChanges++;
            }

            // Large instant jumps are suspicious
            if (delta > 5000)
            {
                score -= 0.1;
            }

            // Check for position changed action correlation
            if (curr.RelatedAction == PlayerAction.PositionChanged)
            {
                positionChanges++;
            }
        }

        // Bonus for smooth movement patterns
        if (history.Count > 1)
        {
            var smoothRatio = (double)smoothChanges / (history.Count - 1);
            score += smoothRatio * 0.2;
        }

        // Bonus for position action correlation
        if (positionChanges >= 2)
        {
            score += 0.2;
        }

        // Check for consistent change pattern (positions change gradually)
        if (history.Count > 2 && totalDelta > 0)
        {
            var avgDelta = totalDelta / (history.Count - 1);
            if (avgDelta > 0.1 && avgDelta < 100)
            {
                score += 0.15;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInPositionRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -100000 && val <= 100000; // Wide range for various game scales
        }
        catch
        {
            return false;
        }
    }
}
