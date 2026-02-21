using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting gold/currency in RPG games (distinct from general currency).
/// Gold values typically:
/// - Are integers (not floats)
/// - Can be very large numbers (0-9999999)
/// - Increase from selling items/quests and decrease from purchases
/// - Often have different naming (Gold, GP, G, etc.)
/// </summary>
public sealed class GoldHeuristic : IValueHeuristic
{
    public string Name => "Gold/RPG Currency Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increaseEvents = 0;
        int decreaseEvents = 0;
        bool largeValues = false;

        // Check value range (gold can be very large)
        if (IsInGoldRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Must be integer type (gold is rarely float)
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

            // Check for large values (common in RPGs)
            if (currVal >= 1000)
            {
                largeValues = true;
            }

            // Check for increase (earning gold)
            if (currVal > prevVal)
            {
                increaseEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Quest rewards are typically larger
                if (delta >= 10)
                {
                    score += 0.08;
                }
            }

            // Check for decrease (spending gold)
            if (currVal < prevVal)
            {
                decreaseEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Purchases often round numbers
                if (delta % 10 == 0 || delta % 100 == 0)
                {
                    score += 0.05;
                }
            }

            // Gold should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for large values
        if (largeValues)
            score += 0.15;

        // Bonus for transaction patterns
        if (increaseEvents >= 1 && decreaseEvents >= 1)
            score += 0.2;

        // RPG gold often has specific values
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && (currentVal.Value % 100 == 0 || currentVal.Value % 1000 == 0))
        {
            score += 0.05;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "int16" or "short";
    }

    private static bool IsInGoldRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 999999999;
        }
        catch
        {
            return false;
        }
    }
}