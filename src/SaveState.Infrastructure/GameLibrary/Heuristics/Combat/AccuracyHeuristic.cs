using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting accuracy percentage in shooter games.
/// Accuracy values typically:
/// - Are floats (0.0-100.0) representing percentage
/// - Change based on hits vs misses
/// - Start at 100% and decrease over time
/// </summary>
public sealed class AccuracyHeuristic : IValueHeuristic
{
    public string Name => "Accuracy Percentage Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool inPercentageRange = false;
        bool gradualDecrease = true;

        // Check value range (accuracy is 0-100%)
        if (IsInAccuracyRange(value.CurrentValue))
        {
            score += 0.4;
            inPercentageRange = true;
        }

        // Float type preferred for percentage
        if (value.ValueType.ToLowerInvariant() is "float" or "single" or "double")
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

            // Accuracy should not increase significantly (rarely improves)
            if (currVal > prevVal + 5)
            {
                gradualDecrease = false;
                score -= 0.1;
            }

            // Check for gradual decrease on misses
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Attacked)
            {
                var delta = prevVal.Value - currVal.Value;
                // Small decreases on misses
                if (delta > 0 && delta < 5)
                {
                    score += 0.1;
                }
            }

            // Should stay in 0-100 range
            if (currVal < 0 || currVal > 100)
            {
                score -= 0.5;
            }
        }

        // Bonus for gradual decrease pattern
        if (gradualDecrease && history.Count > 2)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInAccuracyRange(object? value)
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