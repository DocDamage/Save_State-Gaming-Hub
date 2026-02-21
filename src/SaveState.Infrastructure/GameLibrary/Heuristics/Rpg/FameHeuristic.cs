using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting fame/notoriety in open world RPGs.
/// Fame values typically:
/// - Are integers that only increase
/// - Earned through achievements and exploration
/// - Unlock rewards at thresholds
/// </summary>
public sealed class FameHeuristic : IValueHeuristic
{
    public string Name => "Fame/Notoriety Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool onlyIncreases = true;
        int increaseEvents = 0;

        // Check value range (fame typically 0-999999)
        if (IsInFameRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Must be integer type
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.2;
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

            // Fame should only increase
            if (currVal > prevVal)
            {
                increaseEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Fame increases by various amounts
                if (delta > 0 && delta < 1000)
                {
                    score += 0.08;
                }
            }
            else if (currVal < prevVal)
            {
                onlyIncreases = false;
                score -= 0.4;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for only increasing
        if (onlyIncreases && history.Count > 2)
            score += 0.25;

        // Bonus for increase events
        if (increaseEvents >= 2)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long";
    }

    private static bool IsInFameRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 9999999;
        }
        catch
        {
            return false;
        }
    }
}