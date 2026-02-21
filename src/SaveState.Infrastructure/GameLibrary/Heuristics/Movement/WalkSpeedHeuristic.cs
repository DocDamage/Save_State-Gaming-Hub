using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting base walk speed values in game memory.
/// Walk speed values typically:
/// - Are floats in range 0.0-30.0
/// - The default movement speed when not sprinting or crouching
/// - Often a static base value modified by other factors
/// </summary>
public sealed class WalkSpeedHeuristic : IValueHeuristic
{
    public string Name => "Walk Speed Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int zeroWhenStationary = 0;
        int fluctuatingCount = 0;
        double prevVal = 0;
        bool hasBeenNonZero = false;

        // Check value range
        if (IsInWalkSpeedRange(value.CurrentValue))
        {
            score += 0.3;
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

            // Track if value has been non-zero
            if (val > 0.01)
                hasBeenNonZero = true;

            // Walk speed is 0 when stationary
            if (i > 0 && history[i].RelatedAction == null && val < 0.01)
            {
                zeroWhenStationary++;
            }

            // Walk speed fluctuates when moving
            if (i > 0 && Math.Abs(val - prevVal) > 0.1 && val > 0.01)
            {
                fluctuatingCount++;
            }

            prevVal = val;

            // Walk speed should never be negative
            if (val < 0)
            {
                score -= 0.3;
            }
        }

        // Bonus for walk speed that goes to zero when stationary
        if (zeroWhenStationary >= 2)
        {
            score += 0.2;
        }

        // Bonus for fluctuating values when moving
        if (fluctuatingCount >= 3 && hasBeenNonZero)
        {
            score += 0.25;
        }

        // Bonus for correlation with position changes
        int positionChangeEvents = history.Count(h => h.RelatedAction == PlayerAction.PositionChanged);
        if (positionChangeEvents >= 2 && hasBeenNonZero)
        {
            score += 0.15;
        }

        // Walk speed is typically in a moderate range
        if (history.Count >= 3)
        {
            var nonZeroValues = history
                .Where(h => h.Value != null)
                .Select(h => HeuristicUtilities.ConvertToDouble(h.Value))
                .Where(v => v.HasValue && v.Value > 0.01)
                .Select(v => v!.Value)
                .ToList();

            if (nonZeroValues.Count >= 2)
            {
                var avg = nonZeroValues.Average();
                if (avg >= 3.0 && avg <= 12.0)
                {
                    score += 0.1;
                }
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInWalkSpeedRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 30.0;
        }
        catch
        {
            return false;
        }
    }
}