using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting grapple/hook length values in game memory.
/// Grapple length values typically:
/// - Are floats in range 0.0-100.0
/// - 0 when not grappling
/// - Varies during grapple (extends then retracts)
/// - Common in games with grappling hooks or swing mechanics
/// </summary>
public sealed class GrappleLengthHeuristic : IValueHeuristic
{
    public string Name => "Grapple Length Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int zeroWhenNotGrappling = 0;
        int nonZeroCount = 0;
        int variationCount = 0;
        double prevVal = 0;

        // Check value range
        if (IsInGrappleLengthRange(value.CurrentValue))
        {
            score += 0.35;
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

            // Track non-zero values
            if (val > 0.01)
            {
                nonZeroCount++;

                // Check for variation during grapple
                if (i > 0 && Math.Abs(val - prevVal) > 0.5)
                {
                    variationCount++;
                }
            }

            // Grapple length is 0 when not grappling
            if (i > 0 && history[i].RelatedAction == null && val < 0.01)
            {
                zeroWhenNotGrappling++;
            }

            prevVal = val;

            // Grapple length should never be negative
            if (val < 0)
            {
                score -= 0.3;
            }
        }

        // Bonus for being zero when not grappling
        if (zeroWhenNotGrappling >= 2)
        {
            score += 0.25;
        }

        // Bonus for variation during grapple
        if (variationCount >= 2)
        {
            score += 0.25;
        }

        // Bonus for rare activation
        if (nonZeroCount >= 1 && nonZeroCount < history.Count * 0.4)
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

    private static bool IsInGrappleLengthRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 100.0;
        }
        catch
        {
            return false;
        }
    }
}