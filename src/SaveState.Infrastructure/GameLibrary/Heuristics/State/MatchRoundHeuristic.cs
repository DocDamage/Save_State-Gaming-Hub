using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting match round values in game memory.
/// Match round values typically:
/// - Are small integers (1-9 or 0-9 range)
/// - Increment by 1 between rounds
/// - Reset to 1 or 0 at match start
/// - Remain constant during a round
/// </summary>
public sealed class MatchRoundHeuristic : IValueHeuristic
{
    public string Name => "Match Round Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for match rounds
        if (IsInRoundRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Rounds should be integers
        if (HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score += 0.2;
        }

        // Analyze observation history
        if (history.Count >= 3)
        {
            int sequentialIncreases = 0;
            int resets = 0;
            int constants = 0;

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

                // Sequential increase by 1
                if (delta == 1)
                {
                    sequentialIncreases++;
                }
                // Reset to 0 or 1
                else if (delta < 0 && (currVal.Value == 0 || currVal.Value == 1))
                {
                    resets++;
                }
                // Constant value
                else if (delta == 0)
                {
                    constants++;
                }
            }

            // Bonus for sequential round progression
            if (sequentialIncreases >= 1)
            {
                score += 0.25;
            }

            // Bonus for round resets (match restart)
            if (resets >= 1)
            {
                score += 0.15;
            }

            // Values should be mostly constant during rounds
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var constantRatio = (double)constants / totalComparisons;
                if (constantRatio > 0.7)
                {
                    score += 0.1;
                }
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "byte" or "uint8";
    }

    private static bool IsInRoundRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        return val >= 0 && val <= 9;
    }
}