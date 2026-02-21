using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting timer/countdown values in game memory.
/// Timer values typically:
/// - Are floats or integers
/// - Decrease steadily over time (for countdowns)
/// - Or increase steadily (for elapsed time)
/// - Change at consistent intervals
/// </summary>
public sealed class TimerHeuristic : IValueHeuristic
{
    public string Name => "Timer Detection";
    public string Category => "Timer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for timers (typically seconds)
        if (IsInTimerRange(value.CurrentValue))
        {
            score += 0.25;
        }

        // Check for steady change pattern
        if (history.Count >= 3)
        {
            var deltas = new List<double>();

            for (int i = 1; i < history.Count; i++)
            {
                if (history[i].Value == null || history[i - 1].Value == null)
                    continue;

                double? curr = HeuristicUtilities.ConvertToDouble(history[i].Value);
                double? prev = HeuristicUtilities.ConvertToDouble(history[i - 1].Value);

                if (!curr.HasValue || !prev.HasValue)
                    continue;

                var delta = curr.Value - prev.Value;
                var timeDelta = (history[i].Timestamp - history[i - 1].Timestamp).TotalMilliseconds;

                if (timeDelta > 0)
                {
                    deltas.Add(delta / timeDelta);
                }
            }

            if (deltas.Count >= 2)
            {
                // Check for consistent rate of change (timer characteristic)
                var avgDelta = deltas.Average();
                var variance = deltas.Average(d => Math.Pow(d - avgDelta, 2));
                var stdDev = Math.Sqrt(variance);

                // Low standard deviation means consistent change (timer-like)
                if (stdDev < 0.001)
                {
                    score += 0.35;
                }

                // Timers either consistently increase or decrease
                if (Math.Abs(avgDelta) > 0)
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
        return normalizedType is "float" or "single" or "int32" or "int" or "double";
    }

    private static bool IsInTimerRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Timers typically in range of game sessions (0 to a few hours in seconds)
        return val >= 0 && val <= 86400; // 0 to 24 hours in seconds
    }
}