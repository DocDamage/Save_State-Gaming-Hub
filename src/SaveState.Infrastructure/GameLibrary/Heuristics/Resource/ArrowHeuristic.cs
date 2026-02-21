using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting arrow/ammunition count in RPG/action games.
/// Arrow values typically:
/// - Are integers (0-999)
/// - Decrease by 1 when fired
/// - Increase when crafting or looting in batches
/// </summary>
public sealed class ArrowHeuristic : IValueHeuristic
{
    public string Name => "Arrows/Ammunition Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gainEvents = 0;
        int fireEvents = 0;

        // Check value range (arrows typically 0-999)
        if (IsInArrowRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Must be integer
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

            // Check for gain (crafting/looting)
            if (currVal > prevVal)
            {
                gainEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Usually gain 5-50 arrows at a time
                if (delta >= 5 && delta <= 100)
                {
                    score += 0.15;
                }
            }

            // Check for fire (shooting arrow)
            if (currVal < prevVal)
            {
                fireEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Shooting arrow decreases by exactly 1
                if (delta == 1)
                {
                    score += 0.25;
                }
                else if (delta >= 1 && delta <= 10)
                {
                    score += 0.1;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Strong bonus for single-shot pattern (arrows fire one at a time)
        if (fireEvents >= 3)
            score += 0.25;
        if (gainEvents >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInArrowRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 9999;
        }
        catch
        {
            return false;
        }
    }
}