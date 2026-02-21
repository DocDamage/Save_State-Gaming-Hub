using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting mission/time trial timers.
/// Mission timers typically:
/// - Are floats (seconds with milliseconds precision)
/// - Count down from a set time
/// - Cause failure when reaching 0
/// </summary>
public sealed class MissionTimerHeuristic : IValueHeuristic
{
    public string Name => "Mission Timer Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool steadyDecrease = true;
        double totalDecrease = 0;
        int decreaseCount = 0;

        // Check for float type (timers usually have decimals)
        if (IsFloatType(value.ValueType))
        {
            score += 0.2;
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

            // Timer should decrease
            if (currVal < prevVal)
            {
                decreaseCount++;
                totalDecrease += prevVal.Value - currVal.Value;
            }
            else if (currVal > prevVal)
            {
                // Timers rarely increase (only if extended)
                steadyDecrease = false;
            }

            // Timer should not go negative (usually stops at 0)
            if (currVal < 0)
            {
                score -= 0.3;
            }

            // Reasonable timer range (0-3600 seconds = 1 hour)
            if (currVal > 7200)
            {
                score -= 0.2;
            }
        }

        // Bonus for steady decrease pattern
        if (decreaseCount >= 3)
        {
            var avgDecrease = totalDecrease / decreaseCount;
            // If decreasing at reasonable rate (0.1-5 seconds per tick)
            if (avgDecrease > 0.01 && avgDecrease < 5)
            {
                score += 0.3;
            }
        }

        // Bonus for only decreasing
        if (steadyDecrease && decreaseCount > 0)
            score += 0.2;

        // Check for common starting values
        var firstValue = history.FirstOrDefault(o => o.Value != null);
        if (firstValue != null)
        {
            var val = HeuristicUtilities.ConvertToDouble(firstValue.Value);
            if (val.HasValue)
            {
                // Common timer values: 30, 60, 120, 300, 600 seconds
                var commonTimers = new[] { 30.0, 60.0, 120.0, 180.0, 300.0, 600.0, 1800.0, 3600.0 };
                foreach (var common in commonTimers)
                {
                    if (Math.Abs(val.Value - common) < 5)
                    {
                        score += 0.15;
                        break;
                    }
                }
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsFloatType(string valueType)
    {
        var normalized = valueType.ToLowerInvariant();
        return normalized is "float" or "single" or "double";
    }
}