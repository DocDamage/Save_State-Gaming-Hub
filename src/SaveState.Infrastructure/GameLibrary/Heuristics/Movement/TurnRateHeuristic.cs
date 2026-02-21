using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting turn rate values in game memory.
/// Turn rate values typically:
/// - Are floats in range -360.0 to +360.0 (degrees per second)
/// - Zero when not turning
/// - Positive for right turns, negative for left turns
/// - Affects how quickly facing/rotation changes
/// </summary>
public sealed class TurnRateHeuristic : IValueHeuristic
{
    public string Name => "Turn Rate Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasNegative = false;
        bool hasPositive = false;
        int zeroWhenNotTurning = 0;
        bool valuesAreModerate = false;

        // Check value range
        if (IsInTurnRateRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Analyze observation history
        if (history.Count >= 3)
        {
            var nonZeroValues = history
                .Where(h => h.Value != null)
                .Select(h => HeuristicUtilities.ConvertToDouble(h.Value))
                .Where(v => v.HasValue && Math.Abs(v.Value) > 0.01)
                .Select(v => v!.Value)
                .ToList();

            // Turn rates should be reasonable (not extreme)
            if (nonZeroValues.Count >= 2)
            {
                var avgAbs = nonZeroValues.Average(v => Math.Abs(v));
                if (avgAbs >= 10.0 && avgAbs <= 180.0)
                {
                    valuesAreModerate = true;
                }
            }
        }

        for (int i = 0; i < history.Count; i++)
        {
            if (history[i].Value == null)
                continue;

            double? currVal = HeuristicUtilities.ConvertToDouble(history[i].Value);
            if (!currVal.HasValue)
                continue;

            var val = currVal.Value;

            // Track positive/negative (right/left)
            if (val > 1.0) hasPositive = true;
            if (val < -1.0) hasNegative = true;

            // Turn rate is 0 when not turning
            if (i > 0 && history[i].RelatedAction == null && Math.Abs(val) < 0.01)
            {
                zeroWhenNotTurning++;
            }
        }

        // Bonus for moderate values
        if (valuesAreModerate)
        {
            score += 0.2;
        }

        // Bonus for having both directions
        if (hasNegative && hasPositive)
        {
            score += 0.25;
        }

        // Bonus for being zero when not turning
        if (zeroWhenNotTurning >= 2)
        {
            score += 0.2;
        }

        // Correlation with rotation events
        int rotationEvents = history.Count(h => h.RelatedAction == PlayerAction.Rotated);
        if (rotationEvents >= 2)
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

    private static bool IsInTurnRateRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -360.0 && val <= 360.0;
        }
        catch
        {
            return false;
        }
    }
}