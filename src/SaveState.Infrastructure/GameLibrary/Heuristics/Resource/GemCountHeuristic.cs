using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting premium/gem currency in mobile/F2P games.
/// Gem values typically:
/// - Are integers (premium currency)
/// - Increase with purchases or rare drops
/// - Decrease when buying premium items
/// </summary>
public sealed class GemCountHeuristic : IValueHeuristic
{
    public string Name => "Premium Currency/Gems Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increaseEvents = 0;
        int decreaseEvents = 0;

        // Check value range (gems typically 0-99999)
        if (IsInGemRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Must be integer type
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

            // Check for increase (purchase or drop)
            if (currVal > prevVal)
            {
                increaseEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Purchases give round numbers
                if (delta % 10 == 0 || delta % 100 == 0)
                {
                    score += 0.12;
                }
            }

            // Check for decrease (spending)
            if (currVal < prevVal)
            {
                decreaseEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Premium items cost round amounts
                if (delta % 10 == 0 || delta % 50 == 0 || delta % 100 == 0)
                {
                    score += 0.1;
                }
            }

            // Should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for transaction patterns
        if (increaseEvents >= 1)
            score += 0.1;
        if (decreaseEvents >= 1)
            score += 0.1;

        // Gems often have specific purchase values
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            var commonGems = new[] { 0.0, 100.0, 500.0, 1000.0, 5000.0, 10000.0 };
            foreach (var common in commonGems)
            {
                if (Math.Abs(currentVal.Value - common) < 10)
                {
                    score += 0.1;
                    break;
                }
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long";
    }

    private static bool IsInGemRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 999999;
        }
        catch
        {
            return false;
        }
    }
}