using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting kill/enemy defeat counters in shooter/action games.
/// Kill count values typically:
/// - Are positive integers starting from 0
/// - Only increase (never decrease during normal gameplay)
/// - Increment by 1 per kill (or more for multi-kills)
/// - Often persist across levels in campaign modes
/// </summary>
public sealed class KillCountHeuristic : IValueHeuristic
{
    public string Name => "Kill Count Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int incrementEvents = 0;
        bool onlyIncreases = true;
        bool startsFromZero = false;

        // Check value range (kill counts typically 0-99999)
        if (IsInKillCountRange(value.CurrentValue))
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
            if (val.HasValue && val.Value == 0)
            {
                startsFromZero = true;
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

            // Check for increment (kill)
            if (currVal > prevVal)
            {
                incrementEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Single kill = 1, but could be more for multi-kill scenarios
                if (delta >= 1 && delta <= 5)
                {
                    score += 0.1;
                }
            }

            // Check for any decrease (kill counts should never decrease)
            if (currVal < prevVal)
            {
                onlyIncreases = false;
                score -= 0.5;
            }

            // Kill counts should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Reasonable upper limit
            if (currVal > 999999)
            {
                score -= 0.3;
            }
        }

        // Bonus for increment pattern
        if (incrementEvents >= 2)
            score += 0.15;

        // Strong bonus for only increasing
        if (onlyIncreases && history.Count > 2)
            score += 0.2;

        // Bonus for starting from zero
        if (startsFromZero)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "int64" or "long" or "byte";
    }

    private static bool IsInKillCountRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 999999;
        }
        catch
        {
            return false;
        }
    }
}