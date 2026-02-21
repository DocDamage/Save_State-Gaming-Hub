using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting total play time in games.
/// Play time values typically:
/// - Are floats (seconds with decimals) or integers (seconds/ticks)
/// - Only increase during gameplay
/// - Persist between sessions
/// - Often displayed as hours:minutes
/// </summary>
public sealed class PlayTimeHeuristic : IValueHeuristic
{
    public string Name => "Play Time Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool steadyIncrease = false;
        double totalIncrease = 0;

        // Check value range (play time can be very large)
        if (IsInPlayTimeRange(value.CurrentValue))
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

            // Play time should only increase
            if (currVal >= prevVal)
            {
                var delta = currVal.Value - prevVal.Value;
                totalIncrease += delta;

                // Check for steady small increases (time ticking)
                if (delta > 0 && delta < 60)
                {
                    score += 0.05;
                }
            }
            else
            {
                // Decrease detected - not play time
                score -= 0.5;
            }

            // Should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Check for steady increase pattern
        if (history.Count > 2 && totalIncrease > 0)
        {
            var avgIncrease = totalIncrease / (history.Count - 1);
            // If average increase is reasonable (e.g., 1-5 seconds per observation)
            if (avgIncrease > 0 && avgIncrease < 10)
            {
                steadyIncrease = true;
                score += 0.25;
            }
        }

        // Bonus for large accumulated values
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value > 3600) // More than 1 hour
        {
            score += 0.1;
        }

        // Play time rarely exceeds years of gameplay
        if (currentVal.HasValue && currentVal.Value > 100000000)
        {
            score -= 0.3;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int" or "int64" or "long";
    }

    private static bool IsInPlayTimeRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 999999999;
        }
        catch
        {
            return false;
        }
    }
}