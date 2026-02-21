using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting underground depth/dungeon floor.
/// Underground Depth values typically:
/// - Are integers (0-20) representing floors or depth levels
/// - 0 means surface, positive values mean underground
/// - Change when entering/exiting caves, dungeons, mines
/// </summary>
public sealed class UndergroundDepthHeuristic : IValueHeuristic
{
    public string Name => "Underground Depth Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int depthChanges = 0;

        // Check value range (depth typically 0-50)
        if (IsInDepthRange(value.CurrentValue))
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

            // Depth changes should be +/- 1
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta == 1)
            {
                depthChanges++;
                score += 0.2;
            }
            else if (delta > 1 && delta <= 5)
            {
                // Possible ladder/elevator
                score += 0.08;
            }
            else if (delta > 10)
            {
                // Extreme jumps suspicious
                score -= 0.15;
            }

            // Surface (0) is common
            if (currVal == 0)
            {
                score += 0.1;
            }

            // Should be non-negative
            if (currVal < 0)
            {
                score -= 0.4;
            }

            // Extreme depth suspicious
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Depth changes should be relatively infrequent
        if (depthChanges >= 1 && depthChanges <= history.Count / 4)
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

    private static bool IsInDepthRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 200;
        }
        catch
        {
            return false;
        }
    }
}