using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting oil/petroleum resource in strategy/industrial games.
/// Oil values typically:
/// - Are integers (0-99999)
/// - Increase from pumping or extraction
/// - Decrease when refining or fueling machinery
/// </summary>
public sealed class OilHeuristic : IValueHeuristic
{
    public string Name => "Oil/Petroleum Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gainEvents = 0;
        int spendEvents = 0;

        // Check value range (oil typically 0-99999)
        if (IsInOilRange(value.CurrentValue))
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

            // Check for gain (extraction)
            if (currVal > prevVal)
            {
                gainEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Oil extraction is large scale (50-500)
                if (delta >= 20 && delta <= 1000)
                {
                    score += 0.15;
                }
            }

            // Check for spend (refining/fuel)
            if (currVal < prevVal)
            {
                spendEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Refining/fuel uses 50-1000 oil
                if (delta >= 25 && delta <= 2000)
                {
                    score += 0.12;
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
        return normalizedType is "int32" or "int" or "int64" or "long";
    }

    private static bool IsInOilRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 9999999;
        }
        catch
        {
            return false;
        }
    }
}