using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting influence/political capital in strategy/4X games.
/// Influence values typically:
/// - Are integers (0-9999)
/// - Increase from population, buildings, or diplomacy
/// - Decrease when enacting policies or forming alliances
/// </summary>
public sealed class InfluenceHeuristic : IValueHeuristic
{
    public string Name => "Influence/Political Capital Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gainEvents = 0;
        int spendEvents = 0;

        // Check value range (influence typically 0-9999)
        if (IsInInfluenceRange(value.CurrentValue))
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

            // Check for gain (diplomacy/population)
            if (currVal > prevVal)
            {
                gainEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Influence accumulates steadily
                if (delta >= 1 && delta <= 100)
                {
                    score += 0.12;
                }
            }

            // Check for spend (policies/alliances)
            if (currVal < prevVal)
            {
                spendEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Political actions cost significant influence
                if (delta >= 50 && delta <= 2000)
                {
                    score += 0.15;
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
            score += 0.1;
        if (spendEvents >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long";
    }

    private static bool IsInInfluenceRange(object? value)
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