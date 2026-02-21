using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting scoreboard position in competitive games.
/// Position values typically:
/// - Are integers (1 to player count)
/// - Change during match based on performance
/// - Lower is better
/// </summary>
public sealed class ScoreboardPositionHeuristic : IValueHeuristic
{
    public string Name => "Scoreboard Position Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int changeEvents = 0;
        bool startsAtOne = false;

        // Check value range (position 1-1000)
        if (IsInPositionRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Must be integer
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.3;
        }
        else
        {
            score += 0.1;
        }

        // Check initial value
        var firstValue = history.FirstOrDefault(o => o.Value != null);
        if (firstValue != null)
        {
            var val = HeuristicUtilities.ConvertToDouble(firstValue.Value);
            if (val.HasValue && val.Value == 1)
            {
                startsAtOne = true;
                score += 0.15;
            }
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

            // Check for position changes
            if (currVal != prevVal)
            {
                changeEvents++;
                var delta = Math.Abs(currVal.Value - prevVal.Value);
                // Usually changes by small amounts
                if (delta <= 5)
                {
                    score += 0.12;
                }
            }

            // Should be positive
            if (currVal < 1)
            {
                score -= 0.5;
            }

            // Reasonable upper limit
            if (currVal > 200)
            {
                score -= 0.3;
            }
        }

        // Bonus for changes
        if (changeEvents >= 1)
            score += 0.1;

        // Bonus for starting at 1
        if (startsAtOne)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInPositionRange(object? value)
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