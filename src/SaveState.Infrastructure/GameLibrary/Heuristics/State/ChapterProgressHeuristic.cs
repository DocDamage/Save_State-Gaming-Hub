using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting chapter progress values in game memory.
/// Chapter progress values typically:
/// - Are integers or floats (0.0 to 100.0 or 0-20)
/// - Increase as player advances through a chapter
/// - Reset to 0 at chapter start
/// - Show completion percentage or chapter number
/// </summary>
public sealed class ChapterProgressHeuristic : IValueHeuristic
{
    public string Name => "Chapter Progress Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range
        if (IsInProgressRange(value.CurrentValue))
        {
            score += 0.3;
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
            int resets = 0;
            int constants = 0;
            double totalIncrease = 0;

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

                // Track increases
                if (delta > 0)
                {
                    increases++;
                    totalIncrease += delta;
                }
                // Track resets (chapter restart)
                else if (delta < -50)
                {
                    resets++;
                }
                // Track constants
                else if (delta == 0)
                {
                    constants++;
                }
            }

            // Progress should mostly increase
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var increaseRatio = (double)increases / totalComparisons;
                if (increaseRatio > 0.5)
                {
                    score += 0.3;
                }
                else if (increaseRatio > 0.3)
                {
                    score += 0.15;
                }
            }

            // Some constant periods are expected
            if (constants >= 1)
            {
                score += 0.1;
            }

            // Resets indicate chapter boundaries
            if (resets >= 1)
            {
                score += 0.2;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "float" or "single" or "double";
    }

    private static bool IsInProgressRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Chapter number (0-50) or percentage (0-100)
        return (val >= 0 && val <= 50) || (val >= 0 && val <= 100);
    }
}