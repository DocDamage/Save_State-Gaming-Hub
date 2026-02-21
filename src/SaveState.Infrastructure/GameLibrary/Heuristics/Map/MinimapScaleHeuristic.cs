using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting minimap zoom/scale level.
/// Minimap Scale values typically:
/// - Are floats (0.1-5.0) representing scale multiplier
/// - Change when zooming minimap
/// - Often smaller range than full map zoom
/// </summary>
public sealed class MinimapScaleHeuristic : IValueHeuristic
{
    public string Name => "Minimap Scale Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasScaleChanges = false;

        // Check value range (minimap scale typically 0.5-5.0)
        if (IsInMinimapScaleRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Float type preferred
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

            // Check for scale changes
            if (currVal != prevVal)
            {
                hasScaleChanges = true;
                var delta = Math.Abs(currVal.Value - prevVal.Value);
                // Scale usually changes in small steps
                if (delta >= 0.1 && delta <= 1.0)
                {
                    score += 0.12;
                }
            }

            // Common minimap scales
            var commonScales = new[] { 0.5, 0.75, 1.0, 1.25, 1.5, 2.0 };
            foreach (var scale in commonScales)
            {
                if (Math.Abs(currVal.Value - scale) < 0.05)
                {
                    score += 0.08;
                    break;
                }
            }

            // Should be positive
            if (currVal <= 0)
            {
                score -= 0.5;
            }

            // Reasonable maximum
            if (currVal > 20)
            {
                score -= 0.3;
            }
        }

        // Bonus for scale changes
        if (hasScaleChanges)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInMinimapScaleRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.1 && val <= 20.0;
        }
        catch
        {
            return false;
        }
    }
}