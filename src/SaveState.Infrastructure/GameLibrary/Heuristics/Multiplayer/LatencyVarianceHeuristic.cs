using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting network latency variance (jitter) in multiplayer games.
/// Latency variance values typically:
/// - Are integers (milliseconds)
/// - Range from 0-100ms normally
/// - Indicate network stability
/// - Lower is better
/// </summary>
public sealed class LatencyVarianceHeuristic : IValueHeuristic
{
    public string Name => "Latency Variance Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasFluctuation = false;
        bool inNormalRange = true;

        // Check value range (jitter typically 0-200ms)
        if (IsInJitterRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Must be integer type (jitter is rarely float)
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.2;
        }
        else
        {
            score += 0.1;
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

            // Check for fluctuation (variance changes)
            if (Math.Abs(currVal.Value - prevVal.Value) > 0)
            {
                hasFluctuation = true;
            }

            // Check for normal range
            if (currVal > 100)
            {
                inNormalRange = false;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Extremely high variance is possible but concerning
            if (currVal > 500)
            {
                score -= 0.3;
            }
        }

        // Bonus for fluctuation (network jitter varies)
        if (hasFluctuation)
            score += 0.2;

        // Bonus for staying in normal range
        if (inNormalRange && history.Count > 1)
            score += 0.15;

        // Check for common jitter ranges
        var avgValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Average();

        // Good jitter: 0-20ms, Acceptable: 20-50ms
        if (avgValue >= 0 && avgValue <= 50)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInJitterRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 1000;
        }
        catch
        {
            return false;
        }
    }
}