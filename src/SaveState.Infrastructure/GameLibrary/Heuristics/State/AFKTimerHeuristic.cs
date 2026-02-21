using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting AFK timer values in game memory.
/// AFK timer values typically:
/// - Are floats counting down or up to kick threshold
/// - Reset on player activity
/// - Trigger warnings at specific thresholds
/// - Range from 0 to several minutes
/// </summary>
public sealed class AFKTimerHeuristic : IValueHeuristic
{
    public string Name => "AFK Timer Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for AFK timer
        if (IsInAFKRange(value.CurrentValue))
        {
            score += 0.35;
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

                // Timer can count up or down depending on implementation
                if (delta > 0 && delta < 30)
                {
                    increases++;
                }
                else if (delta < 0 && delta > -30)
                {
                    decreases++;
                }

                // Reset to 0 or max on activity
                if (Math.Abs(delta) > 60 || (delta < 0 && currVal.Value < 5))
                {
                    resets++;
                }
            }

            // AFK timer should have consistent direction
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var changeCount = increases + decreases;
                if (changeCount > 0)
                {
                    // Consistent direction preferred
                    if (increases > decreases * 2 || decreases > increases * 2)
                    {
                        score += 0.25;
                    }
                    else
                    {
                        score += 0.1;
                    }
                }

                // Resets indicate player activity
                if (resets >= 1)
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

    private static bool IsInAFKRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // AFK timer up to 30 minutes
        return val >= 0 && val <= 1800;
    }
}