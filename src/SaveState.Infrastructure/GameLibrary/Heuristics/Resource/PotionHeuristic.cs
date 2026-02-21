using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting potion/medical item count in RPG/action games.
/// Potion values typically:
/// - Are integers (0-99)
/// - Increase when looting or purchasing
/// - Decrease when consuming for healing/buffs
/// </summary>
public sealed class PotionHeuristic : IValueHeuristic
{
    public string Name => "Potions/Medical Items Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gainEvents = 0;
        int consumeEvents = 0;

        // Check value range (potions typically 0-99, limited stack size)
        if (IsInPotionRange(value.CurrentValue))
        {
            score += 0.4;
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

            // Check for gain (looting/purchasing)
            if (currVal > prevVal)
            {
                gainEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Usually gain 1-5 potions at a time
                if (delta >= 1 && delta <= 10)
                {
                    score += 0.12;
                }
            }

            // Check for consume (using potion)
            if (currVal < prevVal)
            {
                consumeEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Consuming usually decreases by 1
                if (delta == 1)
                {
                    score += 0.2;
                }
                else if (delta >= 1 && delta <= 5)
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

        // Bonus for consumption pattern (potions are consumed one at a time)
        if (consumeEvents >= 2)
            score += 0.2;
        if (gainEvents >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInPotionRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 999;
        }
        catch
        {
            return false;
        }
    }
}