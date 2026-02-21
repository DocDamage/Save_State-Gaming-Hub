using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting client tick count in multiplayer games.
/// Client tick values typically:
/// - Are integers that increment constantly
/// - Slightly ahead or behind server tick
/// - Only increase (never decrease)
/// - Update every frame
/// </summary>
public sealed class ClientTickHeuristic : IValueHeuristic
{
    public string Name => "Client Tick Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool onlyIncreases = true;
        int incrementEvents = 0;
        bool steadyIncrement = true;

        // Check value range (client ticks typically 0 to very large)
        if (IsInClientTickRange(value.CurrentValue))
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
                // Usually increments by 1 per frame/tick
                if (delta == 1)
                {
                    score += 0.15;
                }
                else if (delta > 1 && delta <= 5)
                {
                    score += 0.08;
                }
                else if (delta > 10)
                {
                    // Large jumps might indicate different behavior
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

    private static bool IsInClientTickRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Client ticks can get very large over time
            return val >= 0 && val <= 999999999999;
        }
        catch
        {
            return false;
        }
    }
}