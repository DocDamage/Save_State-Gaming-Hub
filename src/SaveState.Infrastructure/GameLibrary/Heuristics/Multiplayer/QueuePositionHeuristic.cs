using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting matchmaking queue position in multiplayer games.
/// Queue position values typically:
/// - Are integers (1-1000+)
/// - Decrease as queue progresses
/// - Reach 0 or 1 when match found
/// - Reset when entering new queue
/// </summary>
public sealed class QueuePositionHeuristic : IValueHeuristic
{
    public string Name => "Queue Position Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool generallyDecreases = true;
        int decreaseEvents = 0;
        int resetEvents = 0;

        // Check value range (queue position typically 0-10000)
        if (IsInQueuePositionRange(value.CurrentValue))
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

            // Check for decrease (moving up in queue)
            if (currVal < prevVal)
            {
                decreaseEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Usually moves by 1 or small amounts
                if (delta >= 1 && delta <= 10)
                {
                    score += 0.1;
                }
            }
            // Check for reset (new queue)
            else if (currVal > prevVal && currVal > 10 && prevVal < 5)
            {
                resetEvents++;
                score += 0.1;
            }
            // Increases are suspicious
            else if (currVal > prevVal)
            {
                generallyDecreases = false;
                score -= 0.15;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for decrease events
        if (decreaseEvents >= 1)
            score += 0.2;

        // Bonus for reset events
        if (resetEvents >= 1)
            score += 0.1;

        // Bonus for generally decreasing
        if (generallyDecreases && history.Count > 2)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInQueuePositionRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 50000;
        }
        catch
        {
            return false;
        }
    }
}