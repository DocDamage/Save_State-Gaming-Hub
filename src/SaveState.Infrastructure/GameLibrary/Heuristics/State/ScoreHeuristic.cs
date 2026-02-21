using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting score values in game memory.
/// Score values typically:
/// - Are integers
/// - Only increase (rarely decrease)
/// - Increase on "ScoreIncreased" action
/// - Often have specific patterns (multiples of 10, 100, etc.)
/// - Can get very large
/// </summary>
public sealed class ScoreHeuristic : IValueHeuristic
{
    public string Name => "Score Detection";
    public string Category => "Score";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int scoreEvents = 0;
        int increases = 0;
        int decreases = 0;

        // Check value range for score
        if (IsInScoreRange(value.CurrentValue))
        {
            score += 0.2;
        }

        // Scores are typically integers
        if (HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score += 0.15;
        }

        // Check for common score patterns (multiples of 10, 100)
        if (HasScorePattern(value.CurrentValue))
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

            var delta = currVal.Value - prevVal.Value;

            // Track increases vs decreases
            if (delta > 0)
            {
                increases++;
            }
            else if (delta < 0)
            {
                decreases++;
            }

            // Check for ScoreIncreased action correlation
            if (curr.RelatedAction == PlayerAction.ScoreIncreased)
            {
                scoreEvents++;
                if (delta > 0)
                {
                    score += 0.15;
                }
            }

            // Scores rarely decrease (unless penalty system)
            if (delta < 0)
            {
                score -= 0.1;
            }

            // Check for reasonable score gain amounts
            if (delta > 0 && delta < 10000)
            {
                score += 0.05;
            }
        }

        // Score should mostly increase
        if (history.Count > 1 && increases > 0)
        {
            var increaseRatio = (double)increases / (history.Count - 1);
            if (increaseRatio > 0.8)
            {
                score += 0.2;
            }
        }

        // Bonus for score events
        if (scoreEvents >= 2)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long";
    }

    private static bool IsInScoreRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        return val >= 0 && val <= 999999999999; // Scores can be very high
    }

    private static bool HasScorePattern(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = (long)doubleValue.Value;
        // Scores often end in 0 (multiples of 10, 100, etc.)
        return val % 10 == 0;
    }
}
