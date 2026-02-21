using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting death/death count in rogue-like and hardcore games.
/// Death count values typically:
/// - Are positive integers starting from 0
/// - Only increase (never decrease)
/// - Increment by 1 per death
/// - Often persist across play sessions
/// </summary>
public sealed class DeathCountHeuristic : IValueHeuristic
{
    public string Name => "Death Count Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int deathEvents = 0;
        bool onlyIncreases = true;
        bool startsFromZero = false;

        // Check value range (death counts typically 0-9999)
        if (IsInDeathCountRange(value.CurrentValue))
        {
            score += 0.35;
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
            if (val.HasValue && val.Value == 0)
            {
                startsFromZero = true;
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

            // Check for death event (increment by exactly 1)
            if (currVal == prevVal + 1 && curr.RelatedAction == PlayerAction.Died)
            {
                deathEvents++;
                score += 0.25;
            }
            // Increment without death action (still counts)
            else if (currVal == prevVal + 1)
            {
                deathEvents++;
                score += 0.1;
            }

            // Check for any decrease (deaths should never decrease)
            if (currVal < prevVal)
            {
                onlyIncreases = false;
                score -= 0.5;
            }

            // Should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for death events
        if (deathEvents >= 1)
            score += 0.15;

        // Strong bonus for only increasing
        if (onlyIncreases && history.Count > 2)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "int64" or "long" or "byte";
    }

    private static bool IsInDeathCountRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 99999;
        }
        catch
        {
            return false;
        }
    }
}