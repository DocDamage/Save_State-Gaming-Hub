using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting retry count values in game memory.
/// Retry count values typically:
/// - Are integers counting retry attempts
/// - Only increase on failed attempts
/// - Often reset on success
/// - Track per-challenge or per-level
/// </summary>
public sealed class RetryCountHeuristic : IValueHeuristic
{
    public string Name => "Retry Count Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for retry counts
        if (IsInRetryRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Should be integer
        if (HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score += 0.2;
        }

        // Non-negative
        if (HeuristicUtilities.IsNonNegative(value.CurrentValue))
        {
            score += 0.1;
        }

        // Analyze observation history
        if (history.Count >= 2)
        {
            int increases = 0;
            int decreases = 0;
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

                if (delta > 0)
                {
                    increases++;
                }
                else if (delta < 0)
                {
                    if (currVal.Value == 0 && prevVal.Value > 0)
                    {
                        resets++;
                    }
                    else
                    {
                        decreases++;
                    }
                }
                else
                {
                    constants++;
                }
            }

            // Retry count should be mostly constant
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var constantRatio = (double)constants / totalComparisons;
                if (constantRatio > 0.5)
                {
                    score += 0.15;
                }
            }

            // Occasional increases (retries)
            if (increases >= 1 && increases <= 5)
            {
                score += 0.15;
            }

            // Resets on success are expected
            if (resets >= 1)
            {
                score += 0.15;
            }

            // Decreases without reset are invalid
            if (decreases > 0)
            {
                score -= 0.2 * decreases;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "byte" or "uint8";
    }

    private static bool IsInRetryRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Retry counts typically 0-50
        return val >= 0 && val <= 50;
    }
}