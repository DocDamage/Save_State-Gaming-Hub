using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting estimated time of arrival to destination.
/// ETA values typically:
/// - Are integers representing seconds or minutes
/// - Decrease as player approaches destination
/// - Update based on distance and speed
/// </summary>
public sealed class ETAHeuristic : IValueHeuristic
{
    public string Name => "Estimated Time of Arrival Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasTimeChanges = false;
        int decreasingEvents = 0;

        // Check value range (ETA typically 0-3600 seconds or 0-60 minutes)
        if (IsInETARange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Must be integer (time is usually whole seconds/minutes)
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.2;
        }
        else
        {
            score += 0.15;
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

            // Check for time changes
            var delta = currVal.Value - prevVal.Value;
            if (Math.Abs(delta) >= 1)
            {
                hasTimeChanges = true;
                // Usually decreases
                if (delta < 0)
                {
                    decreasingEvents++;
                    score += 0.1;
                }
                // Can increase if moving away or changing destination
                else
                {
                    score += 0.03;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Zero means arrived
            if (currVal == 0)
            {
                score += 0.15;
            }

            // Reasonable maximum (1 hour in seconds or minutes)
            if (currVal > 7200)
            {
                score -= 0.3;
            }
        }

        // Bonus for time changes
        if (hasTimeChanges)
            score += 0.1;

        // Bonus for decreasing pattern
        if (decreasingEvents >= 2)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "float" or "single" or "double";
    }

    private static bool IsInETARange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 7200; // Up to 2 hours (seconds) or 120 minutes
        }
        catch
        {
            return false;
        }
    }
}