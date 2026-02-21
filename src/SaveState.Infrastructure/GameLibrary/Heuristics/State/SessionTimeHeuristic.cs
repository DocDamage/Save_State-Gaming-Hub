using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting session time values in game memory.
/// Session time values typically:
/// - Are floats representing elapsed time in seconds
/// - Constantly increase during gameplay
/// - Reset to 0 at session start
/// - Can range from 0 to several hours
/// </summary>
public sealed class SessionTimeHeuristic : IValueHeuristic
{
    public string Name => "Session Time Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for session time (up to 8 hours in seconds)
        if (IsInSessionTimeRange(value.CurrentValue))
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
            double totalDelta = 0;

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

                // Session time should increase
                if (delta > 0 && delta < 60) // Less than a minute jump
                {
                    increases++;
                }

                // Reset to near zero indicates new session
                if (delta < -100 && currVal.Value < 10)
                {
                    resets++;
                }

                // Negative values are invalid
                if (currVal.Value < 0)
                {
                    score -= 0.5;
                }
            }

            // Session time should mostly increase
            if (history.Count > 1)
            {
                var increaseRatio = (double)increases / (history.Count - 1);
                if (increaseRatio > 0.85)
                {
                    score += 0.35;
                }
                else if (increaseRatio > 0.6)
                {
                    score += 0.15;
                }
            }

            // Session resets are acceptable
            if (resets >= 1)
            {
                score += 0.1;
            }

            // Steady increase rate
            if (history.Count > 1 && totalDelta > 0)
            {
                var avgDelta = totalDelta / (history.Count - 1);
                if (avgDelta > 0 && avgDelta < 30) // Steady increase
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

    private static bool IsInSessionTimeRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Session time up to 8 hours (28800 seconds)
        return val >= 0 && val <= 28800;
    }
}