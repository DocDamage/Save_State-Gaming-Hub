using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting dash/dodge distance values in game memory.
/// Dash distance values typically:
/// - Are floats in range 0.0-20.0
/// - Reset to 0, then spike during dash
/// - Return to 0 after dash completes
/// - Common in action games and souls-likes
/// </summary>
public sealed class DashDistanceHeuristic : IValueHeuristic
{
    public string Name => "Dash Distance Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int zeroCount = 0;
        int spikeCount = 0;
        bool sawSpike = false;

        // Check value range
        if (IsInDashDistanceRange(value.CurrentValue))
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

            // Count zeros
            if (val < 0.01)
            {
                zeroCount++;
                if (sawSpike)
                {
                    // Returned to zero after spike - good pattern
                    score += 0.05;
                    sawSpike = false;
                }
            }
            // Detect spikes (high values)
            else if (val > 2.0)
            {
                spikeCount++;
                sawSpike = true;
            }

            // Dash distance should never be negative
            if (val < 0)
            {
                score -= 0.3;
            }
        }

        // Bonus for many zeros (mostly idle)
        if (zeroCount >= history.Count * 0.6)
        {
            score += 0.25;
        }

        // Bonus for spike patterns
        if (spikeCount >= 1 && spikeCount <= history.Count * 0.3)
        {
            score += 0.3;
        }

        // Correlation with dodge/roll events
        int dodgeEvents = history.Count(h => h.RelatedAction == PlayerAction.Dodged);
        if (dodgeEvents >= 1)
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

    private static bool IsInDashDistanceRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 20.0;
        }
        catch
        {
            return false;
        }
    }
}