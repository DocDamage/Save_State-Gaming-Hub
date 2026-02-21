using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting network ping/latency in multiplayer games.
/// Ping values typically:
/// - Are integers (milliseconds)
/// - Range from 5-300ms (normal gameplay)
/// - Fluctuate based on network conditions
/// - Spike during lag
/// </summary>
public sealed class PingHeuristic : IValueHeuristic
{
    public string Name => "Network Ping Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasFluctuation = false;
        bool inNormalRange = true;

        // Check value range (ping typically 0-500ms)
        if (IsInPingRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Must be integer type (ping is rarely float)
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

            // Check for fluctuation (ping varies)
            if (Math.Abs(currVal.Value - prevVal.Value) > 0)
            {
                hasFluctuation = true;
            }

            // Check for normal range
            if (currVal > 500)
            {
                inNormalRange = false;
            }

            // Ping should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Extremely high ping is possible but rare
            if (currVal > 2000)
            {
                score -= 0.3;
            }
        }

        // Bonus for fluctuation (network ping varies constantly)
        if (hasFluctuation)
            score += 0.2;

        // Bonus for staying in normal range
        if (inNormalRange)
            score += 0.15;

        // Check for common ping values
        var avgValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Average();

        // Common ping ranges: 20-50 (good), 50-100 ( playable), 100-200 (laggy)
        if (avgValue >= 10 && avgValue <= 200)
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

    private static bool IsInPingRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 2000;
        }
        catch
        {
            return false;
        }
    }
}