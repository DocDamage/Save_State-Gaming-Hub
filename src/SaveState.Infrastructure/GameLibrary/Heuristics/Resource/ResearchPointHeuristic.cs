using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting research points/knowledge resource in strategy/4X games.
/// Research points typically:
/// - Are integers (0-99999)
/// - Accumulate over time from buildings/scientists
/// - Decrease when unlocking technologies
/// </summary>
public sealed class ResearchPointHeuristic : IValueHeuristic
{
    public string Name => "Research Points/Knowledge Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gainEvents = 0;
        int spendEvents = 0;
        int steadyGainPattern = 0;

        // Check value range (research points typically 0-99999)
        if (IsInResearchPointRange(value.CurrentValue))
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

            // Check for gain (steady accumulation)
            if (currVal > prevVal)
            {
                gainEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Research accumulates steadily (5-100 per tick)
                if (delta >= 1 && delta <= 500)
                {
                    score += 0.12;
                    if (delta <= 100)
                    {
                        steadyGainPattern++;
                    }
                }
            }

            // Check for spend (tech unlocks)
            if (currVal < prevVal)
            {
                spendEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Tech costs are large one-time amounts
                if (delta >= 100 && delta <= 10000)
                {
                    score += 0.18;
                }
            }

            // Can go to zero when spending all points
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for steady accumulation pattern (research generation)
        if (steadyGainPattern >= 3)
            score += 0.2;
        if (spendEvents >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long";
    }

    private static bool IsInResearchPointRange(object? value)
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