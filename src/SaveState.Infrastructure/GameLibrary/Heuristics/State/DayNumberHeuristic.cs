using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting day number in survival/crafting games.
/// Day values typically:
/// - Are positive integers starting from 1
/// - Only increase (never decrease during normal gameplay)
/// - Increment by 1 at day/night cycle
/// - Persist between play sessions
/// </summary>
public sealed class DayNumberHeuristic : IValueHeuristic
{
    public string Name => "Day Number Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int incrementEvents = 0;
        bool onlyIncreases = true;
        bool startsFromOne = false;
        bool consistentIncrementByOne = true;

        // Check value range (days typically 1-9999)
        if (IsInDayRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Must be integer type
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.3;
        }

        // Check initial value
        var firstValue = history.FirstOrDefault(o => o.Value != null);
        if (firstValue != null)
        {
            var val = HeuristicUtilities.ConvertToDouble(firstValue.Value);
            if (val.HasValue && (val.Value == 1 || val.Value == 0))
            {
                startsFromOne = true;
                score += 0.2;
            }
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

            // Check for increment by 1 (next day)
            if (currVal == prevVal + 1)
            {
                incrementEvents++;
                score += 0.15;
            }
            // Check if value changed by more than 1
            else if (currVal > prevVal + 1)
            {
                consistentIncrementByOne = false;
            }

            // Check for any decrease (days should never decrease)
            if (currVal < prevVal)
            {
                onlyIncreases = false;
                score -= 0.4;
            }

            // Day values should be positive
            if (currVal < 1)
            {
                score -= 0.4;
            }

            // Day values rarely exceed 99999
            if (currVal > 99999)
            {
                score -= 0.3;
            }
        }

        // Bonus for increment by 1 pattern
        if (incrementEvents >= 1)
            score += 0.15;

        // Bonus for consistent increment by 1
        if (consistentIncrementByOne && incrementEvents >= 1)
            score += 0.15;

        // Strong bonus for only increasing
        if (onlyIncreases && history.Count > 2)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "int64" or "long" or "byte";
    }

    private static bool IsInDayRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Day typically in range 0-99999
            var val = doubleValue.Value;
            return val >= 0 && val <= 99999;
        }
        catch
        {
            return false;
        }
    }
}