using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting boost speed values in game memory.
/// Boost speed values typically:
/// - Are floats in range 0.0-200.0
/// - Sudden spikes from base speed
/// - Limited duration then returns to normal
/// - Common in racing games and flight games
/// </summary>
public sealed class BoostSpeedHeuristic : IValueHeuristic
{
    public string Name => "Boost Speed Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int zeroWhenNotBoosting = 0;
        int spikeCount = 0;
        double? baseSpeed = null;

        // Check value range
        if (IsInBoostSpeedRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Analyze observation history
        if (history.Count >= 3)
        {
            var values = history
                .Where(h => h.Value != null)
                .Select(h => HeuristicUtilities.ConvertToDouble(h.Value))
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            if (values.Count >= 3)
            {
                // Find base speed (most common lower value)
                baseSpeed = values.Where(v => v > 0.01)
                    .GroupBy(v => Math.Round(v, 0))
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key;

                // Count spikes (much higher than base)
                if (baseSpeed.HasValue && baseSpeed.Value > 0)
                {
                    spikeCount = values.Count(v => v > baseSpeed.Value * 1.5);
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

            // Boost speed is 0 when not moving
            if (i > 0 && history[i].RelatedAction == null && val < 0.01)
            {
                zeroWhenNotBoosting++;
            }

            // Boost speed should never be negative
            if (val < 0)
            {
                score -= 0.3;
            }
        }

        // Bonus for spike patterns (boost activates)
        if (spikeCount >= 1 && spikeCount < history.Count * 0.3)
        {
            score += 0.4;
        }

        // Bonus for being zero when not active
        if (zeroWhenNotBoosting >= 2)
        {
            score += 0.2;
        }

        // Correlation with position changes
        int movementEvents = history.Count(h => h.RelatedAction == PlayerAction.PositionChanged);
        if (movementEvents >= 2)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInBoostSpeedRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 200.0;
        }
        catch
        {
            return false;
        }
    }
}