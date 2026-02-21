using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting competitive rank in ranked games.
/// Rank values typically:
/// - Are integers (1-100 or similar)
/// - Can increase or decrease based on performance
/// - Often tied to matchmaking rating
/// </summary>
public sealed class RankHeuristic : IValueHeuristic
{
    public string Name => "Competitive Rank Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increaseEvents = 0;
        int decreaseEvents = 0;

        // Check value range (rank typically 1-100 or 1-30)
        if (IsInRankRange(value.CurrentValue))
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

            // Check for increase (rank up)
            if (currVal < prevVal) // Lower number = higher rank
            {
                increaseEvents++;
                score += 0.15;
            }

            // Check for decrease (rank down)
            if (currVal > prevVal)
            {
                decreaseEvents++;
                score += 0.12;
            }

            // Usually changes by 1
            if (Math.Abs(currVal.Value - prevVal.Value) == 1)
            {
                score += 0.1;
            }

            // Should be positive
            if (currVal < 1)
            {
                score -= 0.5;
            }
        }

        // Bonus for both increases and decreases
        if (increaseEvents >= 1 || decreaseEvents >= 1)
            score += 0.15;

        // Common rank values
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value >= 1 && currentVal.Value <= 100)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInRankRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 1 && val <= 1000;
        }
        catch
        {
            return false;
        }
    }
}