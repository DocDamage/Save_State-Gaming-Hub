using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting treasure map or treasure hunt count.
/// Treasure Map Count values typically:
/// - Are integers (0-50)
/// - Change when acquiring or using treasure maps
/// - Often used in RPGs and adventure games
/// </summary>
public sealed class TreasureMapCountHeuristic : IValueHeuristic
{
    public string Name => "Treasure Map Count Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int addEvents = 0;
        int removeEvents = 0;

        // Check value range (treasure maps typically 0-50)
        if (IsInTreasureMapRange(value.CurrentValue))
        {
            score += 0.4;
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

            // Check for acquiring map
            if (currVal > prevVal)
            {
                addEvents++;
                var delta = currVal.Value - prevVal.Value;
                if (delta <= 3)
                {
                    score += 0.15;
                }
            }

            // Check for using map
            if (currVal < prevVal)
            {
                removeEvents++;
                var delta = prevVal.Value - currVal.Value;
                if (delta <= 3)
                {
                    score += 0.12;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Reasonable max
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Bonus for add/remove patterns
        if (addEvents >= 1)
            score += 0.1;
        if (removeEvents >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInTreasureMapRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}