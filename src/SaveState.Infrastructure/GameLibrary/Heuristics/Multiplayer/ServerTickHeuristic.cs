using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting server tick count in multiplayer games.
/// Server tick values typically:
/// - Are integers that increment constantly
/// - Start from 0 or 1 at match start
/// - Only increase (never decrease)
/// - Increment at a steady rate (tick rate dependent)
/// </summary>
public sealed class ServerTickHeuristic : IValueHeuristic
{
    public string Name => "Server Tick Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool onlyIncreases = true;
        int incrementEvents = 0;
        bool steadyIncrement = true;

        // Check value range (server ticks typically 0 to very large)
        if (IsInServerTickRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Must be integer
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.3;
        }
        else
        {
            score += 0.15;
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

            // Check for increment
            if (currVal > prevVal)
            {
                incrementEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Usually increments by small amounts (1-10 per observation)
                if (delta >= 1 && delta <= 20)
                {
                    score += 0.1;
                }
                else if (delta > 20 && delta <= 100)
                {
                    score += 0.05;
                }
                else if (delta > 100)
                {
                    // Large jumps are suspicious
                    steadyIncrement = false;
                }
            }
            // Should not decrease
            else if (currVal < prevVal)
            {
                onlyIncreases = false;
                score -= 0.4;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Strong bonus for increment events
        if (incrementEvents >= 2)
            score += 0.2;

        // Strong bonus for only increasing
        if (onlyIncreases && history.Count > 2)
            score += 0.25;

        // Bonus for steady increment
        if (steadyIncrement)
            score += 0.05;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "uint32" or "uint" or "uint64" or "ulong";
    }

    private static bool IsInServerTickRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Server ticks can get very large over time
            return val >= 0 && val <= 999999999999;
        }
        catch
        {
            return false;
        }
    }
}