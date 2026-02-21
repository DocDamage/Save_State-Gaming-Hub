using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting key/item counts in adventure/puzzle games.
/// Key values typically:
/// - Are small integers (0-10)
/// - Increase when picking up keys
/// - Decrease when using keys
/// - Never go negative
/// </summary>
public sealed class KeyCountHeuristic : IValueHeuristic
{
    public string Name => "Key/Item Count Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int pickupEvents = 0;
        int useEvents = 0;
        bool smallValues = true;

        // Check value range (keys typically 0-10)
        if (IsInKeyRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Must be integer type
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.3;
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

            // Check for small values throughout
            if (currVal > 20)
            {
                smallValues = false;
            }

            // Check for key pickup (increment by 1)
            if (currVal == prevVal + 1)
            {
                pickupEvents++;
                score += 0.15;
            }

            // Check for key use (decrement by 1)
            if (currVal == prevVal - 1)
            {
                useEvents++;
                score += 0.15;
            }

            // Keys should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Large changes are unlikely for keys
            if (Math.Abs(currVal.Value - prevVal.Value) > 1)
            {
                score -= 0.2;
            }
        }

        // Bonus for pickup/use pattern
        if (pickupEvents >= 1)
            score += 0.1;
        if (useEvents >= 1)
            score += 0.1;

        // Strong indicator: consistently small values
        if (smallValues && history.Count > 2)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInKeyRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 50;
        }
        catch
        {
            return false;
        }
    }
}