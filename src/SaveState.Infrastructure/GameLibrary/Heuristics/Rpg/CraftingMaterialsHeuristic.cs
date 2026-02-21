using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting crafting material counts.
/// Material values typically:
/// - Are integers (0-9999)
/// - Increase when gathering
/// - Decrease when crafting
/// </summary>
public sealed class CraftingMaterialsHeuristic : IValueHeuristic
{
    public string Name => "Crafting Materials Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increaseEvents = 0;
        int decreaseEvents = 0;

        // Check value range (materials 0-9999)
        if (IsInMaterialRange(value.CurrentValue))
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

            // Check for gathering (increase)
            if (currVal > prevVal)
            {
                increaseEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Gathering gives small amounts
                if (delta >= 1 && delta <= 10)
                {
                    score += 0.1;
                }
            }

            // Check for crafting (decrease)
            if (currVal < prevVal)
            {
                decreaseEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Crafting uses specific amounts
                if (delta >= 1 && delta <= 20)
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

        // Bonus for both gathering and crafting
        if (increaseEvents >= 1 && decreaseEvents >= 1)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInMaterialRange(object? value)
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