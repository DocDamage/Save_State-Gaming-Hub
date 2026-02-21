using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting pause time values in game memory.
/// Pause time values typically:
/// - Are floats representing time spent paused
/// - Only increase when game is paused
/// - Remain constant during gameplay
/// - Reset between sessions or accumulate
/// </summary>
public sealed class PauseTimeHeuristic : IValueHeuristic
{
    public string Name => "Pause Time Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for pause time
        if (IsInPauseTimeRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Non-negative
        if (HeuristicUtilities.IsNonNegative(value.CurrentValue))
        {
            score += 0.15;
        }

        // Analyze observation history
        if (history.Count >= 3)
        {
            int constants = 0;
            int increases = 0;

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

                // Pause time stays constant during gameplay
                if (delta == 0)
                {
                    constants++;
                }
                // Increases during pause
                else if (delta > 0 && delta < 60)
                {
                    increases++;
                }
            }

            // Pause time is mostly constant
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var constantRatio = (double)constants / totalComparisons;
                if (constantRatio > 0.7)
                {
                    score += 0.35;
                }
                else if (constantRatio > 0.5)
                {
                    score += 0.15;
                }

                // Some increases expected (when paused)
                var increaseRatio = (double)increases / totalComparisons;
                if (increaseRatio > 0.1 && increaseRatio < 0.4)
                {
                    score += 0.2;
                }
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int" or "int64" or "long";
    }

    private static bool IsInPauseTimeRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Pause time up to 2 hours (7200 seconds)
        return val >= 0 && val <= 7200;
    }
}