using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting idle time values in game memory.
/// Idle time values typically:
/// - Are floats representing time without input
/// - Reset to 0 on player input
/// - Increase linearly during inactivity
/// - Often trigger AFK warnings at thresholds
/// </summary>
public sealed class IdleTimeHeuristic : IValueHeuristic
{
    public string Name => "Idle Time Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for idle time
        if (IsInIdleTimeRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Non-negative
        if (HeuristicUtilities.IsNonNegative(value.CurrentValue))
        {
            score += 0.15;
        }

        // Analyze observation history
        if (history.Count >= 2)
        {
            int increases = 0;
            int resets = 0;
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

                // Idle time increases during inactivity
                if (delta > 0 && delta < 30)
                {
                    increases++;
                    totalIncrease += delta;
                }

                // Reset to 0 on input
                if (delta < 0 && currVal.Value < 1)
                {
                    resets++;
                }
            }

            // Idle time should mostly increase or reset
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var increaseRatio = (double)increases / totalComparisons;
                if (increaseRatio > 0.6)
                {
                    score += 0.25;
                }

                // Some resets expected (player input)
                if (resets >= 1)
                {
                    score += 0.2;
                }
            }

            // Steady increase rate during idle
            if (increases > 0 && totalIncrease > 0)
            {
                var avgIncrease = totalIncrease / increases;
                if (avgIncrease > 0 && avgIncrease < 5)
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
        return normalizedType is "float" or "single" or "double" or "int32" or "int" or "int64" or "long";
    }

    private static bool IsInIdleTimeRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Idle time up to 30 minutes (1800 seconds) before AFK kick
        return val >= 0 && val <= 1800;
    }
}