using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting momentum values in game memory.
/// Momentum values typically:
/// - Are floats representing mass * velocity
/// - Accumulate during movement, preserved through physics
/// - Affect collision impacts and movement fluidity
/// </summary>
public sealed class MomentumHeuristic : IValueHeuristic
{
    public string Name => "Momentum Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increasingCount = 0;
        int decreasingCount = 0;
        double prevVal = 0;

        // Check value range - momentum can be large depending on mass
        if (IsInMomentumRange(value.CurrentValue))
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

            // Track increasing/decreasing patterns
            if (i > 0)
            {
                if (val > prevVal + 0.1)
                    increasingCount++;
                else if (val < prevVal - 0.1)
                    decreasingCount++;
            }

            // Momentum should never be negative
            if (val < 0)
            {
                score -= 0.3;
            }

            prevVal = val;
        }

        // Momentum builds up (increasing) during acceleration
        if (increasingCount >= 2)
        {
            score += 0.2;
        }

        // Momentum decreases when stopping
        if (decreasingCount >= 2)
        {
            score += 0.15;
        }

        // Correlation with position changes
        int positionChangeEvents = history.Count(h => h.RelatedAction == PlayerAction.PositionChanged);
        if (positionChangeEvents >= 2)
        {
            score += 0.2;
        }

        // Momentum is preserved (persists)
        if (history.Count >= 5)
        {
            var nonZeroCount = history.Count(h =>
            {
                var val = HeuristicUtilities.ConvertToDouble(h.Value);
                return val.HasValue && val.Value > 0.01;
            });
            if (nonZeroCount > history.Count * 0.6)
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

    private static bool IsInMomentumRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 5000.0;
        }
        catch
        {
            return false;
        }
    }
}