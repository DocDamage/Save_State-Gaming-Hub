using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting game completion percentage values in game memory.
/// Completion values typically:
/// - Are floats in range 0.0-100.0
/// - Only increasing
/// - Change on achievements/progress
/// </summary>
public sealed class CompletionHeuristic : IValueHeuristic
{
    public string Name => "Completion Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;

        // Check value range
        if (IsInCompletionRange(value.CurrentValue))
        {
            score += 0.35;
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

            if (delta > 0)
            {
                increases++;
            }
            else if (delta < 0)
            {
                decreases++;
            }

            // Completion should never exceed 100%
            if (currVal.Value > 100)
            {
                score -= 0.3;
            }

            // Completion should never be negative
            if (currVal.Value < 0)
            {
                score -= 0.3;
            }
        }

        // Completion should only increase
        if (history.Count > 1)
        {
            if (decreases == 0 && increases > 0)
            {
                score += 0.35;
            }

            var increaseRatio = (double)increases / (history.Count - 1);
            if (increaseRatio > 0.9)
            {
                score += 0.15;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInCompletionRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        return val >= 0.0 && val <= 100.0;
    }
}
