using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting planks/processed wood resource in crafting/construction games.
/// Plank values typically:
/// - Are integers (0-9999)
/// - Increase when processing wood at sawmill
/// - Decrease when building structures or furniture
/// </summary>
public sealed class PlankHeuristic : IValueHeuristic
{
    public string Name => "Planks/Processed Wood Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gainEvents = 0;
        int spendEvents = 0;

        // Check value range (planks typically 0-9999)
        if (IsInPlankRange(value.CurrentValue))
        {
            score += 0.3;
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

            // Check for gain (processing wood)
            if (currVal > prevVal)
            {
                gainEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Processing yields 2-8 planks per wood unit
                if (delta >= 1 && delta <= 20)
                {
                    score += 0.12;
                }
            }

            // Check for spend (building)
            if (currVal < prevVal)
            {
                spendEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Building uses 4-50 planks
                if (delta >= 2 && delta <= 100)
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

        // Bonus for transaction patterns
        if (gainEvents >= 2)
            score += 0.15;
        if (spendEvents >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInPlankRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 99999;
        }
        catch
        {
            return false;
        }
    }
}