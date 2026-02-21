using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting wave/round number in tower defense/horde mode games.
/// Wave values typically:
/// - Are positive integers starting from 1
/// - Only increase (never decrease except game reset)
/// - Increment by 1 between waves
/// - Often trigger enemy spawns when incremented
/// </summary>
public sealed class WaveNumberHeuristic : IValueHeuristic
{
    public string Name => "Wave/Round Number Detection";
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

        // Check value range (waves typically 1-999)
        if (IsInWaveRange(value.CurrentValue))
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
                score += 0.15;
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

            // Check for increment by 1
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

            // Check for any decrease (waves should never decrease)
            if (currVal < prevVal)
            {
                onlyIncreases = false;
                score -= 0.3;
            }

            // Wave values should be positive
            if (currVal < 1)
            {
                score -= 0.4;
            }

            // Wave values rarely exceed 9999
            if (currVal > 9999)
            {
                score -= 0.3;
            }
        }

        // Bonus for increment by 1 pattern
        if (incrementEvents >= 2)
            score += 0.2;

        // Bonus for consistent increment by 1
        if (consistentIncrementByOne && incrementEvents >= 2)
            score += 0.15;

        // Bonus for only increasing
        if (onlyIncreases && history.Count > 2)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "int64" or "long" or "byte";
    }

    private static bool IsInWaveRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Wave typically in range 0-9999
            var val = doubleValue.Value;
            return val >= 0 && val <= 9999;
        }
        catch
        {
            return false;
        }
    }
}