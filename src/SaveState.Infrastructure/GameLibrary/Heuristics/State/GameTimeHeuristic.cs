using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting in-game time/day counter values in game memory.
/// Game time values typically:
/// - Are floats in range 0.0-86400.0 (seconds in a day)
/// - Constantly increasing
/// - Reset at midnight (0)
/// </summary>
public sealed class GameTimeHeuristic : IValueHeuristic
{
    public string Name => "Game Time Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int resets = 0;
        double totalDelta = 0;

        // Check value range
        if (IsInGameTimeRange(value.CurrentValue))
        {
            score += 0.3;
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
            totalDelta += delta;

            // Track increases
            if (delta > 0 && delta < 3600) // Less than an hour jump
            {
                increases++;
            }

            // Track resets (value went from high to low - midnight reset)
            if (delta < -80000) // Reset from near 86400 to near 0
            {
                resets++;
            }

            // Negative values are invalid for time
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }
        }

        // Game time should mostly increase
        if (history.Count > 1)
        {
            var increaseRatio = (double)increases / (history.Count - 1);
            if (increaseRatio > 0.8)
            {
                score += 0.3;
            }
        }

        // Midnight resets are characteristic
        if (resets >= 1)
        {
            score += 0.25;
        }

        // Steady increase rate
        if (history.Count > 1 && totalDelta > 0)
        {
            var avgDelta = totalDelta / (history.Count - 1);
            if (avgDelta > 0 && avgDelta < 60) // Steady increase, not jumps
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

    private static bool IsInGameTimeRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        return val >= 0.0 && val <= 86400.0; // Seconds in a day
    }
}