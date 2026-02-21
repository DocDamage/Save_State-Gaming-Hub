using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting lean angle values in game memory.
/// Lean angle values typically:
/// - Are floats in range -45.0 to +45.0 degrees
/// - Used when peeking around corners or leaning
/// - Returns to 0 when not leaning
/// - Common in tactical shooters
/// </summary>
public sealed class LeanAngleHeuristic : IValueHeuristic
{
    public string Name => "Lean Angle Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasNegative = false;
        bool hasPositive = false;
        int zeroWhenNotLeaning = 0;
        bool returnsToCenter = false;

        // Check value range - lean is typically -45 to +45 degrees
        if (IsInLeanRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Analyze observation history
        for (int i = 0; i < history.Count; i++)
        {
            if (history[i].Value == null)
                continue;

            double? currVal = HeuristicUtilities.ConvertToDouble(history[i].Value);
            if (!currVal.HasValue)
                continue;

            var val = currVal.Value;

            // Track positive/negative (lean right/left)
            if (val > 5.0) hasPositive = true;
            if (val < -5.0) hasNegative = true;

            // Check for returning to center (0)
            if (i > 0 && Math.Abs(val) < 0.1)
            {
                zeroWhenNotLeaning++;

                var prevVal = HeuristicUtilities.ConvertToDouble(history[i - 1].Value);
                if (prevVal.HasValue && Math.Abs(prevVal.Value) > 5.0)
                {
                    returnsToCenter = true;
                }
            }
        }

        // Bonus for having both directions
        if (hasNegative && hasPositive)
        {
            score += 0.2;
        }

        // Bonus for returning to center
        if (returnsToCenter)
        {
            score += 0.25;
        }

        // Bonus for being zero when not leaning
        if (zeroWhenNotLeaning >= 2)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInLeanRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -45.0 && val <= 45.0;
        }
        catch
        {
            return false;
        }
    }
}